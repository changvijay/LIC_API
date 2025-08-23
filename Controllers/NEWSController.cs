using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace LIC_WebDeskAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NEWSController : ControllerBase
    {
        private static readonly HttpClient _http = new HttpClient(new HttpClientHandler
        {
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
        })
        {
            Timeout = TimeSpan.FromSeconds(15)
        };

        // Seeds (only these four, per your request)
        private static readonly string[] SeedPages = new[]
        {
            "https://www.moneycontrol.com/news/tags/lic.html",
            "https://www.financialexpress.com/about/lic/",
            "https://timesofindia.indiatimes.com/topic/LIC",
            "https://economictimes.indiatimes.com/wealth/insure/lic"
        };

        // Concurrency, partial-read limit and cache windows (tweak if needed)
        // Increased concurrency for faster enrichment; be careful about remote servers and your own outbound limits.
        private readonly SemaphoreSlim _enrichSemaphore = new SemaphoreSlim(20); // raised from 10
        private const int MaxArticleBytesToRead = 150 * 1024; // 150 KB - slightly reduced to speed up reads
        private static readonly TimeSpan SeedCacheDurationEnriched = TimeSpan.FromMinutes(20);
        private static readonly TimeSpan ArticleCacheDuration = TimeSpan.FromMinutes(60);

        private readonly IMemoryCache _cache;

        public NEWSController(IMemoryCache cache)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            if (!_http.DefaultRequestHeaders.UserAgent.Any())
                _http.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (compatible; LIC_WebDeskBot/1.0; +https://example.com)");
        }

        /// <summary>
        /// GET /api/news/{count}
        /// count must be one of 10|20|50|100. Always performs enrichment (fetchFull = true) but uses caching and concurrency to be fast.
        /// If headline/url/imageUrl/summary is missing for an article, it will NOT be sent to frontend.
        /// </summary>
        [HttpGet("{count:int}")]
        public async Task<IActionResult> Get(int count)
        {

            bool fetchFull = true; // always true per your request
            string cacheKey = $"LIC_SEEDS_count_{count}_fetchFull_{fetchFull}";

            if (!_cache.TryGetValue(cacheKey, out List<NewsItem> allItems))
            {
                allItems = new List<NewsItem>();

                var scrapeTasks = SeedPages.Select(seed => Task.Run(async () =>
                {
                    try
                    {
                        var list = await ScrapeSeedAsync(seed, fetchFull);
                        return list ?? new List<NewsItem>();
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"Seed [{seed}] failed: {ex.Message}");
                        return new List<NewsItem>();
                    }
                })).ToArray();

                var results = await Task.WhenAll(scrapeTasks);
                foreach (var r in results) allItems.AddRange(r);

                // Deduplicate by exact URL
                allItems = allItems
                    .GroupBy(x => x.Url, StringComparer.OrdinalIgnoreCase)
                    .Select(g => g.First())
                    .Take(count)
                    .ToList();

                // cache enriched seeds (we only cache top X after filtering below)
                _cache.Set(cacheKey, allItems, SeedCacheDurationEnriched);
            }

            // Final filter: must be LIC related (defensive) and must have all required fields
            var filtered = allItems
                .Where(IsLikelyLIC)
                .Where(i =>
                    !string.IsNullOrWhiteSpace(i.Headline) &&
                    !string.IsNullOrWhiteSpace(i.Url) &&
                    !string.IsNullOrWhiteSpace(i.ImageUrl) &&
                    !string.IsNullOrWhiteSpace(i.Summary))
                .OrderByDescending(i => i.PublishedAt ?? DateTime.MinValue)
                .ToList();

            int totalRecords = filtered.Count;
            // return only "count" items
            var take = filtered.Take(count).ToList();

            return Ok(new
            {
                status = 200,
                totalRecords,
                requested = count,
                data = take
            });
        }

        // --- the rest is mostly unchanged but retained here for completeness ---

        private async Task<List<NewsItem>> ScrapeSeedAsync(string seedUrl, bool fetchFull)
        {
            var results = new List<NewsItem>();

            string seedCacheKey = $"SEED_{seedUrl.GetHashCode()}_fetchFull_{fetchFull}";
            if (_cache.TryGetValue(seedCacheKey, out List<NewsItem> cachedSeed))
            {
                return cachedSeed;
            }

            string html;
            try
            {
                html = await _http.GetStringAsync(seedUrl);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Failed to download seed {seedUrl}: {ex.Message}");
                return results;
            }

            var doc = new HtmlDocument();
            doc.LoadHtml(html);
            Uri seedUri = new Uri(seedUrl);
            string host = seedUri.Host.ToLowerInvariant();

            HtmlNodeCollection nodes = null;

            if (host.Contains("moneycontrol.com"))
                nodes = doc.DocumentNode.SelectNodes("//div[contains(@class,'listing') or contains(@class,'taxonomy-list') or contains(@class,'tags-list')]/article|//ul[contains(@class,'content')]/li");
            else if (host.Contains("financialexpress.com"))
                nodes = doc.DocumentNode.SelectNodes("//article|//div[contains(@class,'listing') or contains(@class,'story')]");
            else if (host.Contains("timesofindia.indiatimes.com"))
                nodes = doc.DocumentNode.SelectNodes("//div[contains(@class,'content')]/div|//div[contains(@class,'articleList')]/div|//ul[contains(@class,'list')]/li");
            else if (host.Contains("economictimes.indiatimes.com"))
                nodes = doc.DocumentNode.SelectNodes("//div[contains(@class,'eachStory')]|//ul[contains(@class,'list')]/li|//div[contains(@class,'article')]");

            if (nodes == null || nodes.Count == 0)
            {
                var linkNodes = doc.DocumentNode.SelectNodes("//a[@href and string-length(normalize-space())>20]");
                if (linkNodes != null)
                {
                    var taken = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var a in linkNodes)
                    {
                        var href = a.GetAttributeValue("href", "");
                        if (string.IsNullOrWhiteSpace(href)) continue;
                        var full = NormalizeHref(href, seedUrl);
                        if (!taken.Add(full)) continue;
                        results.Add(new NewsItem
                        {
                            Headline = WebUtility.HtmlDecode(a.InnerText.Trim()),
                            Url = full,
                            Source = new Uri(full).Host
                        });
                    }
                }

                _cache.Set(seedCacheKey, results, TimeSpan.FromMinutes(5));
                return results;
            }

            foreach (var n in nodes)
            {
                try
                {
                    HtmlNode linkNode = n.SelectSingleNode(".//a[@href and (contains(@class,'title') or contains(@class,'headline') or string-length(normalize-space())>20)]")
                                        ?? n.SelectSingleNode(".//h2//a[@href]")
                                        ?? n.SelectSingleNode(".//h3//a[@href]")
                                        ?? n.SelectSingleNode(".//a[@href]");

                    if (linkNode == null) continue;

                    string href = linkNode.GetAttributeValue("href", "").Trim();
                    if (string.IsNullOrWhiteSpace(href)) continue;
                    string url = NormalizeHref(href, seedUrl);

                    string headline = WebUtility.HtmlDecode(linkNode.InnerText.Trim());
                    if (string.IsNullOrWhiteSpace(headline))
                    {
                        var h = n.SelectSingleNode(".//h1|.//h2|.//h3");
                        if (h != null) headline = WebUtility.HtmlDecode(h.InnerText.Trim());
                    }

                    var imgNode = n.SelectSingleNode(".//img") ?? n.SelectSingleNode(".//figure//img");
                    string img = null;
                    if (imgNode != null)
                    {
                        img = imgNode.GetAttributeValue("data-src", "") ?? imgNode.GetAttributeValue("src", "");
                        if (!string.IsNullOrWhiteSpace(img) && img.StartsWith("/"))
                            img = seedUri.GetLeftPart(UriPartial.Authority) + img;
                    }

                    var timeNode = n.SelectSingleNode(".//time")
                                   ?? n.SelectSingleNode(".//span[contains(@class,'time') or contains(@class,'date') or contains(@class,'timestamp')]");
                    string rawTime = timeNode != null ? WebUtility.HtmlDecode(timeNode.InnerText.Trim()) : null;
                    DateTime? published = TryParseDateTime(rawTime);

                    var authorNode = n.SelectSingleNode(".//span[contains(@class,'author')]") ?? n.SelectSingleNode(".//a[contains(@href,'/author')]");
                    string author = authorNode != null ? WebUtility.HtmlDecode(authorNode.InnerText.Trim()) : null;

                    var p = n.SelectSingleNode(".//p");
                    string summary = p != null ? WebUtility.HtmlDecode(p.InnerText.Trim()) : null;

                    var item = new NewsItem
                    {
                        Headline = headline,
                        Url = url,
                        ImageUrl = img,
                        Summary = summary,
                        Author = author,
                        RawTimeText = rawTime,
                        PublishedAt = published,
                        Source = new Uri(url).Host
                    };

                    results.Add(item);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Node parse error ({seedUrl}): {ex.Message}");
                }
            }

            var licCandidates = results.Where(IsLikelyLIC).ToList();

            if (fetchFull && licCandidates.Count > 0)
            {
                var needEnrich = licCandidates.Where(i =>
                    string.IsNullOrWhiteSpace(i.Headline)
                    || string.IsNullOrWhiteSpace(i.Url)
                    || string.IsNullOrWhiteSpace(i.ImageUrl)
                    || string.IsNullOrWhiteSpace(i.Summary)).ToList();

                if (needEnrich.Count > 0)
                {
                    await EnrichListAsync(needEnrich);
                }
            }

            var cacheDuration = fetchFull ? SeedCacheDurationEnriched : TimeSpan.FromMinutes(5);
            _cache.Set(seedCacheKey, licCandidates, cacheDuration);

            return licCandidates;
        }

        private async Task EnrichListAsync(List<NewsItem> items)
        {
            if (items == null || items.Count == 0) return;

            var tasks = items.Select(async item =>
            {
                if (!string.IsNullOrWhiteSpace(item.Headline)
                 && !string.IsNullOrWhiteSpace(item.Url)
                 && !string.IsNullOrWhiteSpace(item.ImageUrl)
                 && !string.IsNullOrWhiteSpace(item.Summary))
                    return;

                string urlCacheKey = $"ARTICLE_{Math.Abs(item.Url.GetHashCode())}";
                if (_cache.TryGetValue(urlCacheKey, out NewsItem cached))
                {
                    MergeEnriched(item, cached);
                    return;
                }

                await _enrichSemaphore.WaitAsync();
                try
                {
                    await EnrichArticlePartialAsync(item);
                    if (!string.IsNullOrWhiteSpace(item.Headline)
                     && !string.IsNullOrWhiteSpace(item.Url)
                     && !string.IsNullOrWhiteSpace(item.ImageUrl)
                     && !string.IsNullOrWhiteSpace(item.Summary))
                    {
                        _cache.Set(urlCacheKey, item, ArticleCacheDuration);
                    }
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Enrich error for {item.Url}: {ex.Message}");
                }
                finally
                {
                    _enrichSemaphore.Release();
                }
            });

            await Task.WhenAll(tasks);
        }

        private async Task EnrichArticlePartialAsync(NewsItem item)
        {
            if (item == null || string.IsNullOrWhiteSpace(item.Url)) return;

            try
            {
                using (var resp = await _http.GetAsync(item.Url, HttpCompletionOption.ResponseHeadersRead))
                {
                    if (!resp.IsSuccessStatusCode) return;

                    using (var stream = await resp.Content.ReadAsStreamAsync())
                    {
                        int bufferSize = 8192;
                        var buffer = new byte[bufferSize];
                        int totalRead = 0;
                        using (var ms = new MemoryStream())
                        {
                            int read;
                            while (totalRead < MaxArticleBytesToRead
                                && (read = await stream.ReadAsync(buffer, 0, Math.Min(bufferSize, MaxArticleBytesToRead - totalRead))) > 0)
                            {
                                ms.Write(buffer, 0, read);
                                totalRead += read;

                                var soFar = Encoding.UTF8.GetString(ms.ToArray());
                                if (soFar.IndexOf("</head>", StringComparison.OrdinalIgnoreCase) >= 0
                                    || soFar.IndexOf("<body", StringComparison.OrdinalIgnoreCase) >= 0)
                                {
                                    break;
                                }
                            }

                            var partialHtml = Encoding.UTF8.GetString(ms.ToArray());
                            var doc = new HtmlDocument();
                            doc.LoadHtml(partialHtml);

                            var metaTitle = doc.DocumentNode.SelectSingleNode("//meta[@property='og:title']")?.GetAttributeValue("content", null)
                                           ?? doc.DocumentNode.SelectSingleNode("//meta[@name='title']")?.GetAttributeValue("content", null);
                            if (!string.IsNullOrWhiteSpace(metaTitle) && string.IsNullOrWhiteSpace(item.Headline))
                                item.Headline = WebUtility.HtmlDecode(metaTitle.Trim());

                            var metaDesc = doc.DocumentNode.SelectSingleNode("//meta[@property='og:description']")?.GetAttributeValue("content", null)
                                          ?? doc.DocumentNode.SelectSingleNode("//meta[@name='description']")?.GetAttributeValue("content", null);
                            if (!string.IsNullOrWhiteSpace(metaDesc) && string.IsNullOrWhiteSpace(item.Summary))
                                item.Summary = WebUtility.HtmlDecode(metaDesc.Trim());

                            var metaImage = doc.DocumentNode.SelectSingleNode("//meta[@property='og:image']")?.GetAttributeValue("content", null)
                                           ?? doc.DocumentNode.SelectSingleNode("//meta[@name='image']")?.GetAttributeValue("content", null);
                            if (!string.IsNullOrWhiteSpace(metaImage) && string.IsNullOrWhiteSpace(item.ImageUrl))
                                item.ImageUrl = metaImage;

                            var pubMeta = doc.DocumentNode.SelectSingleNode("//meta[@property='article:published_time']")?.GetAttributeValue("content", null)
                                        ?? doc.DocumentNode.SelectSingleNode("//meta[@itemprop='datePublished']")?.GetAttributeValue("content", null);
                            if (!string.IsNullOrWhiteSpace(pubMeta))
                            {
                                item.RawTimeText = pubMeta;
                                item.PublishedAt = TryParseDateTime(pubMeta);
                            }

                            if (string.IsNullOrWhiteSpace(item.Author))
                            {
                                var author = doc.DocumentNode.SelectSingleNode("//meta[@name='author']")?.GetAttributeValue("content", null)
                                             ?? doc.DocumentNode.SelectSingleNode("//a[contains(@href,'/author') or contains(@class,'author') or contains(@class,'byline')]")?.InnerText;
                                if (!string.IsNullOrWhiteSpace(author)) item.Author = WebUtility.HtmlDecode(author.Trim());
                            }

                            if (string.IsNullOrWhiteSpace(item.Summary))
                            {
                                var p = doc.DocumentNode.SelectSingleNode("//p[string-length(normalize-space())>50]");
                                if (p != null) item.Summary = WebUtility.HtmlDecode(p.InnerText.Trim());
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"EnrichArticlePartialAsync error for {item.Url}: {ex.Message}");
            }

            if (string.IsNullOrWhiteSpace(item.Source) && Uri.TryCreate(item.Url, UriKind.Absolute, out var u))
                item.Source = u.Host;
        }

        private void MergeEnriched(NewsItem target, NewsItem enriched)
        {
            if (target == null || enriched == null) return;
            if (string.IsNullOrWhiteSpace(target.Headline)) target.Headline = enriched.Headline;
            if (string.IsNullOrWhiteSpace(target.Summary)) target.Summary = enriched.Summary;
            if (string.IsNullOrWhiteSpace(target.ImageUrl)) target.ImageUrl = enriched.ImageUrl;
            if (string.IsNullOrWhiteSpace(target.Author)) target.Author = enriched.Author;
            if (target.PublishedAt == null) target.PublishedAt = enriched.PublishedAt;
            if (string.IsNullOrWhiteSpace(target.RawTimeText)) target.RawTimeText = enriched.RawTimeText;
            if (string.IsNullOrWhiteSpace(target.Source)) target.Source = enriched.Source;
        }

        private bool IsLikelyLIC(NewsItem ni)
        {
            if (ni == null) return false;
            string combined = $"{ni.Headline} {ni.Summary} {ni.Url} {ni.Source} {ni.RawTimeText} {ni.Author}".ToLowerInvariant();
            string[] tokens = new[] { " lic ", " lic,", "/lic", "life insurance", "life-insurance", "licindia", "life insurance corporation", "life insurance corp", "lic's", "lics" };
            foreach (var t in tokens)
                if (combined.Contains(t)) return true;
            if (!string.IsNullOrWhiteSpace(ni.Headline) && ni.Headline.Contains("LIC")) return true;
            return false;
        }

        private DateTime? TryParseDateTime(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return null;
            raw = Regex.Replace(raw, @"\s+", " ").Trim();

            if (DateTimeOffset.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var dto))
                return dto.UtcDateTime;

            if (DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out var dt))
                return DateTime.SpecifyKind(dt, DateTimeKind.Utc);

            var monthPattern = @"\b(January|Jan|February|Feb|March|Mar|April|Apr|May|June|Jun|July|Jul|August|Aug|September|Sep|October|Oct|November|Nov|December|Dec)\b";
            if (Regex.IsMatch(raw, monthPattern, RegexOptions.IgnoreCase))
            {
                var cleaned = Regex.Replace(raw, @"(on|at|by)\b", "", RegexOptions.IgnoreCase).Trim(',', ' ');
                if (DateTime.TryParse(cleaned, out var dt2)) return DateTime.SpecifyKind(dt2, DateTimeKind.Utc);
            }

            var rel = ParseRelativeTime(raw);
            if (rel != null) return rel;

            return null;
        }

        private DateTime? ParseRelativeTime(string raw)
        {
            raw = (raw ?? "").ToLowerInvariant();
            var now = DateTime.UtcNow;

            var m = Regex.Match(raw, @"(\d+)\s*(sec|second|seconds|min|mins|minute|minutes|hr|hrs|hour|hours|day|days|week|weeks|month|months|year|years)\b");
            if (m.Success)
            {
                if (!int.TryParse(m.Groups[1].Value, out int val)) return null;
                var unit = m.Groups[2].Value;
                if (unit.StartsWith("sec")) return now.AddSeconds(-val);
                if (unit.StartsWith("min")) return now.AddMinutes(-val);
                if (unit.StartsWith("hr") || unit.StartsWith("hour")) return now.AddHours(-val);
                if (unit.StartsWith("day")) return now.AddDays(-val);
                if (unit.StartsWith("week")) return now.AddDays(-7 * val);
                if (unit.StartsWith("month")) return now.AddMonths(-val);
                if (unit.StartsWith("year")) return now.AddYears(-val);
            }

            if (raw.Contains("an hour ago") || raw.Contains("a hour ago")) return now.AddHours(-1);
            if (raw.Contains("a day ago") || raw.Contains("yesterday")) return now.AddDays(-1);

            return null;
        }

        private string NormalizeHref(string href, string baseUrl)
        {
            if (string.IsNullOrWhiteSpace(href)) return href;
            href = href.Trim();
            if (href.StartsWith("//")) return new Uri(baseUrl).Scheme + ":" + href;
            if (href.StartsWith("/")) return new Uri(baseUrl).GetLeftPart(UriPartial.Authority) + href;
            if (href.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || href.StartsWith("https://", StringComparison.OrdinalIgnoreCase)) return href;
            try { return new Uri(new Uri(baseUrl), href).ToString(); } catch { return href; }
        }

        public class NewsItem
        {
            public string Headline { get; set; }
            public string Url { get; set; }
            public string ImageUrl { get; set; }
            public string Summary { get; set; }
            public string Author { get; set; }
            public DateTime? PublishedAt { get; set; } // UTC
            public string RawTimeText { get; set; }
            public string Source { get; set; }
        }
    }
}
