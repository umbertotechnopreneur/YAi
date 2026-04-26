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
 * Signs arbitrary files with the maintainer's private key.
 */

#region Using directives

using System.Text;

#endregion

namespace YAi.Tools.ResourceSigner.Signing;

/// <summary>
/// Creates detached signatures for arbitrary files.
/// </summary>
public sealed class DetachedFileSigner
{
    /// <summary>
    /// Signs the specified file and writes a detached base-64 signature.
    /// </summary>
    /// <param name="filePath">Path to the file being signed.</param>
    /// <param name="signaturePath">Output path for the detached signature file.</param>
    /// <param name="privateKeyPath">Path to the PEM-encoded private key.</param>
    /// <param name="passphrase">Optional passphrase for an encrypted private key.</param>
    /// <param name="algorithm">Signature algorithm to use.</param>
    /// <returns>Zero on success; otherwise a <see cref="SigningExitCodes"/> value.</returns>
    public int SignFile (
        string filePath,
        string signaturePath,
        string privateKeyPath,
        char[]? passphrase,
        string algorithm)
    {
        if (!File.Exists (filePath))
        {
            Console.Error.WriteLine ($"[signing_file_missing] File to sign not found: {filePath}");

            return SigningExitCodes.SigningFileMissing;
        }

        if (!File.Exists (privateKeyPath))
        {
            Console.Error.WriteLine ($"[signing_key_missing] Private key not found: {privateKeyPath}");

            return SigningExitCodes.SigningKeyMissing;
        }

        string privateKeyPem;
        byte[] fileBytes;

        try
        {
            privateKeyPem = File.ReadAllText (privateKeyPath);
            fileBytes = File.ReadAllBytes (filePath);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine ($"[signing_file_read_failed] Failed to read signing inputs: {ex.Message}");

            return SigningExitCodes.SigningFileReadFailed;
        }

        byte[] signature;
        string errorCode;
        string errorMessage;
        int signResult = DetachedSignatureOperations.TrySign (
            algorithm,
            privateKeyPem,
            passphrase,
            fileBytes,
            out signature,
            out errorCode,
            out errorMessage);

        if (signResult != SigningExitCodes.Success)
        {
            Console.Error.WriteLine ($"[{errorCode}] {errorMessage}");

            return signResult;
        }

        try
        {
            File.WriteAllText (
                signaturePath,
                Convert.ToBase64String (signature),
                new UTF8Encoding (encoderShouldEmitUTF8Identifier: false));
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine ($"[signing_signature_write_failed] Failed to write signature: {ex.Message}");

            return SigningExitCodes.SigningSignatureWriteFailed;
        }

        return SigningExitCodes.Success;
    }
}