using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuanLyLichHoc.Data;
using QuanLyLichHoc.Models;
using QuanLyLichHoc.Services;

namespace QuanLyLichHoc.Controllers
{
    /// <summary>Thời khóa biểu dạng lưới của sinh viên / giảng viên. Dữ liệu được nạp bằng AJAX từ Web API.</summary>
    [Authorize(Roles = "SinhVien,GiangVien")]
    public class TimetableController : Controller
    {
        private readonly AppDbContext db;
        private readonly ScheduleService schedule;
        public TimetableController(AppDbContext db, ScheduleService schedule) { this.db = db; this.schedule = schedule; }

        public async Task<IActionResult> Index(int maHK = 0, int tuan = 0)
        {
            var hocKys = await db.HocKys.AsNoTracking().OrderByDescending(h => h.NgayBatDau).ToListAsync();
            var hk = hocKys.FirstOrDefault(h => h.MaHK == maHK) ?? await schedule.HocKyHienTaiAsync();
            ViewBag.HocKys = new SelectList(hocKys, "MaHK", "Ten", hk?.MaHK);
            ViewBag.MaHK = hk?.MaHK ?? 0;
            ViewBag.Tuan = tuan > 0 ? Math.Clamp(tuan, 1, 20) : ScheduleService.TuanHienTai(hk);
            string ma = User.FindFirst("Ma")?.Value ?? "";
            ViewBag.Endpoint = User.IsInRole(VaiTro.SinhVien) ? $"/api/students/{ma}/timetable" : $"/api/lecturers/{ma}/timetable";
            return View();
        }
    }
}
