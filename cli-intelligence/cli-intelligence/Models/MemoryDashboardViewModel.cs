#region Using

namespace cli_intelligence.Models;

#endregion

/// <summary>
/// Represents the aggregated view for the Brain & Memory dashboard.
/// </summary>
sealed class MemoryDashboardViewModel
{
    #region Properties

    public required DashboardConfigSummary Config { get; init; }

    public required IReadOnlyList<DashboardFileSummary> Files { get; init; }

    public required DashboardMaintenanceSummary Maintenance { get; init; }

    public required DashboardRoutingSummary Routing { get; init; }

    #endregion
}

/// <summary>
/// Contains the memory-related runtime configuration values.
/// </summary>
sealed class DashboardConfigSummary
{
    #region Properties

    public required bool ExtractionEnabled { get; init; }

    public required string ExtractionModel { get; init; }

    public required bool ExtractionUseLocal { get; init; }

    public required double ExtractionConfidenceThreshold { get; init; }

    public required int FlushThreshold { get; init; }

    public required bool HeartbeatEnabled { get; init; }

    public required bool HeartbeatRunOnStartup { get; init; }

    public required int HeartbeatDecayIntervalDays { get; init; }

    public required int HeartbeatStaleThresholdDays { get; init; }

    public required string HeartbeatModel { get; init; }

    public required bool LlamaEnabled { get; init; }

    public required string LlamaUrl { get; init; }

    public required string RemoteModel { get; init; }

    #endregion
}

/// <summary>
/// Represents a memory-file summary row on the dashboard.
/// </summary>
sealed class DashboardFileSummary
{
    #region Properties

    public required string LogicalName { get; init; }

    public required string Category { get; init; }

    public required string PhysicalPath { get; init; }

    public required long SizeBytes { get; init; }

    public required int EstimatedTokens { get; init; }

    public required DateTimeOffset? LastModified { get; init; }

    public required string Tier { get; init; }

    #endregion
}

/// <summary>
/// Represents maintenance state for dashboard rendering.
/// </summary>
sealed class DashboardMaintenanceSummary
{
    #region Properties

    public required DateTimeOffset? LastHeartbeatRun { get; init; }

    public required DateTimeOffset? LastDreamingRun { get; init; }

    public required DateTimeOffset? LastFlushRun { get; init; }

    public required int PendingDreamCount { get; init; }

    public required int ReminderCount { get; init; }

    #endregion
}

/// <summary>
/// Represents effective model routing decisions by subsystem.
/// </summary>
sealed class DashboardRoutingSummary
{
    #region Properties

    public required string ChatRoute { get; init; }

    public required string ExtractionRoute { get; init; }

    public required string HeartbeatRoute { get; init; }

    public required string FlushRoute { get; init; }

    public required string DreamingRoute { get; init; }

    #endregion
}
