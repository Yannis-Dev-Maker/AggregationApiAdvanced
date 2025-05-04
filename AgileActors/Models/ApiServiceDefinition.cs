using System.Text.Json;

namespace AgileActors.Models
{
    public class ApiServiceDefinition
    {
        public string ServiceName { get; set; } = "";
        public string BaseUrl { get; set; } = "";
        public string QueryTemplate { get; set; } = "";
        public string AuthType { get; set; } = "";
        public string? ApiKey { get; set; }
        public string? ClientId { get; set; }
        public string? ClientSecret { get; set; }
        public string? TokenUrl { get; set; }
        public bool Enabled { get; set; }
        public string? ItemPath { get; set; }
        public string? TitlePath { get; set; }
        public string? DescriptionPath { get; set; }
        public string? UrlPath { get; set; }
        public string? ImagePath { get; set; }
        public string? DatePath { get; set; }
        public string? TotalCountPath { get; set; }

        public string? PostProcessor { get; set; } //holds the name of the processor (as configured in my Access DB)



        // ✅ Accept JsonElement by readonly reference to match usage with .Value. Actual function to call when building results
        public Func<AggregatedResult, JsonElement, string, AggregatedResult>? PostProcess { get; set; }

    }
}
