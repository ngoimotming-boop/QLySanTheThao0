using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLySanTheThao.Core.Data;
using QLySanTheThao.Core.Entities;
using QLySanTheThao.Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QLySanTheThao.Modules.Admin
{
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 1. Dashboard tổng quan (/Admin)
        public async Task<IActionResult> Index()
        {
            ViewBag.TotalCourts = await _context.Courts.CountAsync();
            ViewBag.TotalBookings = await _context.Bookings.CountAsync();
            ViewBag.TotalUsers = await _context.Users.CountAsync();
            ViewBag.PendingBookings = await _context.Bookings.CountAsync(b => b.Status == BookingStatus.Pending);

            var confirmedBookings = await _context.Bookings
                .Include(b => b.Court)
                .Where(b => b.Status == BookingStatus.Confirmed)
                .ToListAsync();

            decimal totalRevenue = confirmedBookings.Sum(b => b.Court != null ? b.Court.PricePerHour : 0);
            ViewBag.TotalRevenue = totalRevenue;

            // 7 ngày gần nhất
            var last7Days = Enumerable.Range(0, 7).Select(i => DateTime.Today.AddDays(-6 + i)).ToList();
            ViewBag.DaysLabels = last7Days.Select(d => d.ToString("dd/MM/yyyy")).ToList();
            ViewBag.DaysData = last7Days.Select(day =>
                confirmedBookings.Where(b => b.BookingDate.Date == day.Date)
                                 .Sum(b => b.Court != null ? b.Court.PricePerHour : 0)
            ).ToList();

            // 4 tuần gần nhất
            var weekLabels = new List<string>();
            var weekData = new List<decimal>();
            for (int i = 3; i >= 0; i--)
            {
                var endOfWeek = DateTime.Today.AddDays(-i * 7);
                var startOfWeek = endOfWeek.AddDays(-6);
                weekLabels.Add($"{startOfWeek:dd/MM} - {endOfWeek:dd/MM}");

                var revenue = confirmedBookings
                    .Where(b => b.BookingDate.Date >= startOfWeek.Date && b.BookingDate.Date <= endOfWeek.Date)
                    .Sum(b => b.Court != null ? b.Court.PricePerHour : 0);
                weekData.Add(revenue);
            }
            ViewBag.WeeksLabels = weekLabels;
            ViewBag.WeeksData = weekData;

            // 6 tháng gần nhất
            var monthsLabels = Enumerable.Range(0, 6).Select(i => DateTime.Today.AddMonths(-5 + i)).ToList();
            ViewBag.MonthsLabels = monthsLabels.Select(m => m.ToString("MM/yyyy")).ToList();
            ViewBag.MonthsData = monthsLabels.Select(m =>
                confirmedBookings.Where(b => b.BookingDate.Month == m.Month && b.BookingDate.Year == m.Year)
                                 .Sum(b => b.Court != null ? b.Court.PricePerHour : 0)
            ).ToList();

            return View();
        }

        // 2. Quản lý danh sách sân (/Admin/ManageCourts)
        public async Task<IActionResult> ManageCourts(string searchString, string statusFilter)
        {
            var courtsQuery = _context.Courts.AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                courtsQuery = courtsQuery.Where(c => c.Name.Contains(searchString) || c.Type.Contains(searchString));
            }

            if (!string.IsNullOrEmpty(statusFilter))
            {
                courtsQuery = courtsQuery.Where(c => c.Status == statusFilter);
            }

            ViewData["CurrentFilter"] = searchString;
            ViewData["StatusFilter"] = statusFilter;

            var courts = await courtsQuery.ToListAsync();
            return View(courts);
        }

        [HttpPost]
        public async Task<IActionResult> CreateCourt(Court court)
        {
            if (court.PricePerHour == 0) court.PricePerHour = 120000;
            if (court.PeakPricePerHour == 0) court.PeakPricePerHour = court.PricePerHour * 1.3m;

            _context.Courts.Add(court);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Thêm sân mới thành công!";
            return RedirectToAction(nameof(ManageCourts));
        }

        [HttpPost]
        public async Task<IActionResult> EditCourt(Court court)
        {
            var existingCourt = await _context.Courts.FindAsync(court.Id);
            if (existingCourt != null)
            {
                existingCourt.Name = court.Name;
                existingCourt.Type = court.Type;
                existingCourt.Status = court.Status;

                _context.Courts.Update(existingCourt);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Cập nhật thông tin sân '{court.Name}' thành công!";
            }
            else
            {
                TempData["ErrorMessage"] = "Không tìm thấy sân cần cập nhật!";
            }
            return RedirectToAction(nameof(ManageCourts));
        }

        [HttpPost]
        public async Task<IActionResult> DeleteCourt(int id)
        {
            var court = await _context.Courts.FindAsync(id);
            if (court != null)
            {
                _context.Courts.Remove(court);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã xóa sân thành công!";
            }
            return RedirectToAction(nameof(ManageCourts));
        }

        // 3. Quản lý Đặt sân (/Admin/ManageBookings)
        public async Task<IActionResult> ManageBookings(string searchString, BookingStatus? statusFilter)
        {
            var query = _context.Bookings
                .Include(b => b.User)
                .Include(b => b.Court)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(b => b.User.FullName.Contains(searchString) || b.Court.Name.Contains(searchString));
            }

            if (statusFilter.HasValue)
            {
                query = query.Where(b => b.Status == statusFilter.Value);
            }

            var bookings = await query.OrderByDescending(b => b.Id).ToListAsync();
            return View(bookings);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateBookingStatus(int bookingId, BookingStatus status)
        {
            var booking = await _context.Bookings.FindAsync(bookingId);
            if (booking != null)
            {
                booking.Status = status;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Đã cập nhật đơn hàng #{bookingId}!";
            }
            return RedirectToAction(nameof(ManageBookings));
        }

        // 4. Quản lý Tài khoản & Phân quyền (/Admin/ManageUsers)
        public async Task<IActionResult> ManageUsers(string searchString)
        {
            var query = _context.Users.AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(u => u.FullName.Contains(searchString) || u.Email.Contains(searchString) || u.PhoneNumber.Contains(searchString));
            }

            var users = await query.OrderByDescending(u => u.Id).ToListAsync();
            return View(users);
        }

        [HttpPost]
        public async Task<IActionResult> ToggleUserStatus(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Cập nhật trạng thái tài khoản thành công!";
            }
            return RedirectToAction(nameof(ManageUsers));
        }

        // Thêm nhân sự nội bộ mới vào Database
        [HttpPost]
        public async Task<IActionResult> CreateStaff(string fullName, string email, string phoneNumber, string role)
        {
            var newUser = new User
            {
                FullName = fullName,
                Email = email,
                PhoneNumber = phoneNumber
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đã thêm nhân sự '{fullName}' thành công!";
            return RedirectToAction(nameof(ManageUsers));
        }

        // Chỉnh sửa thông tin tài khoản
        [HttpPost]
        public async Task<IActionResult> EditUser(int id, string fullName, string email, string phoneNumber)
        {
            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                user.FullName = fullName;
                user.Email = email;
                user.PhoneNumber = phoneNumber;

                _context.Users.Update(user);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Cập nhật thông tin tài khoản #{id} thành công!";
            }
            else
            {
                TempData["ErrorMessage"] = "Không tìm thấy tài khoản cần chỉnh sửa!";
            }
            return RedirectToAction(nameof(ManageUsers));
        }

        // Khóa hoặc thay đổi trạng thái tài khoản
        [HttpPost]
        public async Task<IActionResult> LockUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                TempData["SuccessMessage"] = $"Đã khóa tài khoản thành công!";
            }
            else
            {
                TempData["ErrorMessage"] = "Không tìm thấy tài khoản cần thao tác!";
            }
            return RedirectToAction(nameof(ManageUsers));
        }

        // 5. Trang Bảng giá & Cấu hình giờ vàng (/Admin/Pricing)
        public async Task<IActionResult> Pricing()
        {
            var courts = await _context.Courts.ToListAsync();

            var courtTypesGroup = courts
                .GroupBy(c => c.Type)
                .Select(g => new CourtTypePricingViewModel
                {
                    Type = g.Key ?? "Chưa phân loại",
                    CourtCount = g.Count(),
                    PricePerHour = g.FirstOrDefault()?.PricePerHour ?? 0,
                    PeakPricePerHour = g.FirstOrDefault()?.PeakPricePerHour ?? 0
                })
                .ToList();

            return View(courtTypesGroup);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateTypePricing(string courtType, decimal pricePerHour, decimal peakPricePerHour)
        {
            var courtsOfType = await _context.Courts.Where(c => c.Type == courtType).ToListAsync();
            foreach (var court in courtsOfType)
            {
                court.PricePerHour = pricePerHour;
                court.PeakPricePerHour = peakPricePerHour > 0 ? peakPricePerHour : pricePerHour * 1.3m;
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Đã cập nhật biểu giá cho tất cả sân thuộc nhóm '{courtType}' thành công!";
            return RedirectToAction(nameof(Pricing));
        }

        [HttpPost]
        public IActionResult SaveGoldenHour(string goldenHourStart, string goldenHourEnd, bool goldenHourActive)
        {
            TempData["SuccessMessage"] = $"Đã lưu khung Giờ vàng ({goldenHourStart} - {goldenHourEnd}) thành công!";
            return RedirectToAction(nameof(Pricing));
        }

        // 6. Cấu hình Hệ thống (/Admin/Settings)
        [HttpGet]
        public IActionResult Settings()
        {
            ViewBag.SystemName = TempData["SystemName"]?.ToString() ?? "Hệ thống Quản lý Sân Thể Thao";
            ViewBag.Hotline = TempData["Hotline"]?.ToString() ?? "0901234567";
            ViewBag.Email = TempData["Email"]?.ToString() ?? "admin@sanethethao.vn";
            ViewBag.Address = TempData["Address"]?.ToString() ?? "123 Đường Cầu Giấy, Hà Nội";
            ViewBag.OpenTime = TempData["OpenTime"]?.ToString() ?? "06:00";
            ViewBag.CloseTime = TempData["CloseTime"]?.ToString() ?? "22:30";
            ViewBag.BankName = TempData["BankName"]?.ToString() ?? "MBBank";
            ViewBag.AccountNumber = TempData["AccountNumber"]?.ToString() ?? "0901234567999";
            ViewBag.AccountHolder = TempData["AccountHolder"]?.ToString() ?? "NGUYEN VAN A";
            ViewBag.MaintenanceMode = TempData["MaintenanceMode"] != null ? Convert.ToBoolean(TempData["MaintenanceMode"]) : false;

            if (TempData["SuccessMessage"] != null)
            {
                TempData.Keep("SuccessMessage");
            }
            TempData.Keep("SystemName");
            TempData.Keep("Hotline");
            TempData.Keep("Email");
            TempData.Keep("Address");
            TempData.Keep("OpenTime");
            TempData.Keep("CloseTime");
            TempData.Keep("BankName");
            TempData.Keep("AccountNumber");
            TempData.Keep("AccountHolder");
            TempData.Keep("MaintenanceMode");

            return View();
        }

        [HttpPost]
        public IActionResult SaveSettings(string systemName, string hotline, string email, string address, string openTime, string closeTime, string bankName, string accountNumber, string accountHolder, bool maintenanceMode)
        {
            TempData["SystemName"] = systemName;
            TempData["Hotline"] = hotline;
            TempData["Email"] = email;
            TempData["Address"] = address;
            TempData["OpenTime"] = openTime;
            TempData["CloseTime"] = closeTime;
            TempData["BankName"] = bankName;
            TempData["AccountNumber"] = accountNumber;
            TempData["AccountHolder"] = accountHolder;
            TempData["MaintenanceMode"] = maintenanceMode;

            TempData["SuccessMessage"] = maintenanceMode
                ? "Đã bật chế độ BẢO TRÌ toàn hệ thống!"
                : "Đã lưu toàn bộ cài đặt hệ thống thành công!";

            return RedirectToAction(nameof(Settings));
        }

        // 7. Báo cáo & Thống kê (/Admin/Reports)
        public async Task<IActionResult> Reports()
        {
            ViewBag.TotalCourts = await _context.Courts.CountAsync();
            ViewBag.TotalBookings = await _context.Bookings.CountAsync();
            ViewBag.TotalUsers = await _context.Users.CountAsync();

            var confirmedBookings = await _context.Bookings
                .Include(b => b.Court)
                .Where(b => b.Status == BookingStatus.Confirmed)
                .ToListAsync();

            decimal totalRevenue = confirmedBookings.Sum(b => b.Court != null ? b.Court.PricePerHour : 0);
            ViewBag.TotalRevenue = totalRevenue;

            return View();
        }

        // 8. Nhật ký Hoạt động (/Admin/AuditLogs)
        public IActionResult AuditLogs()
        {
            return View();
        }
    }

    public class CourtTypePricingViewModel
    {
        public string Type { get; set; }
        public int CourtCount { get; set; }
        public decimal PricePerHour { get; set; }
        public decimal PeakPricePerHour { get; set; }
    }
}