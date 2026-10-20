namespace OrchestratorCore.Models;

/// <summary>
/// Gate validation result for stage entry/exit conditions.
/// </summary>
public class GateValidationResult
{
    public bool IsPassed { get; set; }
    public List<string> Messages { get; set; } = new();
    public string GateName { get; set; } = string.Empty;

    public static GateValidationResult Pass(string gateName, string message = "") =>
        new() { IsPassed = true, GateName = gateName, Messages = string.IsNullOrEmpty(message) ? new() : new(1) { message } };

    public static GateValidationResult Fail(string gateName, params string[] messages) =>
        new() { IsPassed = false, GateName = gateName, Messages = messages.ToList() };
}
