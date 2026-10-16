using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.DTOs.Events
{
    public class EventUpdateDto
    {
        [StringLength(200, MinimumLength = 1)]
        public string? Name { get; set; }
        public string? Description { get; set; }
        [StringLength(50, MinimumLength = 1)]
        public string? EventType { get; set; }
        [StringLength(200, MinimumLength = 1)]
        public string? Location { get; set; }
        [StringLength(100, MinimumLength = 1)]
        public string? City { get; set; }
        [StringLength(100, MinimumLength = 1)]
        public string? Province { get; set; }
        [Range(typeof(decimal), "0.01", "9999.99")]
        public decimal? DistanceKm { get; set; }
        public System.DateTime? EventDateTime { get; set; }
        [StringLength(50, MinimumLength = 1)]
        public string? Status { get; set; }
        [Range(1, int.MaxValue)]
        public int? MaxParticipants { get; set; }
    }
}
