using UrlShortenerService.Domain;

namespace UrlShortenerService.Repository
{
    /// <summary>
    /// In-memory repository for storing shortened URLs.
    /// </summary>
    public class UrlRepository
    {
        private readonly Dictionary<string, ShortenedUrl> _urls = new();
        private readonly List<AccessLog> _accessLogs = new();
        private readonly object _lock = new();

        /// <summary>
        /// Create a new shortened URL.
        /// </summary>
        public ShortenedUrl CreateUrl(string originalUrl, string shortCode, int? expirationMinutes = null)
        {
            var url = new ShortenedUrl
            {
                OriginalUrl = originalUrl,
                ShortCode = shortCode,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = expirationMinutes.HasValue ? DateTime.UtcNow.AddMinutes(expirationMinutes.Value) : null,
                IsActive = true
            };

            lock (_lock)
            {
                _urls[shortCode] = url;
            }

            return url;
        }

        /// <summary>
        /// Get a shortened URL by its short code.
        /// </summary>
        public ShortenedUrl? GetUrl(string shortCode)
        {
            lock (_lock)
            {
                if (_urls.TryGetValue(shortCode, out var url))
                {
                    // Check if URL has expired
                    if (url.ExpiresAt.HasValue && url.ExpiresAt.Value < DateTime.UtcNow)
                    {
                        url.IsActive = false;
                        return null;
                    }

                    return url.IsActive ? url : null;
                }

                return null;
            }
        }

        /// <summary>
        /// Record an access to a shortened URL.
        /// </summary>
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

        /// <summary>
        /// Get analytics for a shortened URL.
        /// </summary>
        public UrlAnalytics? GetAnalytics(string shortCode)
        {
            lock (_lock)
            {
                var logs = _accessLogs.Where(l => l.ShortCode == shortCode).ToList();

                if (logs.Count == 0)
                {
                    return new UrlAnalytics { ShortCode = shortCode };
                }

                var accessesByDay = logs
                    .GroupBy(l => l.AccessedAt.Date.ToString("yyyy-MM-dd"))
                    .ToDictionary(g => g.Key, g => g.Count());

                var topReferrers = logs
                    .Where(l => !string.IsNullOrEmpty(l.Referrer))
                    .GroupBy(l => l.Referrer!)
                    .OrderByDescending(g => g.Count())
                    .Take(10)
                    .ToDictionary(g => g.Key, g => g.Count());

                return new UrlAnalytics
                {
                    ShortCode = shortCode,
                    TotalAccesses = logs.Count,
                    FirstAccess = logs.OrderBy(l => l.AccessedAt).First().AccessedAt,
                    LastAccess = logs.OrderByDescending(l => l.AccessedAt).First().AccessedAt,
                    AccessesByDay = accessesByDay,
                    TopReferrers = topReferrers
                };
            }
        }

        /// <summary>
        /// List all active shortened URLs.
        /// </summary>
        public List<ShortenedUrl> ListUrls()
        {
            lock (_lock)
            {
                return _urls.Values.Where(u => u.IsActive).ToList();
            }
        }

        /// <summary>
        /// Deactivate a shortened URL.
        /// </summary>
        public bool DeactivateUrl(string shortCode)
        {
            lock (_lock)
            {
                if (_urls.TryGetValue(shortCode, out var url))
                {
                    url.IsActive = false;
                    return true;
                }

                return false;
            }
        }

        /// <summary>
        /// Count total URLs.
        /// </summary>
        public int GetUrlCount()
        {
            lock (_lock)
            {
                return _urls.Count(kvp => kvp.Value.IsActive);
            }
        }
    }
}
