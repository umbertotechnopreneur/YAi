using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using YAi.Persona.Models;

namespace YAi.Persona.Services
{
    public sealed class MemoryFileParser
    {
        public MemoryDocument Parse(string markdown)
        {
            if (string.IsNullOrEmpty(markdown))
                return new MemoryDocument();

            var doc = new MemoryDocument();
            var lines = markdown.Replace("\r\n", "\n").Split('\n');
            if (lines.Length > 0 && lines[0].Trim() == "---")
            {
                var fm = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                int i = 1;
                for (; i < lines.Length; i++)
                {
                    var line = lines[i];
                    if (line.Trim() == "---")
                    {
                        i++;
                        break;
                    }
                    var idx = line.IndexOf(':');
                    if (idx > 0)
                    {
                        var key = line.Substring(0, idx).Trim();
                        var value = line.Substring(idx + 1).Trim();
                        fm[key] = value;
                    }
                }

                doc.FrontMatter = fm;
                doc.Body = string.Join("\n", lines.Skip(i));
            }
            else
            {
                doc.Body = markdown;
            }

            return doc;
        }

        public string Serialize(MemoryDocument document)
        {
            if (document == null)
                throw new ArgumentNullException(nameof(document));

            var sb = new StringBuilder();
            if (document.FrontMatter != null && document.FrontMatter.Count > 0)
            {
                sb.AppendLine("---");
                foreach (var kv in document.FrontMatter.OrderBy(k => k.Key, StringComparer.OrdinalIgnoreCase))
                {
                    sb.AppendLine($"{kv.Key}: {kv.Value}");
                }
                sb.AppendLine("---");
            }

            if (!string.IsNullOrEmpty(document.Body))
            {
                sb.Append(document.Body.Replace("\r\n", "\n"));
            }

            return sb.ToString();
        }

        public string UpsertFrontMatter(string markdown, IReadOnlyDictionary<string, string> updates)
        {
            var doc = Parse(markdown ?? string.Empty);
            foreach (var kv in updates)
            {
                doc.FrontMatter[kv.Key] = kv.Value;
            }

            return Serialize(doc);
        }
    }
}
