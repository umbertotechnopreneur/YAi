/*
 * YAi!
 *
 * Copyright © 2019-2026 UmbertoGiacobbiDotBiz. All rights reserved.
 * Website: https://umbertogiacobbi.biz
 * Email: hello@umbertogiacobbi.biz
 *
 * This file is part of YAi!.
 *
 * YAi.Persona.Tests
 * Focused tests for detached file signing and verification.
 */

#region Using directives

using System.Security.Cryptography;
using YAi.Tools.ResourceSigner.Signing;

#endregion

namespace YAi.Persona.Tests;

/// <summary>
/// Focused tests for detached file signing and verification.
/// </summary>
public sealed class DetachedFileSignerTests : IDisposable
{
    #region Fields

    private readonly string _root;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="DetachedFileSignerTests"/> class.
    /// </summary>
    public DetachedFileSignerTests ()
    {
        _root = Path.Combine (Path.GetTempPath (), "yai-detached-sign-" + Guid.NewGuid ().ToString ("N"));
        Directory.CreateDirectory (_root);
    }

    #endregion

    #region Tests

    /// <summary>
    /// Signing a file writes a detached signature that verifies successfully.
    /// </summary>
    [Fact]
    public void SignFile_WritesDetachedSignature_AndVerifySucceeds ()
    {
        string filePath = Path.Combine (_root, "release-manifest.json");
        string signaturePath = Path.Combine (_root, "release-manifest.json.sig");
        string privateKeyPath = Path.Combine (_root, "test-private.pem");
        string publicKeyPath = Path.Combine (_root, "public-key.yai.pem");

        File.WriteAllText (filePath, "{\"version\":\"1.0.0\",\"artifacts\":[]}");

        using RSA rsa = RSA.Create (2048);
        File.WriteAllText (privateKeyPath, rsa.ExportPkcs8PrivateKeyPem ());
        File.WriteAllText (publicKeyPath, rsa.ExportSubjectPublicKeyInfoPem ());

        DetachedFileSigner signer = new ();
        int signExitCode = signer.SignFile (filePath, signaturePath, privateKeyPath, passphrase: null, "RSA-PSS-SHA256");

        Assert.Equal (SigningExitCodes.Success, signExitCode);
        Assert.True (File.Exists (signaturePath));

        DetachedFileVerifier verifier = new ();
        int verifyExitCode = verifier.VerifyFile (filePath, signaturePath, publicKeyPath, "RSA-PSS-SHA256");

        Assert.Equal (SigningExitCodes.Success, verifyExitCode);
    }

    /// <summary>
    /// Modifying a signed file causes detached signature verification to fail.
    /// </summary>
    [Fact]
    public void VerifyFile_TamperedPayload_ReturnsSignatureInvalid ()
    {
        string filePath = Path.Combine (_root, "checksums.sha256");
        string signaturePath = Path.Combine (_root, "checksums.sha256.sig");
        string privateKeyPath = Path.Combine (_root, "test-private.pem");
        string publicKeyPath = Path.Combine (_root, "public-key.yai.pem");

        File.WriteAllText (filePath, "abcdef1234567890 *artifact.zip");

        using RSA rsa = RSA.Create (2048);
        File.WriteAllText (privateKeyPath, rsa.ExportPkcs8PrivateKeyPem ());
        File.WriteAllText (publicKeyPath, rsa.ExportSubjectPublicKeyInfoPem ());

        DetachedFileSigner signer = new ();
        int signExitCode = signer.SignFile (filePath, signaturePath, privateKeyPath, passphrase: null, "RSA-PSS-SHA256");

        Assert.Equal (SigningExitCodes.Success, signExitCode);

        File.WriteAllText (filePath, "ffffffffffffffff *artifact.zip");

        DetachedFileVerifier verifier = new ();
        int verifyExitCode = verifier.VerifyFile (filePath, signaturePath, publicKeyPath, "RSA-PSS-SHA256");

        Assert.Equal (SigningExitCodes.SigningSignatureInvalid, verifyExitCode);
    }

    #endregion

    #region IDisposable

    /// <summary>
    /// Removes the temporary test directory.
    /// </summary>
    public void Dispose ()
    {
        if (!Directory.Exists (_root))
        {
            return;
        }

        Directory.Delete (_root, recursive: true);
    }

    #endregion
}