using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyLichHoc.Data;
using QuanLyLichHoc.Infrastructure;
using QuanLyLichHoc.Models;
using QuanLyLichHoc.Services;

namespace QuanLyLichHoc.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext db;
        private readonly ScheduleService schedule;
        public HomeController(AppDbContext db, ScheduleService schedule) { this.db = db; this.schedule = schedule; }

        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            ViewBag.HocKy = await schedule.HocKyHienTaiAsync();
            if (User.IsInRole(VaiTro.Admin) || User.IsInRole(VaiTro.GiaoVu))
            {
                ViewBag.SoSV = await db.SinhViens.CountAsync();
                ViewBag.SoGV = await db.GiangViens.CountAsync();
                ViewBag.SoPhong = await db.PhongHocs.CountAsync();
                ViewBag.SoLHP = await db.LopHocPhans.CountAsync();
                ViewBag.SoLich = await db.LichHocs.CountAsync();
            }
            return View();
        }

        /// <summary>Tra cứu lịch học công khai (không cần đăng nhập): lọc + phân trang bằng AJAX.</summary>
        [AllowAnonymous]
        public async Task<IActionResult> TraCuu(string q, int maHK = 0, int maPhong = 0, int thu = 0, int page = 1)
        {
            if (maHK == 0 && !IsAjax) maHK = (await schedule.HocKyHienTaiAsync())?.MaHK ?? 0;
            var x = schedule.Query();
            if (maHK > 0) x = x.Where(l => l.LopHocPhan.MaHK == maHK);
            if (maPhong > 0) x = x.Where(l => l.MaPhong == maPhong);
            if (thu > 0) x = x.Where(l => l.Thu == thu);
            if (!string.IsNullOrWhiteSpace(q))
                x = x.Where(l => l.MaLHP.Contains(q) || l.LopHocPhan.MonHoc.TenMH.Contains(q) || l.LopHocPhan.GiangVien.HoTen.Contains(q));
            var list = await PaginatedList<LichHoc>.CreateAsync(x.OrderBy(l => l.Thu).ThenBy(l => l.TietBatDau), page, 10);
            if (IsAjax) return PartialView("_TraCuuTable", list);

            ViewBag.HocKys = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(await db.HocKys.OrderByDescending(h => h.NgayBatDau).ToListAsync(), "MaHK", "Ten", maHK);
            ViewBag.Phongs = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(await db.PhongHocs.OrderBy(p => p.TenPhong).ToListAsync(), "MaPhong", "TenPhong");
            ViewBag.Q = q;
            return View(list);
        }

        private bool IsAjax => Request.Headers["X-Requested-With"] == "XMLHttpRequest";

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
