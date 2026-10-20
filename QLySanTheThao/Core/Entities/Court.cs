namespace QLySanTheThao.Core.Entities
{
    public class Court
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;        // Tên sân
        public string Type { get; set; } = "Sân đôi";           // Loại sân
        public decimal PricePerHour { get; set; }              // Giá tiền / giờ (giờ thường)
        public decimal PeakPricePerHour { get; set; }          // Giá giờ vàng / Cuối tuần (Mới thêm)
        public string Status { get; set; }

        public ICollection<Booking>? Bookings { get; set; }
    }
}