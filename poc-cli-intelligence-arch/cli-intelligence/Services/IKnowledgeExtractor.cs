using cli_intelligence.Models;

namespace cli_intelligence.Services;

interface IKnowledgeExtractor
{
    string ExtractorName { get; }

    string ExtractionType { get; }

    string BuildSchemaDescription();

    Task ApplyAsync(ExtractionItem item, LocalKnowledgeService knowledge, string workspaceStorageDir);
}
