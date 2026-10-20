namespace OrchestratorCore.Models;

/// <summary>
/// Result of a stage execution, including status, output, and any errors.
/// </summary>
public class StageResult
{
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;
    public Dictionary<string, object?> Output { get; set; } = new();
    public Exception? Error { get; set; }
    public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;
    public long DurationMs { get; set; }
    public int RetryCount { get; set; }
}
