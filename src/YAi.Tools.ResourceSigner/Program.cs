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
 *   dotnet run --project src/YAi.Tools.ResourceSigner -- sign-file \
 *     --file <path> \
 *     --private-key <path> \
 *     --signature <path> \
 *     [--algorithm <Ed25519|RSA-PSS-SHA256>] \
 *     [--passphrase-env <ENV_VAR>]
 *
 *   dotnet run --project src/YAi.Tools.ResourceSigner -- verify-file \
 *     --file <path> \
 *     --signature <path> \
 *     --public-key <path> \
 *     [--algorithm <Ed25519|RSA-PSS-SHA256>]
 *
 * NOTE: --passphrase is intentionally NOT supported.
 *       Passphrases must be entered interactively or via --passphrase-env for CI only.
 */

using YAi.Tools.ResourceSigner.Security;
using YAi.Tools.ResourceSigner.Signing;
using YAi.Persona.Services.Security.ResourceIntegrity;

// ─── Parse arguments ─────────────────────────────────────────────────────────

if (args.Length == 0)
{
    Console.Error.WriteLine("Usage: yai-resource-signer <sign|sign-file|verify-file> ...");
    return SigningExitCodes.InvalidArguments;
}

string command = args[0];

// Guard: --passphrase is explicitly forbidden
if (Array.Exists(args, a => string.Equals(a, "--passphrase", StringComparison.OrdinalIgnoreCase)))
{
    Console.Error.WriteLine("[signing_key_passphrase_required] --passphrase is not supported. " +
                             "Enter the passphrase interactively or use --passphrase-env for CI only.");
    return SigningExitCodes.SigningKeyPassphraseRequired;
}

if (string.Equals(command, "verify-file", StringComparison.OrdinalIgnoreCase))
{
    string? filePath = GetArg(args, "--file");
    string? detachedSignaturePath = GetArg(args, "--signature");
    string? publicKeyPath = GetArg(args, "--public-key");
    string algorithm = GetArg(args, "--algorithm") ?? "Ed25519";

    if (string.IsNullOrWhiteSpace(filePath)
        || string.IsNullOrWhiteSpace(detachedSignaturePath)
        || string.IsNullOrWhiteSpace(publicKeyPath))
    {
        Console.Error.WriteLine("Missing required argument(s). --file, --signature, and --public-key are all required.");
        return SigningExitCodes.InvalidArguments;
    }

    DetachedFileVerifier verifier = new();
    int verifyResult = verifier.VerifyFile(filePath, detachedSignaturePath, publicKeyPath, algorithm);

    if (verifyResult == SigningExitCodes.Success)
    {
        Console.WriteLine($"Signature verified for: {filePath}");
    }

    return verifyResult;
}

string? rootPath = GetArg(args, "--root");
string? privateKeyPath = GetArg(args, "--private-key");
string? manifestPath = GetArg(args, "--manifest");
string? signaturePath = GetArg(args, "--signature");
string? passphraseEnv = GetArg(args, "--passphrase-env");
string? filePathToSign = GetArg(args, "--file");
string detachedAlgorithm = GetArg(args, "--algorithm") ?? "Ed25519";

if (string.Equals(command, "sign", StringComparison.OrdinalIgnoreCase)
    && (string.IsNullOrWhiteSpace(rootPath)
        || string.IsNullOrWhiteSpace(privateKeyPath)
        || string.IsNullOrWhiteSpace(manifestPath)
        || string.IsNullOrWhiteSpace(signaturePath)))
{
    Console.Error.WriteLine("Missing required argument(s). --root, --private-key, --manifest, and --signature are all required.");
    return SigningExitCodes.InvalidArguments;
}

if (string.Equals(command, "sign", StringComparison.OrdinalIgnoreCase) && !Directory.Exists(rootPath))
{
    Console.Error.WriteLine($"[signing_unexpected_error] Resource root does not exist: {rootPath}");
    return SigningExitCodes.SigningUnexpectedError;
}

if (string.Equals(command, "sign-file", StringComparison.OrdinalIgnoreCase)
    && (string.IsNullOrWhiteSpace(filePathToSign)
        || string.IsNullOrWhiteSpace(privateKeyPath)
        || string.IsNullOrWhiteSpace(signaturePath)))
{
    Console.Error.WriteLine("Missing required argument(s). --file, --private-key, and --signature are all required.");
    return SigningExitCodes.InvalidArguments;
}

