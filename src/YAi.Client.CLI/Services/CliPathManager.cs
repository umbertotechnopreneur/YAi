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
 * PATH inspection and registration helpers
 */

#region Using directives

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

#endregion

namespace YAi.Client.CLI.Services;

/// <summary>
/// Handles inspection and Windows user PATH registration for the YAi! CLI executable directory.
/// </summary>
internal static class CliPathManager
{
	#region Fields

	private static readonly string CliExecutableBaseName = ResolveCliExecutableBaseName ();
	private const string PathEnvironmentVariable = "PATH";

	#endregion

	#region Public methods

	/// <summary>
	/// Gets the current CLI executable directory matches from the current user PATH and the inherited process PATH.
	/// </summary>
	/// <returns>A tuple with the current CLI matches and the resolved executable and directory paths.</returns>
	public static (IReadOnlyList<string> UserMatches, IReadOnlyList<string> ProcessMatches, string CurrentExecutablePath, string CurrentDirectory) GetCliPathStatus()
	{
		string currentExecutablePath = GetCurrentCliExecutablePath();
		string currentDirectory = GetCurrentCliDirectory();

		string? userPath = OperatingSystem.IsWindows()
			? Environment.GetEnvironmentVariable(PathEnvironmentVariable, EnvironmentVariableTarget.User)
			: null;

		string? processPath = Environment.GetEnvironmentVariable(PathEnvironmentVariable);

		IReadOnlyList<string> userMatches = FindCliExecutableMatches(userPath);
		IReadOnlyList<string> processMatches = FindCliExecutableMatches(processPath);

		return (userMatches, processMatches, currentExecutablePath, currentDirectory);
	}

	/// <summary>
	/// Adds or updates the current CLI directory in the current user's PATH.
	/// </summary>
	/// <returns>The original and updated user PATH values together with any removed stale entries.</returns>
	/// <exception cref="YAiPlatformNotSupporetedException">Thrown when the platform is not Windows.</exception>
	public static (string OriginalUserPath, string UpdatedUserPath, IReadOnlyList<string> RemovedEntries, string CurrentDirectory, string CurrentExecutablePath) AddOrUpdateCurrentCliDirectoryOnUserPath()
	{
		EnsureWindowsPlatform();

		string currentExecutablePath = GetCurrentCliExecutablePath();
		string currentDirectory = GetCurrentCliDirectory();
		string? originalUserPath = Environment.GetEnvironmentVariable(PathEnvironmentVariable, EnvironmentVariableTarget.User);
		IReadOnlyList<string> originalEntries = SplitPathEntries(originalUserPath);

		List<string> updatedEntries = [];
		List<string> removedEntries = [];
		bool insertedCurrentDirectory = false;

		foreach (string rawEntry in originalEntries)
		{
			if (TryNormalizePathEntry(rawEntry, out string? normalizedEntry))
			{
				if (PathsEqual(normalizedEntry, currentDirectory))
				{
					if (!insertedCurrentDirectory)
					{
						updatedEntries.Add(currentDirectory);
						insertedCurrentDirectory = true;
					}
					else
					{
						removedEntries.Add(rawEntry);
					}

					continue;
				}

				if (ContainsCliExecutable(normalizedEntry))
				{
					removedEntries.Add(rawEntry);

					if (!insertedCurrentDirectory)
					{
						updatedEntries.Add(currentDirectory);
						insertedCurrentDirectory = true;
					}

					continue;
				}
			}

			updatedEntries.Add(rawEntry);
		}

		if (!insertedCurrentDirectory)
		{
			updatedEntries.Add(currentDirectory);
		}

		string updatedUserPath = string.Join(Path.PathSeparator, updatedEntries);

		Environment.SetEnvironmentVariable(PathEnvironmentVariable, updatedUserPath, EnvironmentVariableTarget.User);
		UpdateProcessPathMirror(updatedUserPath);

		return (originalUserPath ?? string.Empty, updatedUserPath, removedEntries, currentDirectory, currentExecutablePath);
	}

	#endregion

	#region Private helpers

	private static string ResolveCliExecutableBaseName ()
	{
		string? assemblyName = typeof (CliPathManager).Assembly.GetName ().Name;

		if (!string.IsNullOrWhiteSpace (assemblyName))
		{
			return assemblyName;
		}

		return "YAi.Client.CLI";
	}

