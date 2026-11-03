using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuanLyLichHoc.Data;
using QuanLyLichHoc.Infrastructure;
using QuanLyLichHoc.Models;

namespace QuanLyLichHoc.Controllers
{
    /// <summary>Thời khóa biểu dạng lưới. Dữ liệu được nạp bằng AJAX từ Web API.</summary>
    public class TimetableController : Controller
    {
        private readonly AppDbContext db;
        public TimetableController(AppDbContext db) { this.db = db; }

        private async Task<string> MaCuaToiAsync()
        {
            var claim = User.FindFirst("Ma")?.Value;
            if (!string.IsNullOrEmpty(claim)) return claim;
            int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id);
            string ten = (User.Identity?.Name ?? "").ToLower();
            if (User.IsInRole(VaiTro.SinhVien)) return await db.SinhViens.Where(s => s.MaND == id || s.MaSV.ToLower() == ten).Select(s => s.MaSV).FirstOrDefaultAsync() ?? "";
            if (User.IsInRole(VaiTro.GiangVien)) return await db.GiangViens.Where(g => g.MaND == id || g.MaGV.ToLower() == ten).Select(g => g.MaGV).FirstOrDefaultAsync() ?? "";
            return "";
        }

        private async Task PrepareAsync(int maHK, int tuan, string endpoint)
        {
            var hocKys = await db.HocKys.AsNoTracking().OrderByDescending(h => h.NgayBatDau).ToListAsync();
            var today = DateTime.Today;
            var hk = hocKys.FirstOrDefault(h => h.MaHK == maHK)
                     ?? hocKys.FirstOrDefault(h => h.NgayBatDau <= today && today <= h.NgayKetThuc) ?? hocKys.FirstOrDefault();
            ViewBag.HocKys = new SelectList(hocKys, "MaHK", "Ten", hk?.MaHK);
            ViewBag.MaHK = hk?.MaHK ?? 0;
            ViewBag.Tuan = tuan > 0 ? Math.Clamp(tuan, 1, 20) : TkbHelper.TuanHienTai(hk);
            ViewBag.Endpoint = endpoint;
        }

        /// <summary>Thời khóa biểu của chính người đăng nhập (sinh viên / giảng viên).</summary>
        [Authorize(Roles = "SinhVien,GiangVien")]
        public async Task<IActionResult> Index(int maHK = 0, int tuan = 0)
        {
            string ma = await MaCuaToiAsync();
            await PrepareAsync(maHK, tuan, User.IsInRole(VaiTro.SinhVien) ? $"/api/students/{ma}/timetable" : $"/api/lecturers/{ma}/timetable");
            return View();
        }

        /// <summary>Admin / Giáo vụ xem thời khóa biểu của một sinh viên.</summary>
        [Authorize(Roles = "GiaoVu,Admin")]
        public async Task<IActionResult> Student(string maSV, int maHK = 0, int tuan = 0)
        {
            var sv = await db.SinhViens.AsNoTracking().FirstOrDefaultAsync(s => s.MaSV == maSV);
            if (sv == null) return NotFound();
            var lop = await db.Lops.AsNoTracking().Where(l => l.MaLop == sv.MaLop).Select(l => l.TenLop).FirstOrDefaultAsync();
            await PrepareAsync(maHK, tuan, $"/api/students/{sv.MaSV}/timetable");
            ViewBag.Heading = $"Thời khóa biểu của {sv.HoTen} ({sv.MaSV}{(lop != null ? " – " + lop : "")})";
            return View("Index");
        }
    }
}
