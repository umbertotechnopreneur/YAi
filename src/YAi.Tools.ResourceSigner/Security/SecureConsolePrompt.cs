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
 * Secure passphrase prompt — never echoes input.
 */

namespace YAi.Tools.ResourceSigner.Security;

/// <summary>
/// Provides a secure console prompt that reads a secret passphrase
/// without echoing characters to the terminal.
/// </summary>
public static class SecureConsolePrompt
{
    /// <summary>
    /// Writes <paramref name="prompt"/> to the console and reads characters
    /// without echoing them. Backspace is supported. Returns on Enter.
    /// </summary>
    /// <param name="prompt">The prompt text to display.</param>
    /// <returns>The entered characters as a <see cref="char"/> array. Caller is responsible for clearing the array after use.</returns>
    public static char[] ReadSecret(string prompt)
    {
        Console.Write(prompt);

        List<char> chars = new(64);

        while (true)
        {
            ConsoleKeyInfo key = Console.ReadKey(intercept: true);

            if (key.Key == ConsoleKey.Enter)
                break;

            if (key.Key == ConsoleKey.Backspace)
            {
                if (chars.Count > 0)
                    chars.RemoveAt(chars.Count - 1);

                continue;
            }

            // Ignore control characters
            if (key.KeyChar == '\0')
                continue;

            chars.Add(key.KeyChar);
        }

        Console.WriteLine();

        return chars.ToArray();
    }

    /// <summary>
    /// Overwrites all elements of <paramref name="buffer"/> with <c>'\0'</c>.
    /// </summary>
    public static void Clear(char[]? buffer)
    {
        if (buffer is null)
            return;

        Array.Clear(buffer, 0, buffer.Length);
    }
}
