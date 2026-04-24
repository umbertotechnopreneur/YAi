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
 * Conversation history management
 */

using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using YAi.Persona.Models;

namespace YAi.Persona.Services
{
    public sealed class HistoryService
    {
        private readonly AppPaths _paths;
        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions { WriteIndented = true };

        public HistoryService(AppPaths paths)
        {
            _paths = paths ?? throw new ArgumentNullException(nameof(paths));
        }

        public void SaveEntry(HistoryEntry entry)
        {
            Directory.CreateDirectory(_paths.HistoryRoot);

            var sessionFile = Path.Combine(_paths.HistoryRoot, entry.Id + ".json");
            var json = JsonSerializer.Serialize(entry, _jsonOptions);
            var bytes = System.Text.Encoding.UTF8.GetBytes(json);
            AtomicFileWriter.WriteAtomic(sessionFile, bytes);
        }

        public void SaveChatSession(ChatSession session)
        {
            Directory.CreateDirectory(_paths.HistoryRoot);

            var sessionFile = Path.Combine(_paths.HistoryRoot, session.Id + ".session.json");
            var json = JsonSerializer.Serialize(session, _jsonOptions);
            var bytes = System.Text.Encoding.UTF8.GetBytes(json);
            AtomicFileWriter.WriteAtomic(sessionFile, bytes);
        }

        public IReadOnlyList<HistoryEntry> LoadRecentHistory(int maxEntries = 50)
        {
            if (maxEntries <= 0 || !Directory.Exists(_paths.HistoryRoot))
            {
                return [];
            }

            var files = Directory.GetFiles(_paths.HistoryRoot, "*.json")
                .Where(file => !file.EndsWith(".session.json", StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(File.GetLastWriteTimeUtc)
                .Take(maxEntries)
                .ToArray();

            var entries = new List<HistoryEntry>();
            foreach (var file in files)
            {
                var entry = ReadJson<HistoryEntry>(file);
                if (entry is not null)
                {
                    entries.Add(entry);
                }
            }

            return entries;
        }

        public IReadOnlyList<ChatSession> LoadRecentSessions(int maxSessions = 10)
        {
            if (maxSessions <= 0 || !Directory.Exists(_paths.HistoryRoot))
            {
                return [];
            }

            var files = Directory.GetFiles(_paths.HistoryRoot, "*.session.json")
                .OrderByDescending(File.GetLastWriteTimeUtc)
                .Take(maxSessions)
                .ToArray();

            var sessions = new List<ChatSession>();
            foreach (var file in files)
            {
                var session = ReadJson<ChatSession>(file);
                if (session is not null)
                {
                    sessions.Add(session);
                }
            }

            return sessions;
        }

        private T? ReadJson<T>(string path)
        {
            try
            {
                var json = File.ReadAllText(path);
                return JsonSerializer.Deserialize<T>(json, _jsonOptions);
            }
            catch
            {
                return default;
            }
        }
    }
}
