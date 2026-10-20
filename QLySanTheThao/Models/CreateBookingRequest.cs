namespace DatSanCauLong.Models
{
    public class CreateBookingRequest
    {
        public int CourtId { get; set; }

        public DateTime Date { get; set; }

        public List<string> StartTimes { get; set; } = new();

        public string CustomerPhone { get; set; } = "";
    }
}
