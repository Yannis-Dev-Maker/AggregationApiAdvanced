using AgileActors.Models;

namespace AgileActors.Services
{
    public interface IAggregatorService
    {
        string Name { get; }

        Task<AggregatedResponse> FetchDataAsync(
            string keyword,
            int count = 5,
            int page = 1,
            string sortBy = "publishedAt",
            DateTime? fromDate = null
        );
    }
}
