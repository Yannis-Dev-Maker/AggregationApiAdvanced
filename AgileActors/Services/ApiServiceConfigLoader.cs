using AgileActors.Models;
using System.Data.OleDb;
using System.Text.Json;
using System.Web;
using AgileActors.Services; // Needed for ApiPostProcessorRegistry

namespace AgileActors.Services
{
    public static class ApiServiceConfigLoader
    {
        public static List<ApiServiceDefinition> LoadServices()
        {
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config", "db", "apiservices.accdb");
            var connectionString = $"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={path};Persist Security Info=False;";

            var services = new List<ApiServiceDefinition>();

            using var connection = new OleDbConnection(connectionString);
            connection.Open();

            var command = new OleDbCommand("SELECT * FROM services WHERE Enabled = True", connection);
            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                var definition = new ApiServiceDefinition
                {
                    ServiceName = reader["ServiceName"]?.ToString() ?? "",
                    BaseUrl = reader["BaseUrl"]?.ToString() ?? "",
                    QueryTemplate = reader["QueryTemplate"]?.ToString() ?? "",
                    AuthType = reader["AuthType"]?.ToString() ?? "",
                    ApiKey = reader["ApiKey"] is DBNull ? null : reader["ApiKey"].ToString(),
                    ClientId = reader["ClientId"] is DBNull ? null : reader["ClientId"].ToString(),
                    ClientSecret = reader["ClientSecret"] is DBNull ? null : reader["ClientSecret"].ToString(),
                    TokenUrl = reader["TokenUrl"] is DBNull ? null : reader["TokenUrl"].ToString(),
                    Enabled = reader["Enabled"] is bool b && b,
                    ItemPath = reader["ItemPath"]?.ToString() ?? "",
                    TitlePath = reader["TitlePath"]?.ToString() ?? "",
                    DescriptionPath = reader["DescriptionPath"]?.ToString() ?? "",
                    UrlPath = reader["UrlPath"]?.ToString() ?? "",
                    ImagePath = reader["ImagePath"]?.ToString() ?? "",
                    DatePath = reader["DatePath"]?.ToString() ?? "",
                    TotalCountPath = reader["TotalCountPath"]?.ToString() ?? ""
                };

                // 🔄 Dynamically assign post-processor if defined
                var postProcessorName = reader["PostProcessor"]?.ToString();
                var processor = ApiPostProcessorRegistry.GetProcessor(postProcessorName);

                if (processor != null)
                {
                    Console.WriteLine($"✔ PostProcessor '{postProcessorName}' resolved for {definition.ServiceName}");
                    definition.PostProcess = (result, item, keyword) =>
                    {
                        Console.WriteLine($"➡ Invoking PostProcessor '{postProcessorName}' for keyword '{keyword}'");
                        return processor.Process(result, item, keyword);
                    };
                }
                else if (!string.IsNullOrWhiteSpace(postProcessorName))
                {
                    Console.WriteLine($"⚠ PostProcessor '{postProcessorName}' NOT found for {definition.ServiceName}");
                }

                Console.WriteLine($"📦 Loaded service: {definition.ServiceName}, ItemPath = '{definition.ItemPath}', TotalCountPath = '{definition.TotalCountPath}', PostProcessor = '{postProcessorName}'");

                services.Add(definition);
            }

            return services;
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
