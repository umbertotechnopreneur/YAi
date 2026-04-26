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
 * Verifies detached signatures for arbitrary files.
 */

namespace YAi.Tools.ResourceSigner.Signing;

/// <summary>
/// Verifies detached signatures for arbitrary files.
/// </summary>
public sealed class DetachedFileVerifier
{
    /// <summary>
    /// Verifies the specified detached signature against a file and public key.
    /// </summary>
    /// <param name="filePath">Path to the file being verified.</param>
    /// <param name="signaturePath">Path to the detached signature file.</param>
    /// <param name="publicKeyPath">Path to the PEM-encoded public key.</param>
    /// <param name="algorithm">Signature algorithm to use.</param>
    /// <returns>Zero on success; otherwise a <see cref="SigningExitCodes"/> value.</returns>
    public int VerifyFile (
        string filePath,
        string signaturePath,
        string publicKeyPath,
        string algorithm)
    {
        if (!File.Exists (filePath))
        {
            Console.Error.WriteLine ($"[signing_file_missing] File to verify not found: {filePath}");

            return SigningExitCodes.SigningFileMissing;
        }

        if (!File.Exists (signaturePath))
        {
            Console.Error.WriteLine ($"[signing_signature_read_failed] Signature file not found: {signaturePath}");

            return SigningExitCodes.SigningSignatureReadFailed;
        }

        if (!File.Exists (publicKeyPath))
        {
            Console.Error.WriteLine ($"[signing_public_key_missing] Public key not found: {publicKeyPath}");

            return SigningExitCodes.SigningPublicKeyMissing;
        }

        byte[] fileBytes;
        byte[] signatureBytes;
        string publicKeyPem;

        try
        {
            fileBytes = File.ReadAllBytes (filePath);
            string signatureText = File.ReadAllText (signaturePath).Trim ();
            signatureBytes = Convert.FromBase64String (signatureText);
            publicKeyPem = File.ReadAllText (publicKeyPath);
        }
        catch (FormatException ex)
        {
            Console.Error.WriteLine ($"[signing_signature_read_failed] Failed to decode signature: {ex.Message}");

            return SigningExitCodes.SigningSignatureReadFailed;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine ($"[signing_file_read_failed] Failed to read verification inputs: {ex.Message}");

            return SigningExitCodes.SigningFileReadFailed;
        }

        string errorCode;
        string errorMessage;
        int verifyResult = DetachedSignatureOperations.TryVerify (
            algorithm,
            publicKeyPem,
            fileBytes,
            signatureBytes,
            out errorCode,
            out errorMessage);

        if (verifyResult != SigningExitCodes.Success)
        {
            Console.Error.WriteLine ($"[{errorCode}] {errorMessage}");

            return verifyResult;
        }

        return SigningExitCodes.Success;
    }
}