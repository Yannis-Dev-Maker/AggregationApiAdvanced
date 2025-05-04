namespace AgileActors.Models
{
    public class AggregatedResult
    {
        public string? Source { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public DateTime Date { get; set; }
        public string? Url { get; set; }
        public string? ImageUrl { get; set; }  // ✅ New field
    }
}
