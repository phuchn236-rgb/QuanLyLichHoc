using System.Security.Claims;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyLichHoc.Data;
using QuanLyLichHoc.Infrastructure;
using QuanLyLichHoc.Models;
using QuanLyLichHoc.Services;

namespace QuanLyLichHoc.Controllers.Api
{
    /// <summary>Lấy token CSRF để gọi các API ghi (POST/PUT/DELETE) từ Postman hoặc ứng dụng ngoài sau khi đăng nhập.</summary>
    [ApiController, Authorize]
    public class CsrfApiController : ControllerBase
    {
        private readonly IAntiforgery af;
        public CsrfApiController(IAntiforgery af) { this.af = af; }

        [HttpGet("api/csrf")]
        public IActionResult Get()
        {
            var t = af.GetAndStoreTokens(HttpContext);
            return Ok(new { headerName = "RequestVerificationToken", token = t.RequestToken });
        }
    }

    // ===================== Thời khóa biểu =====================
    [ApiController, Authorize]
    public class TimetableApiController : ControllerBase
    {
        private readonly AppDbContext db;
        private readonly ScheduleService schedule;
        public TimetableApiController(AppDbContext db, ScheduleService schedule) { this.db = db; this.schedule = schedule; }

        private async Task<IActionResult> Build(int maHK, int tuan, Func<int, int?, Task<List<LichHoc>>> loader)
        {
            var hk = await db.HocKys.AsNoTracking().FirstOrDefaultAsync(h => h.MaHK == maHK) ?? await schedule.HocKyHienTaiAsync();
            if (hk == null) return NotFound(new { message = "Chưa có học kỳ." });
            if (tuan < 1) tuan = ScheduleService.TuanHienTai(hk);
            var items = await loader(hk.MaHK, tuan);
            var tu = hk.NgayBatDau.Date.AddDays((tuan - 1) * 7);
            return Ok(new { maHK = hk.MaHK, tuan, tuanTu = tu.ToString("yyyy-MM-dd"), tuanDen = tu.AddDays(6).ToString("yyyy-MM-dd"), items = items.Select(TkbHelper.ToDto) });
        }

        [HttpGet("api/students/{maSV}/timetable")]
        public async Task<IActionResult> Student(string maSV, int maHK = 0, int tuan = 0)
        {
            bool full = User.IsInRole(VaiTro.GiaoVu) || User.IsInRole(VaiTro.Admin);
            bool own = User.IsInRole(VaiTro.SinhVien) && User.FindFirstValue("Ma") == maSV;
            if (!full && !own) return Forbid();
            return await Build(maHK, tuan, (hk, t) => schedule.TheoSinhVienAsync(maSV, hk, t));
        }

        [HttpGet("api/lecturers/{maGV}/timetable")]
        public async Task<IActionResult> Lecturer(string maGV, int maHK = 0, int tuan = 0)
        {
            bool full = User.IsInRole(VaiTro.GiaoVu) || User.IsInRole(VaiTro.Admin);
            bool own = User.IsInRole(VaiTro.GiangVien) && User.FindFirstValue("Ma") == maGV;
            if (!full && !own) return Forbid();
            return await Build(maHK, tuan, (hk, t) => schedule.TheoGiangVienAsync(maGV, hk, t));
        }
    }

    // ===================== Phòng trống =====================
    [ApiController, Authorize(Roles = "GiaoVu,Admin")]
    public class RoomsApiController : ControllerBase
    {
        private readonly AppDbContext db;
        public RoomsApiController(AppDbContext db) { this.db = db; }

        [HttpGet("api/rooms/available")]
        public async Task<IActionResult> Available(int maHK, int thu, int tiet, int soTiet = 1, int tuan = 1)
        {
            int ketThuc = tiet + soTiet - 1;
            var busy = db.LichHocs.Where(l => l.LopHocPhan.MaHK == maHK && l.Thu == thu
                        && l.TietBatDau <= ketThuc && tiet <= l.TietBatDau + l.SoTiet - 1
                        && l.TuanBatDau <= tuan && tuan <= l.TuanKetThuc).Select(l => l.MaPhong);
            var rooms = await db.PhongHocs.AsNoTracking().Where(p => !busy.Contains(p.MaPhong)).OrderBy(p => p.TenPhong)
                .Select(p => new { p.MaPhong, p.TenPhong, p.LoaiPhong, p.SucChua, p.ToaNha }).ToListAsync();
            return Ok(rooms);
        }
    }

