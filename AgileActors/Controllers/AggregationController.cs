using Microsoft.AspNetCore.Mvc;
using AgileActors.Models;
using AgileActors.Services;

namespace AgileActors.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AggregationController : ControllerBase
    {
        private readonly IEnumerable<IAggregatorService> _services;

        public AggregationController(IEnumerable<IAggregatorService> services)
        {
            _services = services;
        }

        [HttpGet]
        public async Task<IActionResult> GetAggregatedData(
      [FromQuery] string keyword = "Athens",
      [FromQuery] int count = 5,
      [FromQuery] int page = 1,
      [FromQuery] string? sortBy = "date",
      [FromQuery] string? fromDate = "2025-05-02",
      [FromQuery] string services = "news,spotify,weather")
        {
            // If fromDate is null or empty, use today's date
            var fromDateParsed = string.IsNullOrWhiteSpace(fromDate)
                ? DateTime.Today
                : DateTime.Parse(fromDate);

            var selectedServices = services?
                .Split(',')
                .Select(s => s.Trim().ToLower())
                .ToList();

            var filteredServices = _services
                .Where(s => selectedServices == null || selectedServices.Contains(s.Name.ToLower()))

                .ToList();

            List<KeyValuePair<string, AggregatedResponse>> results;

            if (filteredServices.Count <= 1)
            {
                results = new List<KeyValuePair<string, AggregatedResponse>>();
                foreach (var service in filteredServices)
                {
                    var response = await service.FetchDataAsync(keyword, count, page, sortBy ?? "publishedAt", fromDateParsed);
                    var serviceKey = service.Name.ToLower();

                    results.Add(new KeyValuePair<string, AggregatedResponse>(serviceKey, response));
                }
            }
            else
            {
                var tasks = filteredServices.Select(async service =>
                {
                    var response = await service.FetchDataAsync(keyword, count, page, sortBy ?? "publishedAt", fromDateParsed);
                    var serviceKey = service.Name;




                    return new KeyValuePair<string, AggregatedResponse>(serviceKey, response);
                });

                results = (await Task.WhenAll(tasks)).ToList();
            }

            var grouped = results.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            return Ok(grouped);
        }
    }
}
