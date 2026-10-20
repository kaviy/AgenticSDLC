using UrlShortenerService.Repository;
using UrlShortenerService.Service;
using Xunit;

namespace UrlShortenerService.Tests
{

    public class UrlShortenerServiceTests
    {
        private readonly UrlRepository _repository = new();
        private readonly UrlShortenerService _service;

        public UrlShortenerServiceTests()
        {
            _service = new UrlShortenerService(_repository);
        }

        [Fact]
        public void ShortenUrl_CreatesShortUrl()
        {
            // Act
            var result = _service.ShortenUrl("https://example.com");

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result.ShortCode);
            Assert.Equal("https://example.com", result.OriginalUrl);
        }

        [Fact]
        public void ShortenUrl_RejectsInvalidUrl()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => _service.ShortenUrl("not-a-url"));
        }

        [Fact]
        public void GetUrl_ReturnsUrl()
        {
            // Arrange
            var shortened = _service.ShortenUrl("https://example.com");

            // Act
            var result = _service.GetUrl(shortened.ShortCode);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(shortened.OriginalUrl, result.OriginalUrl);
        }

        [Fact]
        public void GetUrl_ReturnsNullForNonExistent()
        {
            // Act
            var result = _service.GetUrl("NOTFOUND");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GetRedirectUrl_RecordsAccess()
        {
            // Arrange
            var shortened = _service.ShortenUrl("https://example.com");

            // Act
            var result = _service.GetRedirectUrl(shortened.ShortCode, "Mozilla/5.0", "127.0.0.1");

            // Assert
            Assert.Equal("https://example.com", result);
            var analytics = _service.GetAnalytics(shortened.ShortCode);
            Assert.NotNull(analytics);
            Assert.Equal(1, analytics.TotalAccesses);
        }

        [Fact]
        public void GetAnalytics_TracksDailyAccess()
        {
            // Arrange
            var shortened = _service.ShortenUrl("https://example.com");
            _service.GetRedirectUrl(shortened.ShortCode);
            _service.GetRedirectUrl(shortened.ShortCode);

            // Act
            var analytics = _service.GetAnalytics(shortened.ShortCode);

            // Assert
            Assert.NotNull(analytics);
            Assert.Equal(2, analytics.TotalAccesses);
            Assert.NotNull(analytics.FirstAccess);
            Assert.NotNull(analytics.LastAccess);
        }

        [Fact]
        public void ListUrls_ReturnsAllUrls()
        {
            // Arrange
            _service.ShortenUrl("https://example.com/1");
            _service.ShortenUrl("https://example.com/2");

            // Act
            var results = _service.ListUrls();

            // Assert
            Assert.NotEmpty(results);
            Assert.True(results.Count >= 2);
        }
    }
}
