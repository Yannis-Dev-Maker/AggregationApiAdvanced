using AgileActors.Models;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Web;

namespace AgileActors.Services
{
    public class GenericApiService : IAggregatorService
    {
        private readonly ApiServiceDefinition _definition;
        private readonly HttpClient _httpClient;
        private readonly ILogger<GenericApiService> _logger;

        public string Name => _definition.ServiceName;

        public GenericApiService(ApiServiceDefinition definition, ILogger<GenericApiService> logger)
        {
            _definition = definition;
            _logger = logger;
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("AgileAggregator/2.0");
        }

        public async Task<AggregatedResponse> FetchDataAsync(
            string keyword,
            int count = 5,
            int page = 1,
            string sortBy = "publishedAt",
            DateTime? fromDate = null)
        {
            string url = BuildUrl(keyword, count, page, sortBy, fromDate);
            _logger.LogInformation("Requesting URL: {Url}", url);

            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                await ApplyAuthenticationAsync(request);

                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var stream = await response.Content.ReadAsStreamAsync();
                using var json = await JsonDocument.ParseAsync(stream);

                return ParseDynamicJson(json, keyword);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch data for service {Service}", _definition.ServiceName);

                return new AggregatedResponse
                {
                    Results = new List<AggregatedResult>
                    {
                        new AggregatedResult
                        {
                            Source = _definition.ServiceName,
                            Title = $"Error fetching {_definition.ServiceName}",
                            Description = ex.Message,
                            Date = DateTime.UtcNow,
                            Url = _definition.BaseUrl
                        }
                    },
                    TotalCount = 0
                };
            }
        }

        private string BuildUrl(string keyword, int count, int page, string sortBy, DateTime? fromDate)
        {
            int offset = (page - 1) * count;

            string query = _definition.QueryTemplate
                .Replace("{keyword}", HttpUtility.UrlEncode(keyword))
                .Replace("{count}", count.ToString())
                .Replace("{page}", page.ToString())
                .Replace("{offset}", offset.ToString())
                .Replace("{sortBy}", HttpUtility.UrlEncode(sortBy))
                .Replace("{fromDate}", (fromDate ?? DateTime.UtcNow).ToString("yyyy-MM-dd"))
                .Replace("{apiKey}", _definition.ApiKey ?? "")
                .Replace("{clientId}", _definition.ClientId ?? "")
                .Replace("{clientSecret}", _definition.ClientSecret ?? "");

            return $"{_definition.BaseUrl?.TrimEnd('/')}{query}";
        }

        private async Task ApplyAuthenticationAsync(HttpRequestMessage request)
        {
            if (_definition.AuthType == "ClientCredentials")
            {
                var token = await GetAccessTokenFromClientCredentials();
                if (!string.IsNullOrEmpty(token))
                {
                    request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                }
            }
            else if (_definition.AuthType == "ApiKey" && !string.IsNullOrWhiteSpace(_definition.ApiKey))
            {
                request.Headers.Add("X-Api-Key", _definition.ApiKey);
            }
        }

        private async Task<string?> GetAccessTokenFromClientCredentials()
        {
            if (string.IsNullOrWhiteSpace(_definition.TokenUrl))
                return null;

            var credentials = $"{_definition.ClientId}:{_definition.ClientSecret}";
            var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(credentials));

            var tokenRequest = new HttpRequestMessage(HttpMethod.Post, _definition.TokenUrl);
            tokenRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", encoded);
            tokenRequest.Content = new StringContent("grant_type=client_credentials", Encoding.UTF8, "application/x-www-form-urlencoded");

            var response = await _httpClient.SendAsync(tokenRequest);
            if (!response.IsSuccessStatusCode)
                return null;

            using var stream = await response.Content.ReadAsStreamAsync();
            using var json = await JsonDocument.ParseAsync(stream);

