using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuanLyLichHoc.Data;
using QuanLyLichHoc.Infrastructure;
using QuanLyLichHoc.Models;
using QuanLyLichHoc.Services;

namespace QuanLyLichHoc.Controllers
{
    /// <summary>Chức năng riêng của giảng viên: lịch bận, danh sách lớp, danh sách sinh viên.</summary>
    [Authorize(Roles = "GiangVien")]
    public class TeachingController : Controller
    {
        private readonly AppDbContext db;
        private readonly ScheduleService schedule;
        public TeachingController(AppDbContext db, ScheduleService schedule) { this.db = db; this.schedule = schedule; }
        private string MaGV => User.FindFirst("Ma")?.Value ?? "";

        public async Task<IActionResult> Busy()
        {
            ViewBag.Thus = new SelectList(Enumerable.Range(2, 7).Select(t => new { Id = t, Ten = TkbHelper.ThuText(t) }), "Id", "Ten");
            return View(await db.LichBans.AsNoTracking().Where(b => b.MaGV == MaGV).OrderBy(b => b.Thu).ThenBy(b => b.TietBatDau).ToListAsync());
        }

        [HttpPost]
        public async Task<IActionResult> AddBusy(int thu, int tietBatDau, int soTiet, string ghiChu)
        {
            if (thu < 2 || thu > 8 || tietBatDau < 1 || soTiet < 1 || tietBatDau + soTiet - 1 > 12)
                return Json(new { ok = false, errors = new[] { "Thứ/tiết không hợp lệ (tiết 1–12)." } });
            db.LichBans.Add(new LichBan { MaGV = MaGV, Thu = thu, TietBatDau = tietBatDau, SoTiet = soTiet, GhiChu = ghiChu?.Trim() });
            await db.SaveChangesAsync();
            return Json(new { ok = true });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteBusy(int id)
        {
            var b = await db.LichBans.FirstOrDefaultAsync(x => x.MaLichBan == id && x.MaGV == MaGV);
            if (b == null) return Json(new { ok = false, errors = new[] { "Không tìm thấy." } });
            db.LichBans.Remove(b);
            await db.SaveChangesAsync();
            return Json(new { ok = true });
        }

        public async Task<IActionResult> Classes(int maHK = 0)
        {
            var hocKys = await db.HocKys.AsNoTracking().OrderByDescending(h => h.NgayBatDau).ToListAsync();
            if (maHK == 0) maHK = (await schedule.HocKyHienTaiAsync())?.MaHK ?? 0;
            ViewBag.HocKys = new SelectList(hocKys, "MaHK", "Ten", maHK);
            var list = await db.LopHocPhans.AsNoTracking().Include(l => l.MonHoc).Include(l => l.DangKys).Include(l => l.LichHocs).ThenInclude(x => x.PhongHoc)
                .Where(l => l.MaGV == MaGV && l.MaHK == maHK).OrderBy(l => l.MaLHP).ToListAsync();
            return View(list);
        }

        public async Task<IActionResult> Students(string id)
        {
            var lhp = await db.LopHocPhans.AsNoTracking().Include(l => l.MonHoc).FirstOrDefaultAsync(l => l.MaLHP == id && l.MaGV == MaGV);
            if (lhp == null) return NotFound();
            ViewBag.LHP = lhp;
            var svs = await db.SinhViens.AsNoTracking().Include(s => s.Lop).Where(s => db.DangKys.Any(d => d.MaSV == s.MaSV && d.MaLHP == id)).OrderBy(s => s.MaSV).ToListAsync();
            return View(svs);
        }
    }
}
