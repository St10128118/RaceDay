namespace RaceDay.Api.Models
{
    public class Route
    {
        public int RouteId { get; set; }
        public int EventId { get; set; }
        public string Name { get; set; } = null!;
        public decimal DistanceKm { get; set; }
        public string? GPXUrl { get; set; }
        public int? ElevationGainMeters { get; set; }

        public Event? Event { get; set; }
    }
}
