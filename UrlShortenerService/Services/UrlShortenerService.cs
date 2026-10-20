using UrlShortenerService.Domain;
using UrlShortenerService.Data;

namespace UrlShortenerService.Services
{
    /// <summary>Service for shortening and managing URLs.</summary>
    public class UrlShortenerService
    {
        private readonly UrlRepository _repository;
        private const string CHARS = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        private const int DEFAULT_SHORT_CODE_LENGTH = 6;

        public UrlShortenerService(UrlRepository repository)
        {
            _repository = repository;
        }

        /// <summary>Shorten a URL.</summary>
        public ShortenedUrl ShortenUrl(string longUrl, int? customLength = null, DateTime? expiresAt = null, string? createdBy = null)
        {
            if (string.IsNullOrWhiteSpace(longUrl))
                throw new ArgumentException("URL cannot be empty", nameof(longUrl));

            // Validate URL format (basic check)
            if (!longUrl.StartsWith("http://") && !longUrl.StartsWith("https://"))
                longUrl = "https://" + longUrl;

            // Generate short code
            var shortCode = GenerateShortCode(customLength ?? DEFAULT_SHORT_CODE_LENGTH);

            // Ensure uniqueness (retry on collision)
            int attempts = 0;
            while (_repository.GetByShortCode(shortCode) != null && attempts < 10)
            {
                shortCode = GenerateShortCode(customLength ?? DEFAULT_SHORT_CODE_LENGTH);
                attempts++;
            }

            if (attempts >= 10)
                throw new InvalidOperationException("Failed to generate unique short code after 10 attempts");

            var url = new ShortenedUrl
            {
                ShortCode = shortCode,
                OriginalUrl = longUrl,
                ExpiresAt = expiresAt,
                CreatedBy = createdBy ?? "anonymous"
            };

            _repository.Create(url);
            return url;
        }

        /// <summary>Resolve a short code to its original URL.</summary>
        public ShortenedUrl? ResolveUrl(string shortCode)
        {
            return _repository.GetByShortCode(shortCode);
        }

        /// <summary>Record a hit to a URL."""</summary>
        public void RecordHit(string shortCode, string? userAgent = null, string? ipAddress = null, string? referrer = null)
        {
            _repository.RecordAccess(shortCode, userAgent, ipAddress, referrer);
        }

        /// <summary>Get all shortened URLs.</summary>
        public List<ShortenedUrl> GetAllUrls()
        {
            return _repository.GetAll();
        }

        /// <summary>Delete a short code.</summary>
        public bool DeleteUrl(string shortCode)
        {
            return _repository.Delete(shortCode);
        }

        /// <summary>Get analytics for a URL.</summary>
        public UrlAnalytics GetAnalytics(string shortCode)
        {
            return _repository.GetAnalytics(shortCode);
        }

        private string GenerateShortCode(int length)
        {
            var random = new Random();
            var result = new char[length];
            for (int i = 0; i < length; i++)
            {
                result[i] = CHARS[random.Next(CHARS.Length)];
            }
            return new string(result);
        }
    }

    /// <summary>Service for tracking and reporting analytics.</summary>
    public class AnalyticsService
    {
        private readonly UrlRepository _repository;

        public AnalyticsService(UrlRepository repository)
        {
            _repository = repository;
        }

        /// <summary>Get analytics for a specific URL."""</summary>
        public UrlAnalytics GetUrlAnalytics(string shortCode)
        {
            return _repository.GetAnalytics(shortCode);
        }

        /// <summary>Get top URLs by access count."""</summary>
        public List<(ShortenedUrl Url, UrlAnalytics Analytics)> GetTopUrls(int count = 10)
        {
            var urls = _repository.GetAll().OrderByDescending(u => u.AccessCount).Take(count).ToList();
            return urls.Select(u => (u, _repository.GetAnalytics(u.ShortCode))).ToList();
        }

        /// <summary>Get daily access summary."""</summary>
        public Dictionary<string, int> GetDailyAccessSummary()
        {
            var summary = new Dictionary<string, int>();
            var urls = _repository.GetAll();

            foreach (var url in urls)
            {
                var analytics = _repository.GetAnalytics(url.ShortCode);
                foreach (var (day, count) in analytics.AccessesByDay)
                {
                    if (summary.ContainsKey(day))
                        summary[day] += count;
                    else
                        summary[day] = count;
                }
            }

            return summary.OrderBy(kv => kv.Key).ToDictionary(kv => kv.Key, kv => kv.Value);
        }
    }

    /// <summary>Service for health checks and reliability metrics."""</summary>
    public class HealthCheckService
    {
        private readonly UrlRepository _repository;
        private DateTime _startTime = DateTime.UtcNow;
        private int _totalRequests;
        private int _failedRequests;

        public HealthCheckService(UrlRepository repository)
        {
            _repository = repository;
        }

        /// <summary>Get health check status."""</summary>
        public HealthStatus GetHealthStatus()
        {
            var urls = _repository.GetAll();
            var uptime = DateTime.UtcNow - _startTime;
            var successRate = _totalRequests == 0 ? 100.0 : (double)(_totalRequests - _failedRequests) / _totalRequests * 100;

            return new HealthStatus
            {
                IsHealthy = _failedRequests == 0,
                UptimeMs = (long)uptime.TotalMilliseconds,
                TotalUrls = urls.Count,
                ActiveUrls = urls.Count(u => u.IsActive),
                TotalRequests = _totalRequests,
                FailedRequests = _failedRequests,
                SuccessRate = successRate,
                Timestamp = DateTime.UtcNow
            };
        }

        /// <summary>Record a request."""</summary>
        public void RecordRequest(bool success = true)
        {
            _totalRequests++;
            if (!success)
                _failedRequests++;
        }
    }

    /// <summary>Health status snapshot."""</summary>
    public class HealthStatus
    {
        public bool IsHealthy { get; set; }
        public long UptimeMs { get; set; }
        public int TotalUrls { get; set; }
        public int ActiveUrls { get; set; }
        public int TotalRequests { get; set; }
        public int FailedRequests { get; set; }
        public double SuccessRate { get; set; }
        public DateTime Timestamp { get; set; }
    }
}