	private static void EnsureWindowsPlatform()
	{
		if (OperatingSystem.IsWindows())
		{
			return;
		}

		throw new YAiPlatformNotSupporetedException();
	}

	private static void UpdateProcessPathMirror(string updatedUserPath)
	{
		string? machinePath = Environment.GetEnvironmentVariable(PathEnvironmentVariable, EnvironmentVariableTarget.Machine);

		string updatedProcessPath = string.Join(
			Path.PathSeparator,
			new[] { machinePath, updatedUserPath }.Where(segment => !string.IsNullOrWhiteSpace(segment)));

		Environment.SetEnvironmentVariable(PathEnvironmentVariable, updatedProcessPath);
	}

	private static IReadOnlyList<string> FindCliExecutableMatches(string? pathValue)
	{
		if (string.IsNullOrWhiteSpace(pathValue))
		{
			return [];
		}

		HashSet<string> matches = new(GetPathComparer());

		foreach (string rawEntry in SplitPathEntries(pathValue))
		{
			if (!TryNormalizePathEntry(rawEntry, out string? normalizedEntry))
			{
				continue;
			}

			if (ContainsCliExecutable(normalizedEntry))
			{
				matches.Add(normalizedEntry);
			}
		}

		return matches.ToArray();
	}

	private static bool ContainsCliExecutable(string directoryPath)
	{
		string candidatePath = Path.Combine(directoryPath, GetExecutableFileName());

		return File.Exists(candidatePath);
	}

	private static string GetCurrentCliDirectory()
	{
		string? processPath = Environment.ProcessPath;
		if (!string.IsNullOrWhiteSpace(processPath))
		{
			string processFileName = Path.GetFileName(processPath);
			if (!IsDotNetHost(processFileName))
			{
				string? processDirectory = Path.GetDirectoryName(processPath);
				string? normalizedProcessDirectory = NormalizeDirectoryPath(processDirectory);

				if (!string.IsNullOrWhiteSpace(normalizedProcessDirectory))
				{
					return normalizedProcessDirectory;
				}
			}
		}

		string? normalizedBaseDirectory = NormalizeDirectoryPath(AppContext.BaseDirectory);

		return normalizedBaseDirectory ?? AppContext.BaseDirectory;
	}

	private static string GetCurrentCliExecutablePath()
	{
		string currentDirectory = GetCurrentCliDirectory();

		return Path.GetFullPath(Path.Combine(currentDirectory, GetExecutableFileName()));
	}

	private static string GetExecutableFileName()
	{
		return OperatingSystem.IsWindows()
			? $"{CliExecutableBaseName}.exe"
			: CliExecutableBaseName;
	}

	private static IEqualityComparer<string> GetPathComparer()
	{
		return OperatingSystem.IsWindows()
			? StringComparer.OrdinalIgnoreCase
			: StringComparer.Ordinal;
	}

	private static bool IsDotNetHost(string? fileName)
	{
		if (string.IsNullOrWhiteSpace(fileName))
		{
			return false;
		}

		return string.Equals(fileName, "dotnet", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(fileName, "dotnet.exe", StringComparison.OrdinalIgnoreCase);
	}

	private static IReadOnlyList<string> SplitPathEntries(string? pathValue)
	{
		if (string.IsNullOrWhiteSpace(pathValue))
		{
			return [];
		}

		return pathValue.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);
	}

	private static string? NormalizeDirectoryPath(string? path)
	{
		if (string.IsNullOrWhiteSpace(path))
		{
			return null;
		}

		string trimmedPath = path.Trim().Trim('"');
		string expandedPath = Environment.ExpandEnvironmentVariables(trimmedPath);

		if (!Path.IsPathRooted(expandedPath))
		{
			return null;
		}

		try
		{
			string fullPath = Path.GetFullPath(expandedPath);

			return fullPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
		}
		catch
		{
			return null;
		}
	}

	private static bool PathsEqual(string leftPath, string rightPath)
	{
		return GetPathComparer().Equals(leftPath, rightPath);
	}

	private static bool TryNormalizePathEntry(string rawEntry, out string normalizedEntry)
	{
		string? normalizedPath = NormalizeDirectoryPath(rawEntry);
		if (normalizedPath is null)
		{
			normalizedEntry = string.Empty;
			return false;
		}

		normalizedEntry = normalizedPath;

		return true;
	}

	#endregion
}