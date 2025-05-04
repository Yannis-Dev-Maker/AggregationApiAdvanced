using AgileActors.Models;
using System.Text.Json;

namespace AgileActors.Services
{
    public interface IApiPostProcessor
    {
        AggregatedResult Process(AggregatedResult result, JsonElement item, string keyword);
    }
}
