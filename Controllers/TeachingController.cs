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
    /// <summary>Chức năng riêng của giảng viên: lịch bận, danh sách lớp, danh sách sinh viên.</summary>
    [Authorize(Roles = "GiangVien")]
    public class TeachingController : Controller
    {
        private readonly AppDbContext db;
        public TeachingController(AppDbContext db) { this.db = db; }
        private string maGvCache;
        private async Task<string> GetMaGV()
        {
            if (maGvCache != null) return maGvCache;
            var claim = User.FindFirst("Ma")?.Value;
            if (!string.IsNullOrEmpty(claim)) return maGvCache = claim;
            int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id);
            string ten = (User.Identity?.Name ?? "").ToLower();
            return maGvCache = await db.GiangViens.Where(g => g.MaND == id || g.MaGV.ToLower() == ten).Select(g => g.MaGV).FirstOrDefaultAsync() ?? "";
        }

        public async Task<IActionResult> Busy()
        {
            var maGv = await GetMaGV();
            ViewBag.Thus = new SelectList(Enumerable.Range(2, 7).Select(t => new { Id = t, Ten = TkbHelper.ThuText(t) }), "Id", "Ten");
            return View(await db.LichBans.AsNoTracking().Where(b => b.MaGV == maGv).OrderBy(b => b.Thu).ThenBy(b => b.TietBatDau).ToListAsync());
        }

        [HttpPost]
        public async Task<IActionResult> AddBusy(int thu, int tietBatDau, int soTiet, string ghiChu)
        {
            if (thu < 2 || thu > 8 || tietBatDau < 1 || soTiet < 1 || tietBatDau + soTiet - 1 > 12)
                return Json(new { ok = false, errors = new[] { "Thứ/tiết không hợp lệ (tiết 1–12)." } });
            db.LichBans.Add(new LichBan { MaGV = await GetMaGV(), Thu = thu, TietBatDau = tietBatDau, SoTiet = soTiet, GhiChu = ghiChu?.Trim() });
            await db.SaveChangesAsync();
            return Json(new { ok = true });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteBusy(int id)
        {
            var maGv = await GetMaGV();
            var b = await db.LichBans.FirstOrDefaultAsync(x => x.MaLichBan == id && x.MaGV == maGv);
            if (b == null) return Json(new { ok = false, errors = new[] { "Không tìm thấy." } });
            db.LichBans.Remove(b);
            await db.SaveChangesAsync();
            return Json(new { ok = true });
        }

        public async Task<IActionResult> Classes(int maHK = 0)
        {
            var maGv = await GetMaGV();
            var hocKys = await db.HocKys.AsNoTracking().OrderByDescending(h => h.NgayBatDau).ToListAsync();
            if (maHK == 0)
            {
                var today = DateTime.Today;
                maHK = (hocKys.FirstOrDefault(h => h.NgayBatDau <= today && today <= h.NgayKetThuc) ?? hocKys.FirstOrDefault())?.MaHK ?? 0;
            }
            ViewBag.HocKys = new SelectList(hocKys, "MaHK", "Ten", maHK);
            var lhps = await db.LopHocPhans.AsNoTracking().Where(l => l.MaGV == maGv && l.MaHK == maHK).OrderBy(l => l.MaLHP).ToListAsync();

            // Dựng dòng hiển thị: môn, lịch, sĩ số (đếm đăng ký trực tiếp từ bảng DangKys)
            var ids = lhps.Select(l => l.MaLHP).ToList();
            var maMHs = lhps.Select(l => l.MaMH).Distinct().ToList();
            var mons = await db.MonHocs.AsNoTracking().Where(m => maMHs.Contains(m.MaMH)).Select(m => new { m.MaMH, m.TenMH, m.SoTinChi }).ToListAsync();
            var siSo = await db.DangKys.AsNoTracking().Where(d => ids.Contains(d.MaLHP)).GroupBy(d => d.MaLHP).Select(g => new { MaLHP = g.Key, N = g.Count() }).ToListAsync();
            var lichs = await db.LichHocs.AsNoTracking().Where(x => ids.Contains(x.MaLHP)).Select(x => new { x.MaLHP, x.Thu, x.TietBatDau, x.SoTiet, x.MaPhong }).ToListAsync();
            var maPhongs = lichs.Select(x => x.MaPhong).Distinct().ToList();
            var phongs = await db.PhongHocs.AsNoTracking().Where(p => maPhongs.Contains(p.MaPhong)).Select(p => new { p.MaPhong, p.TenPhong }).ToListAsync();
            var rows = lhps.Select(l =>
            {
                var mon = mons.FirstOrDefault(m => m.MaMH == l.MaMH);
                return new RegRow
                {
                    MaLHP = l.MaLHP, TenMon = mon?.TenMH ?? l.MaMH, SoTinChi = mon?.SoTinChi ?? 0, GiangVien = maGv,
                    SiSo = siSo.FirstOrDefault(x => x.MaLHP == l.MaLHP)?.N ?? 0, SiSoToiDa = l.SiSoToiDa, TrangThai = l.TrangThai,
                    Lich = string.Join("; ", lichs.Where(x => x.MaLHP == l.MaLHP).OrderBy(x => x.Thu)
                        .Select(x => $"{TkbHelper.ThuText(x.Thu)} tiết {x.TietBatDau}-{x.TietBatDau + x.SoTiet - 1} ({phongs.FirstOrDefault(p => p.MaPhong == x.MaPhong)?.TenPhong})"))
                };
            }).ToList();
            return View(rows);
        }

        public async Task<IActionResult> Students(string id)
        {
            var maGv = await GetMaGV();
            var lhp = await db.LopHocPhans.AsNoTracking().FirstOrDefaultAsync(l => l.MaLHP == id && l.MaGV == maGv);
            if (lhp == null) return NotFound();
            ViewBag.MaLHP = lhp.MaLHP;
            ViewBag.TenMon = await db.MonHocs.AsNoTracking().Where(m => m.MaMH == lhp.MaMH).Select(m => m.TenMH).FirstOrDefaultAsync() ?? lhp.MaMH;

            var maSVs = await db.DangKys.AsNoTracking().Where(d => d.MaLHP == id).Select(d => d.MaSV).ToListAsync();
            var svs = await db.SinhViens.AsNoTracking().Where(s => maSVs.Contains(s.MaSV)).Select(s => new { s.MaSV, s.HoTen, s.Email, s.MaLop }).ToListAsync();
            var lopIds = svs.Select(s => s.MaLop).Distinct().ToList();
            var lops = await db.Lops.AsNoTracking().Where(l => lopIds.Contains(l.MaLop)).Select(l => new { l.MaLop, l.TenLop }).ToListAsync();
            var rows = maSVs.Select(ma =>
            {
                var s = svs.FirstOrDefault(x => x.MaSV == ma);
                return new StudentRow { MaSV = ma, HoTen = s?.HoTen ?? ma, Email = s?.Email, Lop = s == null ? null : lops.FirstOrDefault(l => l.MaLop == s.MaLop)?.TenLop };
            }).OrderBy(x => x.HoTen).ToList();
            return View(rows);
        }
    }
}
