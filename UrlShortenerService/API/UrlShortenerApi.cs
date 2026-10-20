namespace UrlShortenerService.API;

using UrlShortenerService.Services;
using UrlShortenerService.Data;
using System.Text.Json;

/// <summary>API Requests.</summary>
public class ShortenRequest
{
    public required string Url { get; set; }
    public int? CustomLength { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

/// <summary>API Responses.</summary>
public class ShortenResponse
{
    public required string ShortCode { get; set; }
    public required string OriginalUrl { get; set; }
    public required string ShortUrl { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ResolveResponse
{
    public required string OriginalUrl { get; set; }
    public int AccessCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ErrorResponse
{
    public required string Message { get; set; }
    public string? Details { get; set; }
}

/// <summary>REST API for URL shortener service."""</summary>
public class UrlShortenerApi
{
    private readonly UrlShortenerService _shortenService;
    private readonly AnalyticsService _analyticsService;
    private readonly HealthCheckService _healthService;
    private readonly string _baseUrl;

    public UrlShortenerApi(string baseUrl = "http://localhost:5000")
    {
        var repository = new UrlRepository();
        _shortenService = new UrlShortenerService(repository);
        _analyticsService = new AnalyticsService(repository);
        _healthService = new HealthCheckService(repository);
        _baseUrl = baseUrl;
    }

    /// <summary>POST /api/shorten - Shorten a URL."""</summary>
    public (int StatusCode, string Response) Shorten(string requestBody)
    {
        try
        {
            _healthService.RecordRequest();

            var request = JsonSerializer.Deserialize<ShortenRequest>(requestBody)
                ?? throw new ArgumentException("Invalid request");

            var url = _shortenService.ShortenUrl(request.Url, request.CustomLength, request.ExpiresAt);

            var response = new ShortenResponse
            {
                ShortCode = url.ShortCode,
                OriginalUrl = url.OriginalUrl,
                ShortUrl = $"{_baseUrl}/{url.ShortCode}",
                CreatedAt = url.CreatedAt
            };

            return (200, JsonSerializer.Serialize(response));
        }
        catch (ArgumentException ex)
        {
            _healthService.RecordRequest(false);
            var error = new { Message = ex.Message, StatusCode = 400 };
            return (400, JsonSerializer.Serialize(error));
        }
        catch (Exception ex)
        {
            _healthService.RecordRequest(false);
            var error = new { Message = "Internal server error", Details = ex.Message, StatusCode = 500 };
            return (500, JsonSerializer.Serialize(error));
        }
    }

    /// <summary>GET /api/{shortCode} - Resolve a short code."""</summary>
    public (int StatusCode, string Response) Resolve(string shortCode)
    {
        try
        {
            _healthService.RecordRequest();

            var url = _shortenService.ResolveUrl(shortCode);
            if (url == null)
            {
                _healthService.RecordRequest(false);
                var notFound = new { Message = "Short code not found", StatusCode = 404 };
                return (404, JsonSerializer.Serialize(notFound));
            }

            // Record hit
            _shortenService.RecordHit(shortCode);

            var response = new ResolveResponse
            {
                OriginalUrl = url.OriginalUrl,
                AccessCount = url.AccessCount,
                CreatedAt = url.CreatedAt
            };

            return (200, JsonSerializer.Serialize(response));
        }
        catch (Exception ex)
        {
            _healthService.RecordRequest(false);
            var error = new { Message = "Internal server error", Details = ex.Message, StatusCode = 500 };
            return (500, JsonSerializer.Serialize(error));
        }
    }

    /// <summary>GET /api/analytics/{shortCode} - Get analytics for a URL."""</summary>
    public (int StatusCode, string Response) GetAnalytics(string shortCode)
    {
        try
        {
            _healthService.RecordRequest();

            var analytics = _analyticsService.GetUrlAnalytics(shortCode);
            return (200, JsonSerializer.Serialize(analytics));
        }
        catch (Exception ex)
        {
            _healthService.RecordRequest(false);
            var error = new { Message = "Internal server error", Details = ex.Message, StatusCode = 500 };
            return (500, JsonSerializer.Serialize(error));
        }
    }

    /// <summary>GET /api/health - Health check."""</summary>
    public (int StatusCode, string Response) Health()
    {
        var status = _healthService.GetHealthStatus();
        return (200, JsonSerializer.Serialize(status));
    }

    /// <summary>GET /api/stats - Overall statistics."""</summary>
    public (int StatusCode, string Response) GetStats()
    {
        try
        {
            var urls = _shortenService.GetAllUrls();
            var dailySummary = _analyticsService.GetDailyAccessSummary();
            var health = _healthService.GetHealthStatus();

            var stats = new
            {
                TotalUrls = urls.Count,
                ActiveUrls = urls.Count(u => u.IsActive),
                TotalAccesses = urls.Sum(u => u.AccessCount),
                DailyAccessSummary = dailySummary,
                Health = health
            };

            return (200, JsonSerializer.Serialize(stats));
        }
        catch (Exception ex)
        {
            var error = new { Message = "Internal server error", Details = ex.Message };
            return (500, JsonSerializer.Serialize(error));
        }
    }
}
