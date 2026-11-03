using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuanLyLichHoc.Data;
using QuanLyLichHoc.Infrastructure;
using QuanLyLichHoc.Models;

namespace QuanLyLichHoc.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext db;
        public HomeController(AppDbContext db) { this.db = db; }

        private bool IsAjax => Request.Headers["X-Requested-With"] == "XMLHttpRequest";
        private bool IsStaff => User.IsInRole(VaiTro.Admin) || User.IsInRole(VaiTro.GiaoVu);

        // ---------- Truy vấn dùng chung trong controller (làm việc trực tiếp với DbContext) ----------
        private async Task<HocKy> HocKyHienTaiAsync()
        {
            var today = DateTime.Today;
            return await db.HocKys.AsNoTracking().Where(h => h.NgayBatDau <= today && today <= h.NgayKetThuc).FirstOrDefaultAsync()
                   ?? await db.HocKys.AsNoTracking().OrderByDescending(h => h.NgayBatDau).FirstOrDefaultAsync();
        }

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

        // ---------- Trang chủ ----------
        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            var hkNow = await HocKyHienTaiAsync();
            ViewBag.HocKy = hkNow;
            if (hkNow != null && (User.IsInRole(VaiTro.SinhVien) || User.IsInRole(VaiTro.GiangVien)))
            {
                string ma = await MaCuaToiAsync();
                int tuan = TkbHelper.TuanHienTai(hkNow);
                int maHK = hkNow.MaHK;
                bool sv = User.IsInRole(VaiTro.SinhVien);

                // Lịch trong tuần hiện tại của sinh viên (đã đăng ký) hoặc giảng viên (phụ trách)
                var q = db.LichHocs.AsNoTracking().Where(l => l.TuanBatDau <= tuan && tuan <= l.TuanKetThuc);
                q = sv ? q.Where(l => db.LopHocPhans.Any(h => h.MaLHP == l.MaLHP && h.MaHK == maHK) && db.DangKys.Any(d => d.MaSV == ma && d.MaLHP == l.MaLHP))
                       : q.Where(l => db.LopHocPhans.Any(h => h.MaLHP == l.MaLHP && h.MaHK == maHK && h.MaGV == ma));
                var week = await ToDtosAsync(await q.OrderBy(x => x.Thu).ThenBy(x => x.TietBatDau).ToListAsync());
                int thu = DateTime.Today.DayOfWeek == DayOfWeek.Sunday ? 8 : (int)DateTime.Today.DayOfWeek + 1;
                ViewBag.Tuan = tuan;
                ViewBag.Week = week;
                ViewBag.Today = week.Where(x => x.Thu == thu).ToList();

                if (sv)
                {
                    // Số học phần và tổng tín chỉ đã đăng ký trong học kỳ
                    var maMHs = await db.LopHocPhans.Where(l => l.MaHK == maHK && db.DangKys.Any(d => d.MaSV == ma && d.MaLHP == l.MaLHP)).Select(l => l.MaMH).ToListAsync();
                    var tc = await db.MonHocs.Where(m => maMHs.Contains(m.MaMH)).Select(m => new { m.MaMH, m.SoTinChi }).ToListAsync();
                    ViewBag.SoHP = maMHs.Count;
                    ViewBag.SoTC = maMHs.Sum(x => tc.FirstOrDefault(t => t.MaMH == x)?.SoTinChi ?? 0);
                }
                else ViewBag.SoHP = await db.LopHocPhans.CountAsync(l => l.MaGV == ma && l.MaHK == maHK);
            }
            if (IsStaff)
            {
                ViewBag.SoSV = await db.SinhViens.CountAsync();
                ViewBag.SoGV = await db.GiangViens.CountAsync();
                ViewBag.SoPhong = await db.PhongHocs.CountAsync();
                ViewBag.SoLHP = await db.LopHocPhans.CountAsync();
                ViewBag.SoLich = await db.LichHocs.CountAsync();
                ViewBag.SoDangKy = await db.DangKys.CountAsync();
            }
            return View();
        }

        /// <summary>Tra cứu lịch học (công khai). Admin/Giáo vụ thấy thêm danh sách sinh viên đăng ký từng lớp.</summary>
        [AllowAnonymous]
        public async Task<IActionResult> TraCuu(string q, int maHK = 0, int maPhong = 0, int thu = 0, int page = 1)
        {
            if (maHK == 0 && !IsAjax) maHK = (await HocKyHienTaiAsync())?.MaHK ?? 0;
            var x = db.LichHocs.AsNoTracking().AsQueryable();
            if (maHK > 0) x = x.Where(l => db.LopHocPhans.Any(h => h.MaLHP == l.MaLHP && h.MaHK == maHK));
            if (maPhong > 0) x = x.Where(l => l.MaPhong == maPhong);
            if (thu > 0) x = x.Where(l => l.Thu == thu);
            if (!string.IsNullOrWhiteSpace(q))
                x = x.Where(l => l.MaLHP.Contains(q)
                    || db.PhongHocs.Any(p => p.MaPhong == l.MaPhong && p.TenPhong.Contains(q))
                    || db.LopHocPhans.Any(h => h.MaLHP == l.MaLHP && (db.MonHocs.Any(m => m.MaMH == h.MaMH && m.TenMH.Contains(q))
                                                                     || db.GiangViens.Any(g => g.MaGV == h.MaGV && g.HoTen.Contains(q)))));
            var paged = await PaginatedList<LichHoc>.CreateAsync(x.OrderBy(l => l.Thu).ThenBy(l => l.TietBatDau).ThenBy(l => l.MaLHP), page, 10);
            var dtos = await ToDtosAsync(paged);
            var list = new PaginatedList<LichDto>(dtos, paged.TotalCount, paged.PageIndex, 10);

            if (IsStaff)
            {
                var ids = dtos.Select(d => d.MaLHP).Distinct().ToList();
                var regs = await db.DangKys.AsNoTracking().Where(d => ids.Contains(d.MaLHP)).Select(d => new { d.MaLHP, d.MaSV }).ToListAsync();
                var svIds = regs.Select(r => r.MaSV).Distinct().ToList();
                var svs = await db.SinhViens.AsNoTracking().Where(s => svIds.Contains(s.MaSV)).Select(s => new { s.MaSV, s.HoTen }).ToListAsync();
                ViewBag.Enrolled = ids.ToDictionary(id => id, id => regs.Where(r => r.MaLHP == id)
                    .Select(r => new StudentRow { MaSV = r.MaSV, HoTen = svs.FirstOrDefault(s => s.MaSV == r.MaSV)?.HoTen ?? r.MaSV })
                    .OrderBy(s => s.HoTen).ToList());
                ViewBag.MaHK = maHK;
            }
            if (IsAjax) return PartialView("_TraCuuTable", list);

            ViewBag.HocKys = new SelectList(await db.HocKys.OrderByDescending(h => h.NgayBatDau).ToListAsync(), "MaHK", "Ten", maHK);
            ViewBag.Phongs = new SelectList(await db.PhongHocs.OrderBy(p => p.TenPhong).ToListAsync(), "MaPhong", "TenPhong");
            ViewBag.Q = q;
            return View(list);
        }

        /// <summary>Danh sách sinh viên đã đăng ký một lớp học phần (hộp thoại dành cho Admin/Giáo vụ).</summary>
        [Authorize(Roles = "GiaoVu,Admin")]
        public async Task<IActionResult> Enrolled(string maLHP, int maHK = 0)
        {
            var lhp = await db.LopHocPhans.AsNoTracking().FirstOrDefaultAsync(l => l.MaLHP == maLHP);
            if (lhp == null) return NotFound();
            var mon = await db.MonHocs.AsNoTracking().Where(m => m.MaMH == lhp.MaMH).Select(m => m.TenMH).FirstOrDefaultAsync();

            var maSVs = await db.DangKys.AsNoTracking().Where(d => d.MaLHP == maLHP).Select(d => d.MaSV).ToListAsync();
            var svs = await db.SinhViens.AsNoTracking().Where(s => maSVs.Contains(s.MaSV)).Select(s => new { s.MaSV, s.HoTen, s.Email, s.MaLop }).ToListAsync();
            var lopIds = svs.Select(s => s.MaLop).Distinct().ToList();
            var lops = await db.Lops.AsNoTracking().Where(l => lopIds.Contains(l.MaLop)).Select(l => new { l.MaLop, l.TenLop }).ToListAsync();
            var rows = maSVs.Select(ma =>
            {
                var s = svs.FirstOrDefault(x => x.MaSV == ma);
                return new StudentRow { MaSV = ma, HoTen = s?.HoTen ?? ma, Email = s?.Email, Lop = s == null ? null : lops.FirstOrDefault(l => l.MaLop == s.MaLop)?.TenLop };
            }).OrderBy(x => x.HoTen).ToList();

            ViewBag.Title = $"{lhp.MaLHP} – {mon ?? lhp.MaMH}";
            ViewBag.MaHK = lhp.MaHK;
            ViewBag.SiSoToiDa = lhp.SiSoToiDa;
            return PartialView("_Enrolled", rows);
        }

        [HttpPost, AllowAnonymous]
        public IActionResult SetLanguage(string culture, string returnUrl)
        {
            if (culture != "vi-VN" && culture != "en-US") culture = "vi-VN";
            Response.Cookies.Append(CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
                new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1), IsEssential = true, SameSite = SameSiteMode.Lax });
            return LocalRedirect(!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl) ? returnUrl : "/");
        }

        [AllowAnonymous]
        public IActionResult Error() => View();
    }
}
