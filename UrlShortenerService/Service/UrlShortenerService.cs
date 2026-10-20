using UrlShortenerService.Domain;
using UrlShortenerService.Repository;

namespace UrlShortenerService.Service
{
    /// <summary>
    /// Business logic for URL shortening and management.
    /// </summary>
    public class UrlShortenerService
    {
        private readonly UrlRepository _repository;
        private readonly IShortCodeGenerator _generator;

        public UrlShortenerService(UrlRepository? repository = null, IShortCodeGenerator? generator = null)
        {
            _repository = repository ?? new UrlRepository();
            _generator = generator ?? new Base62ShortCodeGenerator();
        }

        /// <summary>
        /// Shorten a URL.
        /// </summary>
        public ShortenedUrl ShortenUrl(string originalUrl, int? expirationMinutes = null)
        {
            if (string.IsNullOrWhiteSpace(originalUrl))
            {
                throw new ArgumentException("Original URL cannot be empty", nameof(originalUrl));
            }

            if (!IsValidUrl(originalUrl))
            {
                throw new ArgumentException("Invalid URL format", nameof(originalUrl));
            }

            var shortCode = _generator.Generate();

            // Ensure uniqueness
            while (_repository.GetUrl(shortCode) != null)
            {
                shortCode = _generator.Generate();
            }

            return _repository.CreateUrl(originalUrl, shortCode, expirationMinutes);
        }

        /// <summary>
        /// Get the original URL for a short code.
        /// </summary>
        public ShortenedUrl? GetUrl(string shortCode)
        {
            if (string.IsNullOrWhiteSpace(shortCode))
            {
                return null;
            }

            return _repository.GetUrl(shortCode);
        }

        /// <summary>
        /// Record an access and get redirect URL.
        /// </summary>
        public string? GetRedirectUrl(string shortCode, string? userAgent = null, string? ipAddress = null, string? referrer = null)
        {
            var url = GetUrl(shortCode);
            if (url == null)
            {
                return null;
            }

            _repository.RecordAccess(shortCode, userAgent, ipAddress, referrer);
            return url.OriginalUrl;
        }

        /// <summary>
        /// Get analytics for a URL.
        /// </summary>
        public UrlAnalytics? GetAnalytics(string shortCode)
        {
            return _repository.GetAnalytics(shortCode);
        }

        /// <summary>
        /// List all URLs.
        /// </summary>
        public List<ShortenedUrl> ListUrls()
        {
            return _repository.ListUrls();
        }

        /// <summary>
        /// Validate a URL format.
        /// </summary>
        private static bool IsValidUrl(string url)
        {
            try
            {
                var uri = new Uri(url);
                return uri.Scheme is "http" or "https";
            }
            catch
            {
                return false;
            }
        }
    }

    /// <summary>
    /// Interface for generating short codes.
    /// </summary>
    public interface IShortCodeGenerator
    {
        string Generate();
    }

    /// <summary>
    /// Base62 short code generator for readable short codes.
    /// </summary>
    public class Base62ShortCodeGenerator : IShortCodeGenerator
    {
        private const string Alphabet = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
        private static readonly Random Random = new();

        public string Generate()
        {
            var sb = new System.Text.StringBuilder(6);
            for (int i = 0; i < 6; i++)
            {
                sb.Append(Alphabet[Random.Next(Alphabet.Length)]);
            }

            return sb.ToString();
        }
    }
}
