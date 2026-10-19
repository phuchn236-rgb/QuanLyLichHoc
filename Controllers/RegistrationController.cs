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
    [Authorize(Roles = "SinhVien")]
    public class RegistrationController : Controller
    {
        private readonly AppDbContext db;
        private readonly RegistrationService reg;
        private readonly ScheduleService schedule;
        private readonly AuditService audit;
        public RegistrationController(AppDbContext db, RegistrationService reg, ScheduleService schedule, AuditService audit)
        { this.db = db; this.reg = reg; this.schedule = schedule; this.audit = audit; }

        private string MaSV => User.FindFirst("Ma")?.Value ?? "";
        private bool IsAjax => Request.Headers["X-Requested-With"] == "XMLHttpRequest";

        public async Task<IActionResult> Index(string q, int maHK = 0, bool chiDaDangKy = false, int page = 1)
        {
            if (maHK == 0) maHK = (await schedule.HocKyHienTaiAsync())?.MaHK ?? 0;
            string ma = MaSV;
            var rows = db.LopHocPhans.AsNoTracking().Where(l => l.MaHK == maHK && l.TrangThai != TrangThaiLHP.Huy);
            if (!string.IsNullOrWhiteSpace(q)) rows = rows.Where(l => l.MaLHP.Contains(q) || l.MonHoc.TenMH.Contains(q) || l.GiangVien.HoTen.Contains(q));
            if (chiDaDangKy) rows = rows.Where(l => l.DangKys.Any(d => d.MaSV == ma));

            var proj = rows.OrderBy(l => l.MaLHP).Select(l => new RegRow
            {
                MaLHP = l.MaLHP, TenMon = l.MonHoc.TenMH, SoTinChi = l.MonHoc.SoTinChi, GiangVien = l.GiangVien.HoTen,
                SiSo = l.DangKys.Count(), SiSoToiDa = l.SiSoToiDa, TrangThai = l.TrangThai,
                DaDangKy = l.DangKys.Any(d => d.MaSV == ma)
            });
            var list = await PaginatedList<RegRow>.CreateAsync(proj, page, 8);
            var ids = list.Select(r => r.MaLHP).ToList();
            var lichs = await db.LichHocs.AsNoTracking().Include(x => x.PhongHoc).Where(x => ids.Contains(x.MaLHP)).ToListAsync();
            foreach (var r in list)
                r.Lich = string.Join("; ", lichs.Where(x => x.MaLHP == r.MaLHP).OrderBy(x => x.Thu)
                    .Select(x => $"{TkbHelper.ThuText(x.Thu)} tiết {x.TietBatDau}-{x.TietBatDau + x.SoTiet - 1} ({x.PhongHoc.TenPhong})"));

            if (IsAjax) return PartialView("_Table", list);
            ViewBag.HocKys = new SelectList(await db.HocKys.OrderByDescending(h => h.NgayBatDau).ToListAsync(), "MaHK", "Ten", maHK);
            return View(list);
        }

        [HttpPost]
        public async Task<IActionResult> Register(string maLHP)
        {
            var (ok, msg) = await reg.DangKyAsync(MaSV, maLHP);
            if (ok) await audit.LogAsync(User.Identity.Name, "Đăng ký học phần", maLHP);
            return Json(new { ok, message = msg });
        }

        [HttpPost]
        public async Task<IActionResult> Unregister(string maLHP)
        {
            var (ok, msg) = await reg.HuyAsync(MaSV, maLHP);
            if (ok) await audit.LogAsync(User.Identity.Name, "Hủy đăng ký", maLHP);
            return Json(new { ok, message = msg });
        }
    }
}
