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
 * Shared detached signature operations for signing and verifying release metadata.
 */

#region Using directives

using System.Security.Cryptography;

#endregion

namespace YAi.Tools.ResourceSigner.Signing;

/// <summary>
/// Shared detached signature operations used by the YAi signing tools.
/// </summary>
internal static class DetachedSignatureOperations
{
    /// <summary>
    /// Signs the provided payload with the requested algorithm.
    /// </summary>
    /// <param name="algorithm">The signing algorithm.</param>
    /// <param name="privateKeyPem">PEM-encoded private key text.</param>
    /// <param name="passphrase">Optional passphrase for encrypted PEM keys.</param>
    /// <param name="data">Payload bytes to sign.</param>
    /// <param name="signature">The generated signature bytes on success.</param>
    /// <param name="errorCode">Structured error code on failure.</param>
    /// <param name="errorMessage">Human-readable error message on failure.</param>
    /// <returns>Zero on success; otherwise a <see cref="SigningExitCodes"/> value.</returns>
    internal static int TrySign (
        string algorithm,
        string privateKeyPem,
        char[]? passphrase,
        byte[] data,
        out byte[] signature,
        out string errorCode,
        out string errorMessage)
    {
        signature = [];
        errorCode = string.Empty;
        errorMessage = string.Empty;

        if (string.Equals (algorithm, "Ed25519", StringComparison.OrdinalIgnoreCase))
        {
            return TrySignEd25519 (
                privateKeyPem,
                passphrase,
                data,
                out signature,
                out errorCode,
                out errorMessage);
        }

        if (string.Equals (algorithm, "RSA-PSS-SHA256", StringComparison.OrdinalIgnoreCase))
        {
            return TrySignRsaPss (
                privateKeyPem,
                passphrase,
                data,
                out signature,
                out errorCode,
                out errorMessage);
        }

        errorCode = "signing_algorithm_unsupported";
        errorMessage = $"Algorithm '{algorithm}' is not supported.";

        return SigningExitCodes.SigningAlgorithmUnsupported;
    }

    /// <summary>
    /// Verifies the provided signature against the supplied payload.
    /// </summary>
    /// <param name="algorithm">The signing algorithm.</param>
    /// <param name="publicKeyPem">PEM-encoded public key text.</param>
    /// <param name="data">Payload bytes to verify.</param>
    /// <param name="signature">Detached signature bytes.</param>
    /// <param name="errorCode">Structured error code on failure.</param>
    /// <param name="errorMessage">Human-readable error message on failure.</param>
    /// <returns>Zero on success; otherwise a <see cref="SigningExitCodes"/> value.</returns>
    internal static int TryVerify (
        string algorithm,
        string publicKeyPem,
        byte[] data,
        byte[] signature,
        out string errorCode,
        out string errorMessage)
    {
        errorCode = string.Empty;
        errorMessage = string.Empty;

        if (string.Equals (algorithm, "Ed25519", StringComparison.OrdinalIgnoreCase))
        {
            return TryVerifyEd25519 (
                publicKeyPem,
                data,
                signature,
                out errorCode,
                out errorMessage);
        }

        if (string.Equals (algorithm, "RSA-PSS-SHA256", StringComparison.OrdinalIgnoreCase))
        {
            return TryVerifyRsaPss (
                publicKeyPem,
                data,
                signature,
                out errorCode,
                out errorMessage);
        }

        errorCode = "signing_algorithm_unsupported";
        errorMessage = $"Algorithm '{algorithm}' is not supported.";

        return SigningExitCodes.SigningAlgorithmUnsupported;
    }

    private static int TrySignEd25519 (
        string pem,
        char[]? passphrase,
        byte[] data,
        out byte[] signature,
        out string errorCode,
        out string errorMessage)
    {
        signature = [];
        errorCode = string.Empty;
        errorMessage = string.Empty;

        try
        {
            using ECDsa ecdsa = ECDsa.Create ();
            ImportPrivateKey (ecdsa, pem, passphrase);

            signature = ecdsa.SignData (
                data,
                HashAlgorithmName.SHA512,
                DSASignatureFormat.IeeeP1363FixedFieldConcatenation);

            return SigningExitCodes.Success;
        }
        catch (CryptographicException ex)
        {
            return MapSigningCryptographicError (ex, out errorCode, out errorMessage);
        }
        catch (Exception ex)
        {
            return MapSigningException (ex, out errorCode, out errorMessage);
        }
    }

    private static int TrySignRsaPss (
        string pem,
        char[]? passphrase,
        byte[] data,
        out byte[] signature,
        out string errorCode,
        out string errorMessage)
    {
        signature = [];
        errorCode = string.Empty;
        errorMessage = string.Empty;

        try
        {
            using RSA rsa = RSA.Create ();
            ImportPrivateKey (rsa, pem, passphrase);

            byte[] hash = SHA256.HashData (data);
            signature = rsa.SignHash (hash, HashAlgorithmName.SHA256, RSASignaturePadding.Pss);

            return SigningExitCodes.Success;
        }
        catch (CryptographicException ex)
        {
            return MapSigningCryptographicError (ex, out errorCode, out errorMessage);
        }
        catch (Exception ex)
        {
            return MapSigningException (ex, out errorCode, out errorMessage);
        }
    }

