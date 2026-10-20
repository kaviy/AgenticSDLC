namespace UrlShortenerService.Domain;

/// <summary>
/// Domain model for a shortened URL.
/// </summary>
public class ShortenedUrl
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string OriginalUrl { get; set; } = string.Empty;
    public string ShortCode { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiresAt { get; set; }
    public bool IsActive { get; set; } = true;
    public int AccessCount { get; set; } = 0;
    public string CreatedBy { get; set; } = "system";
    public Dictionary<string, string> Tags { get; set; } = new();
}

/// <summary>
/// Represents an access log entry for analytics.
/// </summary>
public class AccessLog
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string ShortCode { get; set; } = string.Empty;
    public DateTime AccessedAt { get; set; } = DateTime.UtcNow;
    public string? UserAgent { get; set; }
    public string? IpAddress { get; set; }
    public string? Referrer { get; set; }
}

/// <summary>
/// Analytics summary for a shortened URL.
/// </summary>
public class UrlAnalytics
{
    public string ShortCode { get; set; } = string.Empty;
    public int TotalAccesses { get; set; }
    public DateTime? FirstAccess { get; set; }
    public DateTime? LastAccess { get; set; }
    public Dictionary<string, int> AccessesByDay { get; set; } = new();
    public Dictionary<string, int> TopReferrers { get; set; } = new();
}