    // ===================== Lịch học (RESTful) =====================
    [ApiController, Authorize, Route("api/schedules")]
    public class SchedulesApiController : ControllerBase
    {
        private readonly AppDbContext db;
        private readonly ScheduleService schedule;
        private readonly AuditService audit;
        public SchedulesApiController(AppDbContext db, ScheduleService schedule, AuditService audit) { this.db = db; this.schedule = schedule; this.audit = audit; }

        [HttpGet("")]
        public async Task<IActionResult> List(int? hocKy = null, string gv = null, int? phong = null, int page = 1, int size = 20)
        {
            size = Math.Clamp(size, 1, 100); page = Math.Max(1, page);
            var q = schedule.Query();
            if (hocKy.HasValue) q = q.Where(x => x.LopHocPhan.MaHK == hocKy);
            if (!string.IsNullOrEmpty(gv)) q = q.Where(x => x.LopHocPhan.MaGV == gv);
            if (phong.HasValue) q = q.Where(x => x.MaPhong == phong);
            int total = await q.CountAsync();
            var items = await q.OrderBy(x => x.Thu).ThenBy(x => x.TietBatDau).Skip((page - 1) * size).Take(size).ToListAsync();
            return Ok(new { page, size, total, items = items.Select(TkbHelper.ToDto) });
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> Get(int id)
        {
            var x = await schedule.Query().FirstOrDefaultAsync(l => l.MaLich == id);
            return x == null ? NotFound() : Ok(TkbHelper.ToDto(x));
        }

        [HttpPost(""), Authorize(Roles = "GiaoVu,Admin")]
        public async Task<IActionResult> Post(LichHocInput input)
        {
            var m = new LichHoc { MaLHP = input.MaLHP, MaPhong = input.MaPhong, Thu = input.Thu, TietBatDau = input.TietBatDau, SoTiet = input.SoTiet, TuanBatDau = input.TuanBatDau, TuanKetThuc = input.TuanKetThuc };
            var errors = await schedule.KiemTraAsync(m);
            if (errors.Count > 0) return Conflict(new { message = "Xung đột hoặc dữ liệu không hợp lệ", errors });
            db.LichHocs.Add(m);
            await db.SaveChangesAsync();
            await audit.LogAsync(User.Identity.Name, "Thêm (API)", TkbHelper.Mo(m));
            var created = await schedule.Query().FirstAsync(l => l.MaLich == m.MaLich);
            return Created($"/api/schedules/{m.MaLich}", TkbHelper.ToDto(created));
        }

        [HttpPut("{id:int}"), Authorize(Roles = "GiaoVu,Admin")]
        public async Task<IActionResult> Put(int id, LichHocInput input)
        {
            var m = await db.LichHocs.FindAsync(id);
            if (m == null) return NotFound();
            m.MaLHP = input.MaLHP; m.MaPhong = input.MaPhong; m.Thu = input.Thu; m.TietBatDau = input.TietBatDau;
            m.SoTiet = input.SoTiet; m.TuanBatDau = input.TuanBatDau; m.TuanKetThuc = input.TuanKetThuc;
            var errors = await schedule.KiemTraAsync(m);
            if (errors.Count > 0) { db.Entry(m).State = EntityState.Unchanged; return Conflict(new { message = "Xung đột hoặc dữ liệu không hợp lệ", errors }); }
            await db.SaveChangesAsync();
            await audit.LogAsync(User.Identity.Name, "Sửa (API)", TkbHelper.Mo(m));
            return NoContent();
        }

        [HttpDelete("{id:int}"), Authorize(Roles = "GiaoVu,Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var m = await db.LichHocs.FindAsync(id);
            if (m == null) return NotFound();
            db.LichHocs.Remove(m);
            await db.SaveChangesAsync();
            await audit.LogAsync(User.Identity.Name, "Xóa (API)", "Lịch #" + id);
            return NoContent();
        }
    }
}
