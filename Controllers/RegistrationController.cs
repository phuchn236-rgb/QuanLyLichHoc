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
    /// <summary>Đăng ký học phần của sinh viên. Toàn bộ nghiệp vụ xử lý trực tiếp bằng DbContext trong controller.</summary>
    [Authorize(Roles = "SinhVien")]
    public class RegistrationController : Controller
    {
        private readonly AppDbContext db;
        public RegistrationController(AppDbContext db) { this.db = db; }

        private bool IsAjax => Request.Headers["X-Requested-With"] == "XMLHttpRequest";

        // ---------- Truy vấn dùng trong controller ----------
        private async Task<string> MaSVAsync()
        {
            var claim = User.FindFirst("Ma")?.Value;
            if (!string.IsNullOrEmpty(claim)) return claim;
            int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id);
            string ten = (User.Identity?.Name ?? "").ToLower();
            return await db.SinhViens.Where(s => s.MaND == id || s.MaSV.ToLower() == ten).Select(s => s.MaSV).FirstOrDefaultAsync() ?? "";
        }

        private async Task<HocKy> HocKyHienTaiAsync()
        {
            var today = DateTime.Today;
            return await db.HocKys.AsNoTracking().Where(h => h.NgayBatDau <= today && today <= h.NgayKetThuc).FirstOrDefaultAsync()
                   ?? await db.HocKys.AsNoTracking().OrderByDescending(h => h.NgayBatDau).FirstOrDefaultAsync();
        }

        private async Task GhiNhatKyAsync(string hanhDong, string chiTiet)
        {
            db.NhatKys.Add(new NhatKy { NguoiDung = User.Identity?.Name, HanhDong = hanhDong, ChiTiet = chiTiet });
            await db.SaveChangesAsync();
        }

        /// <summary>Số học phần và tổng tín chỉ sinh viên đã đăng ký trong học kỳ.</summary>
        private async Task<(int soHP, int soTC)> TongKetAsync(string maSV, int maHK)
        {
            var maMHs = await db.LopHocPhans.Where(l => l.MaHK == maHK && db.DangKys.Any(d => d.MaSV == maSV && d.MaLHP == l.MaLHP)).Select(l => l.MaMH).ToListAsync();
            var tc = await db.MonHocs.Where(m => maMHs.Contains(m.MaMH)).Select(m => new { m.MaMH, m.SoTinChi }).ToListAsync();
            return (maMHs.Count, maMHs.Sum(x => tc.FirstOrDefault(t => t.MaMH == x)?.SoTinChi ?? 0));
        }

        /// <summary>Dựng dòng hiển thị lớp học phần (môn, giảng viên, sĩ số, lịch, đã đăng ký) bằng tra cứu theo khóa.</summary>
        private async Task<List<RegRow>> BuildRegRowsAsync(List<LopHocPhan> lhps, string maSV)
        {
            var ids = lhps.Select(l => l.MaLHP).ToList();
            var maMHs = lhps.Select(l => l.MaMH).Distinct().ToList();
            var maGVs = lhps.Select(l => l.MaGV).Distinct().ToList();
            var mons = await db.MonHocs.AsNoTracking().Where(m => maMHs.Contains(m.MaMH)).Select(m => new { m.MaMH, m.TenMH, m.SoTinChi }).ToListAsync();
            var gvs = await db.GiangViens.AsNoTracking().Where(g => maGVs.Contains(g.MaGV)).Select(g => new { g.MaGV, g.HoTen }).ToListAsync();
            var siSo = await db.DangKys.AsNoTracking().Where(d => ids.Contains(d.MaLHP)).GroupBy(d => d.MaLHP).Select(g => new { MaLHP = g.Key, N = g.Count() }).ToListAsync();
            var daDk = await db.DangKys.AsNoTracking().Where(d => d.MaSV == maSV && ids.Contains(d.MaLHP)).Select(d => d.MaLHP).ToListAsync();
            var lichs = await db.LichHocs.AsNoTracking().Where(x => ids.Contains(x.MaLHP)).Select(x => new { x.MaLHP, x.Thu, x.TietBatDau, x.SoTiet, x.MaPhong }).ToListAsync();
            var maPhongs = lichs.Select(x => x.MaPhong).Distinct().ToList();
            var phongs = await db.PhongHocs.AsNoTracking().Where(p => maPhongs.Contains(p.MaPhong)).Select(p => new { p.MaPhong, p.TenPhong }).ToListAsync();

            return lhps.Select(l =>
            {
                var mon = mons.FirstOrDefault(m => m.MaMH == l.MaMH);
                var gv = gvs.FirstOrDefault(g => g.MaGV == l.MaGV);
                return new RegRow
                {
                    MaLHP = l.MaLHP, TenMon = mon?.TenMH ?? l.MaMH, SoTinChi = mon?.SoTinChi ?? 0, GiangVien = gv?.HoTen ?? l.MaGV,
                    SiSo = siSo.FirstOrDefault(x => x.MaLHP == l.MaLHP)?.N ?? 0, SiSoToiDa = l.SiSoToiDa, TrangThai = l.TrangThai,
                    DaDangKy = daDk.Contains(l.MaLHP),
                    Lich = string.Join("; ", lichs.Where(x => x.MaLHP == l.MaLHP).OrderBy(x => x.Thu)
                        .Select(x => $"{TkbHelper.ThuText(x.Thu)} tiết {x.TietBatDau}-{x.TietBatDau + x.SoTiet - 1} ({phongs.FirstOrDefault(p => p.MaPhong == x.MaPhong)?.TenPhong})"))
                };
            }).ToList();
        }

        private async Task<List<LichDto>> ToDtosAsync(IEnumerable<LichHoc> lichs)
        {
            var list = lichs.ToList();
            if (list.Count == 0) return new List<LichDto>();
            var lhpIds = list.Select(x => x.MaLHP).Distinct().ToList();
            var phongIds = list.Select(x => x.MaPhong).Distinct().ToList();
            var lhps = await db.LopHocPhans.AsNoTracking().Where(h => lhpIds.Contains(h.MaLHP)).Select(h => new { h.MaLHP, h.MaMH, h.MaGV }).ToListAsync();
            var mhIds = lhps.Select(h => h.MaMH).Distinct().ToList();
            var gvIds = lhps.Select(h => h.MaGV).Distinct().ToList();
            var mons = await db.MonHocs.AsNoTracking().Where(m => mhIds.Contains(m.MaMH)).Select(m => new { m.MaMH, m.TenMH }).ToListAsync();
            var gvs = await db.GiangViens.AsNoTracking().Where(g => gvIds.Contains(g.MaGV)).Select(g => new { g.MaGV, g.HoTen }).ToListAsync();
            var phongs = await db.PhongHocs.AsNoTracking().Where(p => phongIds.Contains(p.MaPhong)).Select(p => new { p.MaPhong, p.TenPhong }).ToListAsync();
            return list.Select(x =>
            {
                var h = lhps.FirstOrDefault(y => y.MaLHP == x.MaLHP);
                var mon = h == null ? null : mons.FirstOrDefault(m => m.MaMH == h.MaMH);
                var gv = h == null ? null : gvs.FirstOrDefault(g => g.MaGV == h.MaGV);
                var ph = phongs.FirstOrDefault(p => p.MaPhong == x.MaPhong);
                return new LichDto(x.MaLich, x.MaLHP, h?.MaMH, mon?.TenMH ?? h?.MaMH, gv?.HoTen ?? h?.MaGV, ph?.TenPhong,
                    x.Thu, TkbHelper.ThuText(x.Thu), x.TietBatDau, x.SoTiet, x.TuanBatDau, x.TuanKetThuc, TkbHelper.ThoiGian(x.TietBatDau, x.SoTiet));
            }).ToList();
        }

        /// <summary>Kiểm tra sinh viên có đăng ký được lớp học phần không. Trả về thông báo lỗi, hoặc null nếu hợp lệ.</summary>
        private async Task<string> KiemTraDangKyAsync(string maSV, string maLHP)
        {
            if (string.IsNullOrEmpty(maSV) || !await db.SinhViens.AnyAsync(s => s.MaSV == maSV)) return "Không xác định được sinh viên của tài khoản này.";
            var lhp = await db.LopHocPhans.AsNoTracking().FirstOrDefaultAsync(x => x.MaLHP == maLHP);
            if (lhp == null) return "Lớp học phần không tồn tại.";
            if (lhp.TrangThai != TrangThaiLHP.Mo) return "Lớp học phần hiện không mở đăng ký.";
            if (await db.DangKys.AnyAsync(d => d.MaSV == maSV && d.MaLHP == maLHP)) return "Bạn đã đăng ký lớp học phần này.";
            if (await db.DangKys.CountAsync(d => d.MaLHP == maLHP) >= lhp.SiSoToiDa) return "Lớp đã đủ sĩ số tối đa.";

            // Đã đăng ký lớp khác của cùng môn trong học kỳ?
            bool trungMon = await db.LopHocPhans.AnyAsync(h => h.MaMH == lhp.MaMH && h.MaHK == lhp.MaHK && h.MaLHP != maLHP
                                                            && db.DangKys.Any(d => d.MaSV == maSV && d.MaLHP == h.MaLHP));
            if (trungMon) return "Bạn đã đăng ký một lớp khác của môn học này trong học kỳ.";

            // Trùng lịch với các lớp đã đăng ký trong cùng học kỳ?
            var moiList = await db.LichHocs.AsNoTracking().Where(x => x.MaLHP == maLHP).ToListAsync();
            var daDangKy = await db.LichHocs.AsNoTracking()
                .Where(l => db.LopHocPhans.Any(h => h.MaLHP == l.MaLHP && h.MaHK == lhp.MaHK)
                            && db.DangKys.Any(d => d.MaSV == maSV && d.MaLHP == l.MaLHP))
                .ToListAsync();
            foreach (var moi in moiList)
                foreach (var cu in daDangKy.Where(c => TkbHelper.GiaoNhau(moi, c)))
                    return "Trùng lịch với " + TkbHelper.Mo(cu) + ".";
            return null;
        }

        // ---------- Các action ----------
        public async Task<IActionResult> Index(string q, int maHK = 0, bool chiDaDangKy = false, int page = 1)
        {
            if (maHK == 0) maHK = (await HocKyHienTaiAsync())?.MaHK ?? 0;
            string ma = await MaSVAsync() ?? "";

            var rows = db.LopHocPhans.AsNoTracking().Where(l => l.MaHK == maHK && l.TrangThai != TrangThaiLHP.Huy);
            if (!string.IsNullOrWhiteSpace(q))
            {
                var mhKeys = db.MonHocs.Where(m => m.TenMH.Contains(q)).Select(m => m.MaMH);
                var gvKeys = db.GiangViens.Where(g => g.HoTen.Contains(q)).Select(g => g.MaGV);
                rows = rows.Where(l => l.MaLHP.Contains(q) || mhKeys.Contains(l.MaMH) || gvKeys.Contains(l.MaGV));
            }
            if (chiDaDangKy)
            {
                var mine = db.DangKys.Where(d => d.MaSV == ma).Select(d => d.MaLHP);
                rows = rows.Where(l => mine.Contains(l.MaLHP));
            }
            var paged = await PaginatedList<LopHocPhan>.CreateAsync(rows.OrderBy(l => l.MaMH).ThenBy(l => l.MaLHP), page, 8);
            var data = await BuildRegRowsAsync(paged.ToList(), ma);
            var list = new PaginatedList<RegRow>(data, paged.TotalCount, paged.PageIndex, 8);

            if (IsAjax) return PartialView("_Table", list);

            var hk = await db.HocKys.AsNoTracking().FirstOrDefaultAsync(h => h.MaHK == maHK);
            ViewBag.HocKys = new SelectList(await db.HocKys.OrderByDescending(h => h.NgayBatDau).ToListAsync(), "MaHK", "Ten", maHK);
            ViewBag.Ma = ma; ViewBag.MaHK = maHK; ViewBag.Tuan = TkbHelper.TuanHienTai(hk);
            var (soHP, soTC) = await TongKetAsync(ma, maHK);
            ViewBag.SoHP = soHP; ViewBag.SoTC = soTC;
            return View(list);
        }

        /// <summary>Xem trước lịch của một lớp học phần trên thời khóa biểu và cho biết có đăng ký được không.</summary>
        [HttpGet]
        public async Task<IActionResult> Preview(string maLHP)
        {
            string ma = await MaSVAsync();
            var items = await ToDtosAsync(await db.LichHocs.AsNoTracking().Where(x => x.MaLHP == maLHP).ToListAsync());
            bool daDk = await db.DangKys.AnyAsync(d => d.MaSV == ma && d.MaLHP == maLHP);
            string conflict = daDk ? null : await KiemTraDangKyAsync(ma, maLHP);
            return Json(new { items, conflict, registered = daDk });
        }

        private async Task<object> SummaryAsync(string ma, string maLHP)
        {
            int maHK = await db.LopHocPhans.Where(l => l.MaLHP == maLHP).Select(l => l.MaHK).FirstOrDefaultAsync();
            var (soHP, soTC) = await TongKetAsync(ma, maHK);
            int siSo = await db.DangKys.CountAsync(d => d.MaLHP == maLHP);
            return new { soHP, soTC, siSo };
        }

        [HttpPost]
        public async Task<IActionResult> Register(string maLHP)
        {
            string ma = await MaSVAsync();
            string ok_msg = null;
            var loi = await KiemTraDangKyAsync(ma, maLHP);
            if (loi == null)
            {
                db.DangKys.Add(new DangKyHocPhan { MaSV = ma, MaLHP = maLHP, NgayDangKy = DateTime.Now });
                try { await db.SaveChangesAsync(); ok_msg = "Đăng ký thành công."; await GhiNhatKyAsync("Đăng ký học phần", $"{ma} → {maLHP}"); }
                catch (DbUpdateException) { loi = "Không thể đăng ký, vui lòng thử lại."; }
            }
            return Json(new { ok = loi == null, message = loi ?? ok_msg, summary = await SummaryAsync(ma, maLHP) });
        }

        [HttpPost]
        public async Task<IActionResult> Unregister(string maLHP)
        {
            string ma = await MaSVAsync();
            var dk = await db.DangKys.FindAsync(ma, maLHP);
            bool ok = dk != null;
            if (ok)
            {
                db.DangKys.Remove(dk);
                await db.SaveChangesAsync();
                await GhiNhatKyAsync("Hủy đăng ký", $"{ma} → {maLHP}");
            }
            return Json(new { ok, message = ok ? "Đã hủy đăng ký." : "Bạn chưa đăng ký lớp học phần này.", summary = await SummaryAsync(ma, maLHP) });
        }
    }
}
