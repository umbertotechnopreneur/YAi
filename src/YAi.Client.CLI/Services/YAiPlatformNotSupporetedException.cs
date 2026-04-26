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
 * YAi.Client.CLI
 * Exception used when a CLI operation is not supported on the current platform
 */

namespace YAi.Client.CLI.Services;

/// <summary>
/// Thrown when a CLI operation is only available on Windows.
/// </summary>
public sealed class YAiPlatformNotSupporetedException : PlatformNotSupportedException
{
	private const string DefaultMessage = "YAi! PATH registration is only available on Windows. This feature is not available on macOS/Linux.";

	/// <summary>
	/// Initializes a new instance of the <see cref="YAiPlatformNotSupporetedException"/> class.
	/// </summary>
	public YAiPlatformNotSupporetedException()
		: base(DefaultMessage)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="YAiPlatformNotSupporetedException"/> class with a custom message.
	/// </summary>
	/// <param name="message">The exception message.</param>
	public YAiPlatformNotSupporetedException(string message)
		: base(message)
	{
	}
}