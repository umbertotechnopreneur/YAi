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
 * Hidden console passphrase input helper
 */

using System.Text;

namespace YAi.Persona.Services.Security.AppLock;

/// <summary>
/// Reads passphrases from the console without echoing the input.
/// </summary>
public static class SecureSecretReader
{
    /// <summary>
    /// Reads a hidden passphrase from the current console.
    /// </summary>
    /// <param name="prompt">Prompt text shown before input starts.</param>
    /// <returns>The entered passphrase characters.</returns>
    public static char[] ReadHiddenPassphrase(string prompt)
    {
        if (Console.IsInputRedirected || Console.IsOutputRedirected)
        {
            Console.Write(prompt);
            string? redirected = Console.ReadLine();
            return string.IsNullOrEmpty(redirected) ? [] : redirected.ToCharArray();
        }

        Console.Write(prompt);

        List<char> buffer = [];

        while (true)
        {
            ConsoleKeyInfo key = Console.ReadKey(intercept: true);

            if (key.Key == ConsoleKey.Enter)
            {
                Console.WriteLine();
                break;
            }

            if (key.Key == ConsoleKey.Backspace)
            {
                if (buffer.Count > 0)
                {
                    buffer.RemoveAt(buffer.Count - 1);
                }

                continue;
            }

            if (key.Key == ConsoleKey.Escape)
            {
                buffer.Clear();
                Console.WriteLine();
                break;
            }

            if (!char.IsControl(key.KeyChar))
            {
                buffer.Add(key.KeyChar);
            }
        }

        return buffer.ToArray();
    }

    /// <summary>
    /// Clears a passphrase buffer in place.
    /// </summary>
    /// <param name="buffer">Buffer to clear.</param>
    public static void Clear(char[]? buffer)
    {
        if (buffer is null)
        {
            return;
        }

        Array.Clear(buffer, 0, buffer.Length);
    }
}