            return json.RootElement.GetProperty("access_token").GetString();
        }

        private AggregatedResponse ParseDynamicJson(JsonDocument json, string keyword)
        {
            var results = new List<AggregatedResult>();
            var root = json.RootElement;

            _logger.LogInformation("Root JSON keys: {Keys}", string.Join(", ", root.EnumerateObject().Select(p => p.Name)));

            string itemPath = _definition.ItemPath?.Trim() ?? "";
            _logger.LogInformation("Using ItemPath: '{ItemPath}'", itemPath);
            _logger.LogInformation("Raw JSON:\n{Json}", root.ToString());

            try
            {
                if (string.IsNullOrEmpty(itemPath))
                {
                    _logger.LogInformation("No ItemPath defined — treating root object as single result.");
                    var result = CreateAggregatedResult(root, keyword);

                    if (_definition.PostProcess != null)
                    {
                        _logger.LogInformation("Applying PostProcess for service {Service} (single object)", _definition.ServiceName);
                        result = _definition.PostProcess(result, root, keyword);
                    }

                    results.Add(result);
                }
                else
                {
                    var itemArrayElement = ResolveJsonPath(root, itemPath);

                    if (itemArrayElement.HasValue && itemArrayElement.Value.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var item in itemArrayElement.Value.EnumerateArray())
                        {
                            var result = CreateAggregatedResult(item, keyword);
                            if (_definition.PostProcess != null)
                            {
                                _logger.LogInformation("Applying PostProcess for service {Service} (array item)", _definition.ServiceName);
                                result = _definition.PostProcess(result, item, keyword);
                            }
                            results.Add(result);
                        }
                    }
                    else if (itemArrayElement.HasValue && itemArrayElement.Value.ValueKind == JsonValueKind.Object)
                    {
                        _logger.LogInformation("ItemPath points to an object, wrapping in array.");
                        var result = CreateAggregatedResult(itemArrayElement.Value, keyword);
                        if (_definition.PostProcess != null)
                        {
                            _logger.LogInformation("Applying PostProcess for service {Service} (single object from ItemPath)", _definition.ServiceName);
                            result = _definition.PostProcess(result, itemArrayElement.Value, keyword);
                        }
                        results.Add(result);
                    }
                    else
                    {
                        _logger.LogWarning("Expected array or object at path '{Path}' but got something else or null.", itemPath);

                        results.Add(new AggregatedResult
                        {
                            Source = _definition.ServiceName,
                            Title = "No data array found",
                            Description = $"Expected array or object at path '{itemPath}', but got null or non-object.",
                            Date = DateTime.UtcNow,
                            Url = _definition.BaseUrl
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while parsing JSON for {Service}", _definition.ServiceName);

                results.Add(new AggregatedResult
                {
                    Source = _definition.ServiceName,
                    Title = $"Error parsing {_definition.ServiceName}",
                    Description = ex.Message,
                    Date = DateTime.UtcNow,
                    Url = ""
                });
            }

            int total = results.Count;
            if (!string.IsNullOrEmpty(_definition.TotalCountPath))
            {
                var totalElement = ResolveJsonPath(root, _definition.TotalCountPath);
                if (totalElement.HasValue && totalElement.Value.ValueKind == JsonValueKind.Number)
                {
                    total = totalElement.Value.GetInt32();
                }
            }

            return new AggregatedResponse
            {
                Results = results,
                TotalCount = total
            };
        }

        private AggregatedResult CreateAggregatedResult(JsonElement item, string keyword)
        {
            var icon = ResolveJsonPath(item, _definition.ImagePath)?.GetString();

            return new AggregatedResult
            {
                Source = _definition.ServiceName,
                Title = ResolveJsonPath(item, _definition.TitlePath)?.GetString() ?? keyword,
                Description = ResolveJsonPath(item, _definition.DescriptionPath)?.GetString() ?? "",
                Url = ResolveJsonPath(item, _definition.UrlPath)?.GetString() ?? "",
                ImageUrl = !string.IsNullOrWhiteSpace(icon) ? $"https://openweathermap.org/img/wn/{icon}@2x.png" : null,
                Date = TryParseDate(ResolveJsonPath(item, _definition.DatePath))
            };
        }

        private JsonElement? ResolveJsonPath(JsonElement element, string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return null;

            var parts = path.Split('.');
            JsonElement current = element;

            foreach (var part in parts)
            {
                if (part.Contains("["))
                {
                    var name = part.Substring(0, part.IndexOf('['));
                    var indexStr = part.Substring(part.IndexOf('[') + 1).TrimEnd(']');
                    if (current.TryGetProperty(name, out var arrayElement) &&
                        int.TryParse(indexStr, out int index) &&
                        arrayElement.ValueKind == JsonValueKind.Array)
                    {
                        var arr = arrayElement.EnumerateArray().ToList();
                        if (index < arr.Count)
                            current = arr[index];
                        else
                            return null;
                    }
                    else return null;
                }
                else if (current.TryGetProperty(part, out var next))
                {
                    current = next;
                }
                else return null;
            }

            return current;
        }

        private DateTime TryParseDate(JsonElement? dateElement)
        {
            if (dateElement.HasValue && dateElement.Value.ValueKind == JsonValueKind.String)
            {
                if (DateTime.TryParse(dateElement.Value.GetString(), out var dt))
                    return dt;
            }
            else if (dateElement.HasValue && dateElement.Value.ValueKind == JsonValueKind.Number)
            {
                if (dateElement.Value.TryGetInt64(out var timestamp))
                {
                    return DateTimeOffset.FromUnixTimeSeconds(timestamp).UtcDateTime;
                }
            }
            return DateTime.UtcNow;
        }
    }
}
