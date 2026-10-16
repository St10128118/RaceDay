namespace RaceDay.Api.Models
{
    public class Result
    {
        public int ResultId { get; set; }
        public int EnrolmentId { get; set; }
        public int? FinishTimeSeconds { get; set; }
        public int? PositionOverall { get; set; }
        public int? PositionCategory { get; set; }
        public int? PositionGender { get; set; }
        public string Status { get; set; } = null!;

        public EventEnrolment? Enrolment { get; set; }
    }
}
