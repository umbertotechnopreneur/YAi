using System;
using System.IO;
using System.Text;
using cli_intelligence.Models;

namespace cli_intelligence.Services;

/// <summary>
/// Provides a singleton, thread-safe append-only CSV logger for AI usage analytics.
/// </summary>
public sealed class AiUsageLogger
{
    private static readonly Lazy<AiUsageLogger> _instance = new(() => new AiUsageLogger());

    /// <summary>Gets the shared logger instance for the process lifetime.</summary>
    public static AiUsageLogger Instance => _instance.Value;

    /// <summary>Gets the CSV file path used by the logger.</summary>
    private readonly string _logPath;
    /// <summary>Serializes writes to the CSV file.</summary>
    private readonly object _lock = new();
    /// <summary>Tracks whether the CSV header has been written during this process.</summary>
    private bool _headerWritten;

    /// <summary>Initializes a new instance of the logger.</summary>
    private AiUsageLogger()
    {
        var dir = Path.Combine("data", "logs");
        Directory.CreateDirectory(dir);
        _logPath = Path.Combine(dir, "ai-usage.csv");
        _headerWritten = File.Exists(_logPath) && new FileInfo(_logPath).Length > 0;
    }

    /// <summary>Appends a single AI usage row to the CSV file.</summary>
    public void Log(AiUsageLogEntry entry)
    {
        lock (_lock)
        {
            using var stream = new FileStream(_logPath, FileMode.Append, FileAccess.Write, FileShare.Read);
            using var writer = new StreamWriter(stream, new UTF8Encoding(false));
            if (!_headerWritten)
            {
                writer.WriteLine(AiUsageLogEntry.CsvHeader);
                _headerWritten = true;
            }
            writer.WriteLine(entry.ToCsvRow());
        }
    }
}
