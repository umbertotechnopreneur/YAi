/*
 * YAi!
 *
 * Copyright © 2019-2026 UmbertoGiacobbiDotBiz. All rights reserved.
 * Website: https://umbertogiacobbi.biz
 * Email: hello@umbertogiacobbi.biz
 *
 * This file is part of YAi!.
 *
 * YAi! is free software: you can redistribute it and/or modify it under the terms
 * of the GNU Affero General Public License version 3 as published by the Free
 * Software Foundation.
 *
 * YAi! is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY;
 * without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR
 * PURPOSE. See the GNU Affero General Public License for more details.
 *
 * You should have received a copy of the GNU Affero General Public License along
 * with YAi!. If not, see <https://www.gnu.org/licenses/>.
 *
 * YAi!
 * Windows DPAPI secret protector
 */

using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace YAi.Persona.Services.Security.Secrets;

/// <summary>
/// Protects secrets using Windows DPAPI current-user scope.
/// </summary>
public sealed class WindowsDpapiSecretProtector : ISecretProtector
{
    private const string ProtectorName = "WindowsDpapiCurrentUser";
    private const int CryptProtectUiForbidden = 0x1;

    /// <inheritdoc />
    public string Name => ProtectorName;

    /// <inheritdoc />
    public SecretProtectionResult Protect(byte[] plaintext, IReadOnlyDictionary<string, string>? metadata = null)
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException("Windows DPAPI is only available on Windows.");
        }

        if (plaintext is null)
        {
            throw new ArgumentNullException(nameof(plaintext));
        }

        byte[] protectedBytes = ProtectWithDpapi(plaintext);

        return new SecretProtectionResult
        {
            Protector = ProtectorName,
            CiphertextBase64 = Convert.ToBase64String(protectedBytes),
            Metadata = metadata is null ? [] : new Dictionary<string, string>(metadata, StringComparer.OrdinalIgnoreCase),
            CreatedAtUtc = DateTimeOffset.UtcNow.ToString("O")
        };
    }

    /// <inheritdoc />
    public bool TryUnprotect(SecretProtectionResult payload, out byte[] plaintext)
    {
        plaintext = [];

        if (!OperatingSystem.IsWindows())
        {
            return false;
        }

        if (payload is null || !string.Equals(payload.Protector, ProtectorName, StringComparison.Ordinal))
        {
            return false;
        }

        try
        {
            byte[] protectedBytes = Convert.FromBase64String(payload.CiphertextBase64);
            plaintext = UnprotectWithDpapi(protectedBytes);
            return true;
        }
        catch
        {
            plaintext = [];
            return false;
        }
    }

    private static byte[] ProtectWithDpapi(byte[] plaintext)
    {
        DATA_BLOB input = new (plaintext);
        DATA_BLOB output = default;

        try
        {
            if (!CryptProtectData(ref input, null, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, CryptProtectUiForbidden, out output))
            {
                throw new CryptographicException($"CryptProtectData failed with Win32 error {Marshal.GetLastWin32Error()}.");
            }

            return output.ToArray ();
        }
        finally
        {
            input.Dispose ();
            output.Dispose ();
        }
    }

    private static byte[] UnprotectWithDpapi(byte[] protectedBytes)
    {
        DATA_BLOB input = new (protectedBytes);
        DATA_BLOB output = default;

        try
        {
            if (!CryptUnprotectData(ref input, out string? _, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, CryptProtectUiForbidden, out output))
            {
                throw new CryptographicException($"CryptUnprotectData failed with Win32 error {Marshal.GetLastWin32Error()}.");
            }

            return output.ToArray ();
        }
        finally
        {
            input.Dispose ();
            output.Dispose ();
        }
    }

    [DllImport("crypt32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool CryptProtectData(
        ref DATA_BLOB pDataIn,
        string? szDataDescr,
        IntPtr optionalEntropy,
        IntPtr pvReserved,
        IntPtr pPromptStruct,
        int dwFlags,
        out DATA_BLOB pDataOut);

    [DllImport("crypt32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool CryptUnprotectData(
        ref DATA_BLOB pDataIn,
        out string? ppszDataDescr,
        IntPtr optionalEntropy,
        IntPtr pvReserved,
        IntPtr pPromptStruct,
        int dwFlags,
        out DATA_BLOB pDataOut);

    [StructLayout(LayoutKind.Sequential)]
    private struct DATA_BLOB : IDisposable
    {
        public int cbData;
        public IntPtr pbData;

        public DATA_BLOB(byte[] data)
        {
            cbData = data.Length;
            pbData = Marshal.AllocHGlobal(data.Length);
            Marshal.Copy(data, 0, pbData, data.Length);
        }

        public byte[] ToArray()
        {
            byte[] data = new byte [cbData];
            Marshal.Copy(pbData, data, 0, cbData);
            return data;
        }

        public void Dispose()
        {
            if (pbData == IntPtr.Zero)
            {
                return;
            }

            try
            {
                byte[] zeros = new byte [cbData];
                Marshal.Copy(zeros, 0, pbData, cbData);
            }
            catch
            {
            }

            Marshal.FreeHGlobal(pbData);
            pbData = IntPtr.Zero;
            cbData = 0;
        }
    }
}