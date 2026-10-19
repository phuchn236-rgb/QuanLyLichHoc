using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyLichHoc.Data;
using QuanLyLichHoc.Models;

namespace QuanLyLichHoc.Controllers
{
    [Authorize(Roles = "Admin")]
    public class LogsController : Controller
    {
        private readonly AppDbContext db;
        public LogsController(AppDbContext db) { this.db = db; }

        public async Task<IActionResult> Index(string q, int page = 1)
        {
            var x = db.NhatKys.AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(q)) x = x.Where(n => n.NguoiDung.Contains(q) || n.HanhDong.Contains(q) || n.ChiTiet.Contains(q));
            var list = await PaginatedList<NhatKy>.CreateAsync(x.OrderByDescending(n => n.ThoiGian), page, 15);
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest") return PartialView("_Table", list);
            return View(list);
        }
    }
}
