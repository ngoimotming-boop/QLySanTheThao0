namespace QLySanTheThao.Core.Entities
{
    public class User
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty; // <-- Thêm dòng này vào
        public string PhoneNumber { get; set; } = string.Empty;
        public string Role { get; set; } = "Customer";
        
        public string Status { get; set; } = "Pending"; // Mặc định là Pending (Chờ duyệt)
    }
}
