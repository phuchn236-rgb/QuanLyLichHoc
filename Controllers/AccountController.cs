using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyLichHoc.Data;
using QuanLyLichHoc.Models;
using QuanLyLichHoc.Services;

namespace QuanLyLichHoc.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext db;
        private readonly AuditService audit;
        private const int MaxFail = 5;
        public AccountController(AppDbContext db, AuditService audit) { this.db = db; this.audit = audit; }

        [HttpGet, AllowAnonymous]
        public IActionResult Login(string returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true) return RedirectToAction("Index", "Home");
            return View(new LoginVM { ReturnUrl = returnUrl });
        }

        [HttpPost, AllowAnonymous]
        public async Task<IActionResult> Login(LoginVM vm)
        {
            if (!ModelState.IsValid) return View(vm);
            string ten = vm.TenDangNhap.Trim().ToLower();
            var user = await db.NguoiDungs.FirstOrDefaultAsync(u => u.TenDangNhap == ten);

            if (user != null && user.KhoaDen.HasValue && user.KhoaDen > DateTime.Now)
            {
                ModelState.AddModelError("", $"Tài khoản bị khóa tạm thời đến {user.KhoaDen:HH:mm dd/MM/yyyy} do đăng nhập sai nhiều lần.");
                return View(vm);
            }
            if (user == null || !user.TrangThai || !PasswordHasher.Verify(vm.MatKhau, user.MatKhauHash))
            {
                if (user != null && user.TrangThai)
                {
                    user.SoLanSai++;
                    if (user.SoLanSai >= MaxFail) { user.KhoaDen = DateTime.Now.AddMinutes(10); user.SoLanSai = 0; }
                    await db.SaveChangesAsync();
                }
                ModelState.AddModelError("", "Sai tên đăng nhập hoặc mật khẩu.");
                return View(vm);
            }

            user.SoLanSai = 0; user.KhoaDen = null;
            await db.SaveChangesAsync();

            string ma = user.VaiTro == VaiTro.SinhVien ? (await db.SinhViens.Where(s => s.MaND == user.MaND).Select(s => s.MaSV).FirstOrDefaultAsync())
                      : user.VaiTro == VaiTro.GiangVien ? (await db.GiangViens.Where(g => g.MaND == user.MaND).Select(g => g.MaGV).FirstOrDefaultAsync())
                      : null;

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.MaND.ToString()),
                new Claim(ClaimTypes.Name, user.TenDangNhap),
                new Claim(ClaimTypes.Role, user.VaiTro),
                new Claim("HoTen", user.HoTen ?? user.TenDangNhap),
                new Claim("Ma", ma ?? "")
            };
            var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
            var props = new AuthenticationProperties { IsPersistent = vm.GhiNho };
            if (vm.GhiNho) props.ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, props);

            // Session lưu thông tin hiển thị cho phiên làm việc
            HttpContext.Session.SetInt32("UserId", user.MaND);
            HttpContext.Session.SetString("UserRole", user.VaiTro);
            HttpContext.Session.SetString("FullName", user.HoTen ?? user.TenDangNhap);

            await audit.LogAsync(user.TenDangNhap, "Đăng nhập", "Thành công");
            if (!string.IsNullOrEmpty(vm.ReturnUrl) && Url.IsLocalUrl(vm.ReturnUrl)) return Redirect(vm.ReturnUrl);
            return RedirectToAction("Index", "Home");
        }

        [HttpPost, Authorize]
        public async Task<IActionResult> Logout()
        {
            await audit.LogAsync(User.Identity.Name, "Đăng xuất", "");
            HttpContext.Session.Clear();
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }

        [HttpGet, Authorize]
        public async Task<IActionResult> Profile()
        {
            int id = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var u = await db.NguoiDungs.AsNoTracking().FirstAsync(x => x.MaND == id);
            ViewBag.SV = await db.SinhViens.AsNoTracking().Include(s => s.Lop).ThenInclude(l => l.Khoa).FirstOrDefaultAsync(s => s.MaND == id);
            ViewBag.GV = await db.GiangViens.AsNoTracking().Include(g => g.Khoa).FirstOrDefaultAsync(g => g.MaND == id);
            return View(u);
        }

        [HttpPost, Authorize]
        public async Task<IActionResult> ChangePassword(ChangePasswordVM vm)
        {
            if (!ModelState.IsValid)
                return Json(new { ok = false, errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList() });
            int id = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var u = await db.NguoiDungs.FindAsync(id);
            if (u == null || !PasswordHasher.Verify(vm.MatKhauCu, u.MatKhauHash))
                return Json(new { ok = false, errors = new[] { "Mật khẩu hiện tại không đúng." } });
            u.MatKhauHash = PasswordHasher.Hash(vm.MatKhauMoi);
            await db.SaveChangesAsync();
            await audit.LogAsync(u.TenDangNhap, "Đổi mật khẩu", "");
            return Json(new { ok = true });
        }

        [HttpGet, AllowAnonymous]
        public IActionResult AccessDenied() => View();
    }
}
