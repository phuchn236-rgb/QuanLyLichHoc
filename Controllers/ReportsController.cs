using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuanLyLichHoc.Data;
using QuanLyLichHoc.Models;
using QuanLyLichHoc.Services;

namespace QuanLyLichHoc.Controllers
{
    [Authorize(Roles = "GiaoVu,Admin")]
    public class ReportsController : Controller
    {
        private readonly AppDbContext db;
        private readonly ScheduleService schedule;
        public ReportsController(AppDbContext db, ScheduleService schedule) { this.db = db; this.schedule = schedule; }

        private async Task<(List<ReportRow> rooms, List<ReportRow> lecturers)> BuildAsync(int maHK)
        {
            var lichs = await db.LichHocs.AsNoTracking().Include(l => l.PhongHoc).Include(l => l.LopHocPhan).ThenInclude(x => x.GiangVien)
                .Where(l => l.LopHocPhan.MaHK == maHK).ToListAsync();
            const double tietTuan = 12 * 6; // 12 tiết x 6 ngày (T2–T7)
            var rooms = await db.PhongHocs.AsNoTracking().OrderBy(p => p.TenPhong).ToListAsync();
            var rr = rooms.Select(p =>
            {
                var l = lichs.Where(x => x.MaPhong == p.MaPhong).ToList();
                int tt = l.Sum(x => x.SoTiet);
                return new ReportRow { Ten = p.TenPhong, SoBuoi = l.Count, TietTuan = tt, TongTiet = l.Sum(x => x.SoTiet * (x.TuanKetThuc - x.TuanBatDau + 1)), TyLe = Math.Round(tt / tietTuan * 100, 1) };
            }).ToList();
            var lr = lichs.GroupBy(x => x.LopHocPhan.GiangVien.HoTen).Select(g => new ReportRow
            {
                Ten = g.Key, SoBuoi = g.Count(), TietTuan = g.Sum(x => x.SoTiet), TongTiet = g.Sum(x => x.SoTiet * (x.TuanKetThuc - x.TuanBatDau + 1))
            }).OrderByDescending(x => x.TongTiet).ToList();
            return (rr, lr);
        }

        public async Task<IActionResult> Index(int maHK = 0)
        {
            if (maHK == 0) maHK = (await schedule.HocKyHienTaiAsync())?.MaHK ?? 0;
            ViewBag.HocKys = new SelectList(await db.HocKys.OrderByDescending(h => h.NgayBatDau).ToListAsync(), "MaHK", "Ten", maHK);
            ViewBag.MaHK = maHK;
            var (rooms, lecturers) = await BuildAsync(maHK);
            ViewBag.Rooms = rooms; ViewBag.Lecturers = lecturers;
            return View();
        }

        /// <summary>Xuất CSV (UTF-8 có BOM) – mở trực tiếp bằng Excel.</summary>
        public async Task<IActionResult> ExportCsv(int maHK, string loai = "phong")
        {
            var (rooms, lecturers) = await BuildAsync(maHK);
            var data = loai == "giangvien" ? lecturers : rooms;
            var sb = new StringBuilder();
            sb.AppendLine(loai == "giangvien" ? "Giảng viên,Số buổi/tuần,Số tiết/tuần,Tổng số tiết học kỳ" : "Phòng,Số buổi/tuần,Số tiết/tuần,Tổng số tiết học kỳ,Công suất (%)");
            foreach (var r in data)
                sb.AppendLine(loai == "giangvien"
                    ? $"\"{r.Ten}\",{r.SoBuoi},{r.TietTuan},{r.TongTiet}"
                    : $"\"{r.Ten}\",{r.SoBuoi},{r.TietTuan},{r.TongTiet},{r.TyLe.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
            var bytes = new UTF8Encoding(true).GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
            return File(bytes, "text/csv", $"baocao_{loai}_hk{maHK}.csv");
        }
    }
}