    private static int TryVerifyEd25519 (
        string publicKeyPem,
        byte[] data,
        byte[] signature,
        out string errorCode,
        out string errorMessage)
    {
        errorCode = string.Empty;
        errorMessage = string.Empty;

        try
        {
            using ECDsa ecdsa = ECDsa.Create ();
            ecdsa.ImportFromPem (publicKeyPem);

            bool verified = ecdsa.VerifyData (
                data,
                signature,
                HashAlgorithmName.SHA512,
                DSASignatureFormat.IeeeP1363FixedFieldConcatenation);

            if (verified)
            {
                return SigningExitCodes.Success;
            }

            errorCode = "signing_signature_invalid";
            errorMessage = "Signature did not verify against the public key.";

            return SigningExitCodes.SigningSignatureInvalid;
        }
        catch (CryptographicException ex)
        {
            errorCode = "signing_unexpected_error";
            errorMessage = $"Cryptographic error: {ex.Message}";

            return SigningExitCodes.SigningUnexpectedError;
        }
        catch (Exception ex)
        {
            errorCode = "signing_unexpected_error";
            errorMessage = ex.Message;

            return SigningExitCodes.SigningUnexpectedError;
        }
    }

    private static int TryVerifyRsaPss (
        string publicKeyPem,
        byte[] data,
        byte[] signature,
        out string errorCode,
        out string errorMessage)
    {
        errorCode = string.Empty;
        errorMessage = string.Empty;

        try
        {
            using RSA rsa = RSA.Create ();
            rsa.ImportFromPem (publicKeyPem);

            byte[] hash = SHA256.HashData (data);
            bool verified = rsa.VerifyHash (hash, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pss);

            if (verified)
            {
                return SigningExitCodes.Success;
            }

            errorCode = "signing_signature_invalid";
            errorMessage = "Signature did not verify against the public key.";

            return SigningExitCodes.SigningSignatureInvalid;
        }
        catch (CryptographicException ex)
        {
            errorCode = "signing_unexpected_error";
            errorMessage = $"Cryptographic error: {ex.Message}";

            return SigningExitCodes.SigningUnexpectedError;
        }
        catch (Exception ex)
        {
            errorCode = "signing_unexpected_error";
            errorMessage = ex.Message;

            return SigningExitCodes.SigningUnexpectedError;
        }
    }

    private static int MapSigningCryptographicError (
        CryptographicException exception,
        out string errorCode,
        out string errorMessage)
    {
        if (IsPassphraseOrDecryptionFailure (exception.Message))
        {
            errorCode = "signing_key_decryption_failed";
            errorMessage = "Failed to decrypt private key. Check your passphrase.";

            return SigningExitCodes.SigningKeyDecryptionFailed;
        }

        errorCode = "signing_unexpected_error";
        errorMessage = $"Cryptographic error: {exception.Message}";

        return SigningExitCodes.SigningUnexpectedError;
    }

    private static int MapSigningException (
        Exception exception,
        out string errorCode,
        out string errorMessage)
    {
        if (IsPassphraseOrDecryptionFailure (exception.Message))
        {
            errorCode = "signing_key_decryption_failed";
            errorMessage = "Failed to decrypt private key. Check your passphrase.";

            return SigningExitCodes.SigningKeyDecryptionFailed;
        }

        errorCode = "signing_unexpected_error";
        errorMessage = exception.Message;

        return SigningExitCodes.SigningUnexpectedError;
    }

    private static bool IsPassphraseOrDecryptionFailure (string? message)
    {
        if (string.IsNullOrWhiteSpace (message))
        {
            return false;
        }

        return message.Contains ("passphrase", StringComparison.OrdinalIgnoreCase)
            || message.Contains ("password", StringComparison.OrdinalIgnoreCase)
            || message.Contains ("decrypt", StringComparison.OrdinalIgnoreCase)
            || message.Contains ("encrypted key", StringComparison.OrdinalIgnoreCase)
            || message.Contains ("ImportFromEncryptedPem", StringComparison.OrdinalIgnoreCase);
    }

    private static void ImportPrivateKey (AsymmetricAlgorithm algorithm, string pem, char[]? passphrase)
    {
        if (algorithm is ECDsa ecdsa)
        {
            if (passphrase is { Length: > 0 })
            {
                string pass = new (passphrase);
                try
                {
                    ecdsa.ImportFromEncryptedPem (pem, pass);
                }
                finally
                {
                    pass = string.Empty;
                }

                return;
            }

            ecdsa.ImportFromPem (pem);

            return;
        }

        if (algorithm is RSA rsa)
        {
            if (passphrase is { Length: > 0 })
            {
                string pass = new (passphrase);
                try
                {
                    rsa.ImportFromEncryptedPem (pem, pass);
                }
                finally
                {
                    pass = string.Empty;
                }

                return;
            }

            rsa.ImportFromPem (pem);
        }
    }
}