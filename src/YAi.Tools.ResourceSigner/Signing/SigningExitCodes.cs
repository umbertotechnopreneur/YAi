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
 * Structured exit codes for signing operations.
 */

namespace YAi.Tools.ResourceSigner.Signing;

/// <summary>
/// Non-zero exit codes returned by the resource signer on failure.
/// </summary>
public static class SigningExitCodes
{
    public const int Success = 0;
    public const int SigningKeyMissing = 10;
    public const int SigningKeyPassphraseRequired = 11;
    public const int SigningKeyPassphraseInvalid = 12;
    public const int SigningKeyDecryptionFailed = 13;
    public const int SigningAlgorithmUnsupported = 14;
    public const int SigningManifestWriteFailed = 15;
    public const int SigningSignatureWriteFailed = 16;
    public const int SigningSignatureInvalid = 17;
    public const int SigningPublicKeyMissing = 18;
    public const int SigningFileMissing = 19;
    public const int SigningFileReadFailed = 20;
    public const int SigningSignatureReadFailed = 21;
    public const int SigningUnexpectedError = 99;
    public const int InvalidArguments = 1;
}
