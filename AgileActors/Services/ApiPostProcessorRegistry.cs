using AgileActors.PostProcessors;

namespace AgileActors.Services
{
    public static class ApiPostProcessorRegistry
    {
        private static readonly Dictionary<string, IApiPostProcessor> _processors =
            new(StringComparer.OrdinalIgnoreCase)
            {
                { "WeatherPostProcessor", new WeatherPostProcessor() }
                // Add more mappings here in future
            };

        public static IApiPostProcessor? GetProcessor(string? key)
        {
            if (string.IsNullOrWhiteSpace(key)) return null;
            return _processors.TryGetValue(key, out var processor) ? processor : null;
        }
    }
}
