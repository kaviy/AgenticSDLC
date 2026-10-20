namespace UrlShortenerService.Data;

using UrlShortenerService.Domain;
using System.Collections.Generic;

/// <summary>Repository for URL mappings and analytics.</summary>
public class UrlRepository
{
    private readonly Dictionary<string, ShortenedUrl> _urls = new();
    private readonly List<AccessLog> _accessLogs = new();
    private readonly object _lock = new();

    /// <summary>Create a new shortened URL.</summary>
    public void Create(ShortenedUrl url)
    {
        lock (_lock)
        {
            if (_urls.ContainsKey(url.ShortCode))
                throw new InvalidOperationException($"Short code '{url.ShortCode}' already exists");

            _urls[url.ShortCode] = url;
        }
    }

    /// <summary>Retrieve a URL by short code.</summary>
    public ShortenedUrl? GetByShortCode(string shortCode)
    {
        lock (_lock)
        {
            return _urls.TryGetValue(shortCode, out var url) ? url : null;
        }
    }

    /// <summary>Record an access/hit to a short code.</summary>
    public void RecordAccess(string shortCode, string? userAgent = null, string? ipAddress = null, string? referrer = null)
    {
        lock (_lock)
        {
            if (_urls.TryGetValue(shortCode, out var url))
            {
                url.AccessCount++;
            }

            _accessLogs.Add(new AccessLog
            {
                ShortCode = shortCode,
                AccessedAt = DateTime.UtcNow,
                UserAgent = userAgent,
                IpAddress = ipAddress,
                Referrer = referrer
            });
        }
    }

    /// <summary>Get all URLs.</summary>
    public List<ShortenedUrl> GetAll()
    {
        lock (_lock)
        {
            return _urls.Values.ToList();
        }
    }

    /// <summary>Get access logs for a short code.</summary>
    public List<AccessLog> GetAccessLogs(string shortCode)
    {
        lock (_lock)
        {
            return _accessLogs.Where(log => log.ShortCode == shortCode).ToList();
        }
    }

    /// <summary>Delete a URL mapping.</summary>
    public bool Delete(string shortCode)
    {
        lock (_lock)
        {
            return _urls.Remove(shortCode);
        }
    }

    /// <summary>Get analytics summary for a URL.</summary>
    public UrlAnalytics GetAnalytics(string shortCode)
    {
        lock (_lock)
        {
            var logs = _accessLogs.Where(l => l.ShortCode == shortCode).ToList();
            var url = _urls.TryGetValue(shortCode, out var u) ? u : null;

            var referrerCounts = new Dictionary<string, int>();
            foreach (var log in logs)
            {
                if (log.Referrer != null)
                {
                    if (referrerCounts.ContainsKey(log.Referrer))
                        referrerCounts[log.Referrer]++;
                    else
                        referrerCounts[log.Referrer] = 1;
                }
            }

            var accessesByDay = new Dictionary<string, int>();
            foreach (var log in logs)
            {
                var day = log.AccessedAt.Date.ToString("yyyy-MM-dd");
                if (accessesByDay.ContainsKey(day))
                    accessesByDay[day]++;
                else
                    accessesByDay[day] = 1;
            }

            return new UrlAnalytics
            {
                ShortCode = shortCode,
                TotalAccesses = url?.AccessCount ?? 0,
                FirstAccess = logs.MinBy(l => l.AccessedAt)?.AccessedAt,
                LastAccess = logs.MaxBy(l => l.AccessedAt)?.AccessedAt,
                AccessesByDay = accessesByDay,
                TopReferrers = referrerCounts
            };
        }
    }

    /// <summary>Clear all data (for testing).</summary>
    public void Clear()
    {
        lock (_lock)
        {
            _urls.Clear();
            _accessLogs.Clear();
        }
    }
}
