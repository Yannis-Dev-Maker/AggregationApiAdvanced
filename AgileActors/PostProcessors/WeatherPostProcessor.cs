using AgileActors.Models;
using AgileActors.Services;
using System.Text.Json;
using System.Web;

namespace AgileActors.PostProcessors
{
    public class WeatherPostProcessor : IApiPostProcessor
    {
        public AggregatedResult Process(AggregatedResult result, JsonElement item, string keyword)
        {
            var main = ResolvePath(item, "weather[0].main") ?? "";
            var desc = ResolvePath(item, "weather[0].description") ?? "";
            var icon = ResolvePath(item, "weather[0].icon");

            // ✅ Title includes location
            result.Title = $"{main} in {keyword}";
            result.Description = desc;

            // ✅ Append temperature if present
            if (item.TryGetProperty("main", out var mainObj) &&
                mainObj.TryGetProperty("temp", out var temp) &&
                temp.ValueKind == JsonValueKind.Number)
            {
                var tempFormatted = temp.GetDouble().ToString("0.#").Replace('.', ',');
                result.Description += $" ({tempFormatted}°C)";
            }

            // ✅ Weather icon
            result.ImageUrl = !string.IsNullOrWhiteSpace(icon)
                ? $"https://openweathermap.org/img/wn/{icon}@2x.png"
                : null;

            // ✅ Link to OpenWeather search for keyword
            result.Url = $"https://openweathermap.org/find?q={HttpUtility.UrlEncode(keyword)}";

            return result;
        }

        private static string? ResolvePath(JsonElement element, string path)
        {
            var parts = path.Split('.');
            JsonElement current = element;

            foreach (var part in parts)
            {
                if (part.Contains("["))
                {
                    var name = part[..part.IndexOf('[')];
                    var indexStr = part[(part.IndexOf('[') + 1)..^1];

                    if (current.TryGetProperty(name, out var arrayElement) &&
                        int.TryParse(indexStr, out var index) &&
                        arrayElement.ValueKind == JsonValueKind.Array &&
                        index < arrayElement.GetArrayLength())
                    {
                        current = arrayElement[index];
                    }
                    else return null;
                }
                else if (current.TryGetProperty(part, out var next))
                {
                    current = next;
                }
                else return null;
            }

            return current.ValueKind == JsonValueKind.String
                ? current.GetString()
                : current.ToString();
        }
    }
}
