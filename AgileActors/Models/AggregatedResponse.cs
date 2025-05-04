using System.Collections.Generic;

namespace AgileActors.Models
{
    public class AggregatedResponse
    {
        public List<AggregatedResult> Results { get; set; } = new();
        public int TotalCount { get; set; } = 0;
    }
}
