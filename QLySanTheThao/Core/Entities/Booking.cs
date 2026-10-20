using QLySanTheThao.Core.Enums;

namespace QLySanTheThao.Core.Entities
{
    public class Booking
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public User? User { get; set; }

        public int CourtId { get; set; }
        public Court? Court { get; set; }

        public DateTime BookingDate { get; set; }
        public string StartTime { get; set; } = string.Empty;
        public string EndTime { get; set; } = string.Empty;

        public decimal TotalPrice { get; set; } // Tổng tiền của đơn đặt sân (Mới thêm để thống kê)

        public BookingStatus Status { get; set; }
    }
}