/*
 * YAi!
 *
 * Copyright © 2019-2026 UmbertoGiacobbiDotBiz. All rights reserved.
 * Website: https://umbertogiacobbi.biz
 * Email: hello@umbertogiacobbi.biz
 *
 * This file is part of YAi!.
 *
 * YAi.Tools.ResourceSigner
 * Entry point for the YAi resource signing tool.
 *
 * Usage:
 *   dotnet run --project src/YAi.Tools.ResourceSigner -- sign \
 *     --root <resourceRoot> \
 *     --private-key <path> \
 *     --manifest <path> \
 *     --signature <path> \
 *     [--passphrase-env <ENV_VAR>]
 *
 * NOTE: --passphrase is intentionally NOT supported.
 *       Passphrases must be entered interactively or via --passphrase-env for CI only.
 */

using YAi.Tools.ResourceSigner.Security;
using YAi.Tools.ResourceSigner.Signing;
using YAi.Persona.Services.Security.ResourceIntegrity;

// ─── Parse arguments ─────────────────────────────────────────────────────────

if (args.Length == 0 || args[0] != "sign")
{
    Console.Error.WriteLine("Usage: yai-resource-signer sign --root <path> --private-key <path> --manifest <path> --signature <path> [--passphrase-env <ENV_VAR>]");
    return SigningExitCodes.InvalidArguments;
}

string? rootPath = GetArg(args, "--root");
string? privateKeyPath = GetArg(args, "--private-key");
string? manifestPath = GetArg(args, "--manifest");
string? signaturePath = GetArg(args, "--signature");
string? passphraseEnv = GetArg(args, "--passphrase-env");

// Guard: --passphrase is explicitly forbidden
if (Array.Exists(args, a => string.Equals(a, "--passphrase", StringComparison.OrdinalIgnoreCase)))
{
    Console.Error.WriteLine("[signing_key_passphrase_required] --passphrase is not supported. " +
                             "Enter the passphrase interactively or use --passphrase-env for CI only.");
    return SigningExitCodes.SigningKeyPassphraseRequired;
}

if (string.IsNullOrWhiteSpace(rootPath)
    || string.IsNullOrWhiteSpace(privateKeyPath)
    || string.IsNullOrWhiteSpace(manifestPath)
    || string.IsNullOrWhiteSpace(signaturePath))
{
    Console.Error.WriteLine("Missing required argument(s). --root, --private-key, --manifest, and --signature are all required.");
    return SigningExitCodes.InvalidArguments;
}

if (!Directory.Exists(rootPath))
{
    Console.Error.WriteLine($"[signing_unexpected_error] Resource root does not exist: {rootPath}");
    return SigningExitCodes.SigningUnexpectedError;
}

// ─── Resolve passphrase ───────────────────────────────────────────────────────

char[]? passphrase = null;
bool interactive = !Console.IsInputRedirected;

try
{
    if (!string.IsNullOrWhiteSpace(passphraseEnv))
    {
        // CI explicit env var fallback
        string? envValue = Environment.GetEnvironmentVariable(passphraseEnv);
        if (string.IsNullOrEmpty(envValue))
        {
            Console.Error.WriteLine($"[signing_key_passphrase_required] --passphrase-env was specified but '{passphraseEnv}' is empty or not set.");
            return SigningExitCodes.SigningKeyPassphraseRequired;
        }
        passphrase = envValue.ToCharArray();
        Console.WriteLine($"Using passphrase from environment variable '{passphraseEnv}'.");
    }
    else if (interactive)
    {
        passphrase = SecureConsolePrompt.ReadSecret("Enter YAi signing key passphrase: ");

        if (passphrase.Length == 0)
        {
            Console.Error.WriteLine("[signing_key_passphrase_required] No passphrase entered. Signing requires a passphrase for encrypted keys.");
            return SigningExitCodes.SigningKeyPassphraseRequired;
        }
    }
    else
    {
        Console.Error.WriteLine("[signing_key_passphrase_required] Non-interactive mode detected and no --passphrase-env provided. Cannot sign.");
        return SigningExitCodes.SigningKeyPassphraseRequired;
    }

    // ─── Build manifest ───────────────────────────────────────────────────────

    Console.WriteLine($"Scanning resource root: {rootPath}");
    var builder = new ResourceManifestBuilder();
    ResourceManifest manifest = builder.Build(rootPath);
    Console.WriteLine($"Found {manifest.Files.Count} file(s) to sign.");

    // ─── Sign and write ───────────────────────────────────────────────────────

    var signer = new ResourceManifestSigner();
    int exitCode = signer.Sign(manifest, manifestPath, signaturePath, privateKeyPath, passphrase);

    if (exitCode == 0)
    {
        Console.WriteLine($"Manifest written to:  {manifestPath}");
        Console.WriteLine($"Signature written to: {signaturePath}");
        Console.WriteLine("Signing complete.");
    }

    return exitCode;
}
finally
{
    SecureConsolePrompt.Clear(passphrase);
}

// ─── Helpers ─────────────────────────────────────────────────────────────────

static string? GetArg(string[] args, string name)
{
    int idx = Array.IndexOf(args, name);
    if (idx >= 0 && idx + 1 < args.Length)
        return args[idx + 1];
    return null;
}
