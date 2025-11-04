namespace MbabaneHighlandersBackend2.Model
{
    public class Fixture
    {
        public int Id { get; set; }
        public DateOnly Date { get; set; }
        public TimeOnly Time { get; set; }
        public string HomeTeam { get; set; } = string.Empty;
        public string AwayTeam { get; set; } = string.Empty;
        public string? Stadium { get; set; }
        public DateTime CreatedUtc { get; set; }

       
    }
}
