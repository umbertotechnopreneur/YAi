using System;
using System.IO;
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
            var sessionFile = Path.Combine(_paths.HistoryRoot, entry.Id + ".json");
            var json = JsonSerializer.Serialize(entry, _jsonOptions);
            var bytes = System.Text.Encoding.UTF8.GetBytes(json);
            AtomicFileWriter.WriteAtomic(sessionFile, bytes);
        }

        public void SaveChatSession(ChatSession session)
        {
            var sessionFile = Path.Combine(_paths.HistoryRoot, session.Id + ".session.json");
            var json = JsonSerializer.Serialize(session, _jsonOptions);
            var bytes = System.Text.Encoding.UTF8.GetBytes(json);
            AtomicFileWriter.WriteAtomic(sessionFile, bytes);
        }
    }
}