if (!string.Equals(command, "sign", StringComparison.OrdinalIgnoreCase)
    && !string.Equals(command, "sign-file", StringComparison.OrdinalIgnoreCase)
    && !string.Equals(command, "verify-file", StringComparison.OrdinalIgnoreCase))
{
    Console.Error.WriteLine("Usage: yai-resource-signer <sign|sign-file|verify-file> ...");
    return SigningExitCodes.InvalidArguments;
}

// ─── Resolve passphrase on demand ────────────────────────────────────────────

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

    if (string.Equals(command, "sign", StringComparison.OrdinalIgnoreCase))
    {
        // ─── Build manifest ───────────────────────────────────────────────────

        Console.WriteLine($"Scanning resource root: {rootPath}");
        ResourceManifestBuilder builder = new();
        string manifestRoot = rootPath!;
        string manifestOutputPath = manifestPath!;
        string manifestSignaturePath = signaturePath!;
        string signingKeyPath = privateKeyPath!;

        ResourceManifest manifest = builder.Build(manifestRoot);
        Console.WriteLine($"Found {manifest.Files.Count} file(s) to sign.");

        // ─── Sign and write ───────────────────────────────────────────────────

        ResourceManifestSigner signer = new();
        int exitCode = signer.Sign(manifest, manifestOutputPath, manifestSignaturePath, signingKeyPath, passphrase);

        if (exitCode == SigningExitCodes.SigningKeyDecryptionFailed && string.IsNullOrWhiteSpace(passphraseEnv))
        {
            if (!interactive)
            {
                Console.Error.WriteLine("[signing_key_passphrase_required] Non-interactive mode detected and the private key requires a passphrase. Use --passphrase-env.");
                return SigningExitCodes.SigningKeyPassphraseRequired;
            }

            passphrase = SecureConsolePrompt.ReadSecret("Enter YAi signing key passphrase: ");

            if (passphrase.Length == 0)
            {
                Console.Error.WriteLine("[signing_key_passphrase_required] No passphrase entered. Signing requires a passphrase for encrypted keys.");
                return SigningExitCodes.SigningKeyPassphraseRequired;
            }

            exitCode = signer.Sign(manifest, manifestOutputPath, manifestSignaturePath, signingKeyPath, passphrase);
        }

        if (exitCode == 0)
        {
            Console.WriteLine($"Manifest written to:  {manifestOutputPath}");
            Console.WriteLine($"Signature written to: {manifestSignaturePath}");
            Console.WriteLine("Signing complete.");
        }

        return exitCode;
    }

    DetachedFileSigner detachedFileSigner = new();
    int detachedExitCode = detachedFileSigner.SignFile(filePathToSign!, signaturePath!, privateKeyPath!, passphrase, detachedAlgorithm);

    if (detachedExitCode == SigningExitCodes.SigningKeyDecryptionFailed && string.IsNullOrWhiteSpace(passphraseEnv))
    {
        if (!interactive)
        {
            Console.Error.WriteLine("[signing_key_passphrase_required] Non-interactive mode detected and the private key requires a passphrase. Use --passphrase-env.");
            return SigningExitCodes.SigningKeyPassphraseRequired;
        }

        passphrase = SecureConsolePrompt.ReadSecret("Enter YAi signing key passphrase: ");

        if (passphrase.Length == 0)
        {
            Console.Error.WriteLine("[signing_key_passphrase_required] No passphrase entered. Signing requires a passphrase for encrypted keys.");
            return SigningExitCodes.SigningKeyPassphraseRequired;
        }

        detachedExitCode = detachedFileSigner.SignFile(filePathToSign!, signaturePath!, privateKeyPath!, passphrase, detachedAlgorithm);
    }

    if (detachedExitCode == SigningExitCodes.Success)
    {
        Console.WriteLine($"Signed file:      {filePathToSign}");
        Console.WriteLine($"Signature written to: {signaturePath}");
        Console.WriteLine($"Algorithm: {detachedAlgorithm}");
    }

    return detachedExitCode;
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
