using System.Text.Json;
using System.Text.Json.Serialization;
using OrchestratorCore.Models;

namespace OrchestratorCore.Persistence;

/// <summary>
/// Manages persistence and retrieval of workflow executions.
/// </summary>
public class WorkflowExecutionRepository
{
    private readonly string _storagePath;
    private readonly JsonSerializerOptions _jsonOptions;

    public WorkflowExecutionRepository(string storagePath = "./workflows")
    {
        _storagePath = storagePath;
        Directory.CreateDirectory(_storagePath);

        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
    }

    /// <summary>
    /// Save a workflow execution to disk.
    /// </summary>
    public async Task SaveExecutionAsync(WorkflowExecution execution)
    {
        var filePath = Path.Combine(_storagePath, $"{execution.Id}.json");
        var json = JsonSerializer.Serialize(execution, _jsonOptions);
        await File.WriteAllTextAsync(filePath, json);
    }

    /// <summary>
    /// Load a workflow execution from disk.
    /// </summary>
    public async Task<WorkflowExecution?> LoadExecutionAsync(string executionId)
    {
        var filePath = Path.Combine(_storagePath, $"{executionId}.json");
        if (!File.Exists(filePath))
        {
            return null;
        }

        var json = await File.ReadAllTextAsync(filePath);
        return JsonSerializer.Deserialize<WorkflowExecution>(json, _jsonOptions);
    }

    /// <summary>
    /// List all saved workflow executions.
    /// </summary>
    public List<string> ListExecutions()
    {
        return Directory.GetFiles(_storagePath, "*.json")
            .Select(f => Path.GetFileNameWithoutExtension(f))
            .OrderByDescending(f => f)
            .ToList();
    }

    /// <summary>
    /// Delete a workflow execution.
    /// </summary>
    public bool DeleteExecution(string executionId)
    {
        var filePath = Path.Combine(_storagePath, $"{executionId}.json");
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
            return true;
        }

        return false;
    }
}
