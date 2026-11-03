using System.Security.Claims;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyLichHoc.Data;
using QuanLyLichHoc.Infrastructure;
using QuanLyLichHoc.Models;

namespace QuanLyLichHoc.Controllers.Api
{
    /// <summary>
    /// Web API RESTful của hệ thống: thời khóa biểu, phòng trống, CRUD lịch học.
    /// Controller tự khai báo DbContext và truy vấn trực tiếp.
    /// </summary>
    [ApiController, Authorize]
    public class LichHocApiController : ControllerBase
    {
        private readonly AppDbContext db;
        private readonly IAntiforgery antiforgery;
        public LichHocApiController(AppDbContext db, IAntiforgery antiforgery) { this.db = db; this.antiforgery = antiforgery; }

        // ================= Truy vấn dùng trong controller =================
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

        /// <summary>Kiểm tra mọi ràng buộc của một buổi học. Trả về danh sách lỗi (rỗng = hợp lệ).</summary>
        private async Task<List<string>> KiemTraLichAsync(LichHoc m)
        {
            var err = new List<string>();
            if (m.TuanKetThuc < m.TuanBatDau) err.Add("Tuần kết thúc phải lớn hơn hoặc bằng tuần bắt đầu.");
            int ketThuc = m.TietBatDau + m.SoTiet - 1;
            if (ketThuc > 12) err.Add("Lịch học vượt quá tiết 12 (tiết bắt đầu + số tiết − 1 phải ≤ 12).");
            if (m.TietBatDau <= TkbHelper.TietCuoiBuoiSang && ketThuc > TkbHelper.TietCuoiBuoiSang)
                err.Add("Một buổi học không được kéo dài từ buổi sáng (tiết 1–6) sang buổi chiều (tiết 7–12).");
            if (err.Count > 0) return err;

            var lhp = await db.LopHocPhans.AsNoTracking().FirstOrDefaultAsync(x => x.MaLHP == m.MaLHP);
            var phong = await db.PhongHocs.AsNoTracking().FirstOrDefaultAsync(p => p.MaPhong == m.MaPhong);
            if (lhp == null) err.Add("Lớp học phần không tồn tại.");
            if (phong == null) err.Add("Phòng học không tồn tại.");
            if (err.Count > 0) return err;

            var mon = await db.MonHocs.AsNoTracking().FirstOrDefaultAsync(x => x.MaMH == lhp.MaMH);
            if (mon?.ThucHanh == true && phong.LoaiPhong != "TH") err.Add("Môn thực hành phải xếp vào phòng thực hành.");
            if (lhp.SiSoToiDa > phong.SucChua)
                err.Add($"Sức chứa phòng {phong.TenPhong} ({phong.SucChua}) nhỏ hơn sĩ số tối đa của lớp ({lhp.SiSoToiDa}).");

            var cands = await db.LichHocs.AsNoTracking()
                .Where(x => x.MaLich != m.MaLich && x.Thu == m.Thu
                            && db.LopHocPhans.Any(h => h.MaLHP == x.MaLHP && h.MaHK == lhp.MaHK)
                            && x.TietBatDau <= ketThuc && m.TietBatDau <= x.TietBatDau + x.SoTiet - 1
                            && x.TuanBatDau <= m.TuanKetThuc && m.TuanBatDau <= x.TuanKetThuc)
                .ToListAsync();
            var candIds = cands.Select(x => x.MaLHP).Distinct().ToList();
            var gvOf = await db.LopHocPhans.AsNoTracking().Where(h => candIds.Contains(h.MaLHP)).Select(h => new { h.MaLHP, h.MaGV }).ToListAsync();

            foreach (var x in cands.Where(x => x.MaPhong == m.MaPhong)) err.Add($"Trùng phòng {phong.TenPhong} với {TkbHelper.Mo(x)}.");
            foreach (var x in cands.Where(x => gvOf.Any(g => g.MaLHP == x.MaLHP && g.MaGV == lhp.MaGV))) err.Add($"Giảng viên bị trùng lịch dạy với {TkbHelper.Mo(x)}.");

            var others = candIds.Where(id => id != m.MaLHP).ToList();
            if (others.Count > 0)
            {
                int chung = await db.DangKys.Where(d => d.MaLHP == m.MaLHP && db.DangKys.Any(e => e.MaSV == d.MaSV && others.Contains(e.MaLHP))).CountAsync();
                if (chung > 0) err.Add($"Có {chung} sinh viên đã đăng ký lớp khác trùng khung giờ này.");
            }
            bool ban = await db.LichBans.AnyAsync(b => b.MaGV == lhp.MaGV && b.Thu == m.Thu && b.TietBatDau <= ketThuc && m.TietBatDau <= b.TietBatDau + b.SoTiet - 1);
            if (ban) err.Add("Giảng viên đã đăng ký lịch bận trong khung giờ này.");
            return err;
        }

        private async Task<IActionResult> TimetableAsync(int maHK, int tuan, string maSV, string maGV)
        {
            var hk = await db.HocKys.AsNoTracking().FirstOrDefaultAsync(h => h.MaHK == maHK) ?? await HocKyHienTaiAsync();
            if (hk == null) return NotFound(new { message = "Chưa có học kỳ." });
            if (tuan < 1) tuan = TkbHelper.TuanHienTai(hk);
            int hkId = hk.MaHK;

            var q = db.LichHocs.AsNoTracking().Where(l => l.TuanBatDau <= tuan && tuan <= l.TuanKetThuc);
            q = maSV != null
                ? q.Where(l => db.LopHocPhans.Any(h => h.MaLHP == l.MaLHP && h.MaHK == hkId) && db.DangKys.Any(d => d.MaSV == maSV && d.MaLHP == l.MaLHP))
                : q.Where(l => db.LopHocPhans.Any(h => h.MaLHP == l.MaLHP && h.MaHK == hkId && h.MaGV == maGV));
            var items = await ToDtosAsync(await q.OrderBy(x => x.Thu).ThenBy(x => x.TietBatDau).ToListAsync());

            var tu = hk.NgayBatDau.Date.AddDays((tuan - 1) * 7);
            return Ok(new { maHK = hk.MaHK, tuan, tuanTu = tu.ToString("yyyy-MM-dd"), tuanDen = tu.AddDays(6).ToString("yyyy-MM-dd"), items });
        }

        // ================= Token CSRF cho các API ghi =================
        [HttpGet("api/csrf")]
        public IActionResult Csrf()
        {
            var t = antiforgery.GetAndStoreTokens(HttpContext);
            return Ok(new { headerName = "RequestVerificationToken", token = t.RequestToken });
        }

        // ================= Thời khóa biểu =================
        [HttpGet("api/students/{maSV}/timetable")]
        public async Task<IActionResult> StudentTimetable(string maSV, int maHK = 0, int tuan = 0)
        {
            bool full = User.IsInRole(VaiTro.GiaoVu) || User.IsInRole(VaiTro.Admin);
            bool own = User.IsInRole(VaiTro.SinhVien) && await MaCuaToiAsync() == maSV;
            if (!full && !own) return Forbid();
            return await TimetableAsync(maHK, tuan, maSV, null);
        }

        [HttpGet("api/lecturers/{maGV}/timetable")]
        public async Task<IActionResult> LecturerTimetable(string maGV, int maHK = 0, int tuan = 0)
        {
            bool full = User.IsInRole(VaiTro.GiaoVu) || User.IsInRole(VaiTro.Admin);
            bool own = User.IsInRole(VaiTro.GiangVien) && await MaCuaToiAsync() == maGV;
            if (!full && !own) return Forbid();
            return await TimetableAsync(maHK, tuan, null, maGV);
        }

        // ================= Phòng trống =================
        [HttpGet("api/rooms/available"), Authorize(Roles = "GiaoVu,Admin")]
        public async Task<IActionResult> AvailableRooms(int maHK, int thu, int tiet, int soTiet = 1, int tuan = 1)
        {
            int ketThuc = tiet + soTiet - 1;
            var busy = db.LichHocs.Where(l => db.LopHocPhans.Any(h => h.MaLHP == l.MaLHP && h.MaHK == maHK) && l.Thu == thu
                        && l.TietBatDau <= ketThuc && tiet <= l.TietBatDau + l.SoTiet - 1
                        && l.TuanBatDau <= tuan && tuan <= l.TuanKetThuc).Select(l => l.MaPhong);
            var rooms = await db.PhongHocs.AsNoTracking().Where(p => !busy.Contains(p.MaPhong)).OrderBy(p => p.TenPhong)
                .Select(p => new { p.MaPhong, p.TenPhong, p.LoaiPhong, p.SucChua, p.ToaNha }).ToListAsync();
            return Ok(rooms);
        }

        // ================= Lịch học (RESTful) =================
        [HttpGet("api/schedules")]
        public async Task<IActionResult> ListSchedules(int? hocKy = null, string gv = null, int? phong = null, int page = 1, int size = 20)
        {
            size = Math.Clamp(size, 1, 100); page = Math.Max(1, page);
            var q = db.LichHocs.AsNoTracking().AsQueryable();
            if (hocKy.HasValue) q = q.Where(l => db.LopHocPhans.Any(h => h.MaLHP == l.MaLHP && h.MaHK == hocKy));
            if (!string.IsNullOrEmpty(gv)) q = q.Where(l => db.LopHocPhans.Any(h => h.MaLHP == l.MaLHP && h.MaGV == gv));
            if (phong.HasValue) q = q.Where(x => x.MaPhong == phong);
            int total = await q.CountAsync();
            var items = await q.OrderBy(x => x.Thu).ThenBy(x => x.TietBatDau).Skip((page - 1) * size).Take(size).ToListAsync();
            return Ok(new { page, size, total, items = await ToDtosAsync(items) });
        }

        [HttpGet("api/schedules/{id:int}")]
        public async Task<IActionResult> GetSchedule(int id)
        {
            var x = await db.LichHocs.AsNoTracking().FirstOrDefaultAsync(l => l.MaLich == id);
            return x == null ? NotFound() : Ok((await ToDtosAsync(new[] { x })).First());
        }

        [HttpPost("api/schedules"), Authorize(Roles = "GiaoVu,Admin")]
        public async Task<IActionResult> CreateSchedule(LichHocInput input)
        {
            var m = new LichHoc { MaLHP = input.MaLHP, MaPhong = input.MaPhong, Thu = input.Thu, TietBatDau = input.TietBatDau, SoTiet = input.SoTiet, TuanBatDau = input.TuanBatDau, TuanKetThuc = input.TuanKetThuc };
            var errors = await KiemTraLichAsync(m);
            if (errors.Count > 0) return Conflict(new { message = "Xung đột hoặc dữ liệu không hợp lệ", errors });
            db.LichHocs.Add(m);
            await db.SaveChangesAsync();
            await GhiNhatKyAsync("Thêm (API)", TkbHelper.Mo(m));
            return Created($"/api/schedules/{m.MaLich}", (await ToDtosAsync(new[] { m })).First());
        }

        [HttpPut("api/schedules/{id:int}"), Authorize(Roles = "GiaoVu,Admin")]
        public async Task<IActionResult> UpdateSchedule(int id, LichHocInput input)
        {
            var m = await db.LichHocs.FindAsync(id);
            if (m == null) return NotFound();
            m.MaLHP = input.MaLHP; m.MaPhong = input.MaPhong; m.Thu = input.Thu; m.TietBatDau = input.TietBatDau;
            m.SoTiet = input.SoTiet; m.TuanBatDau = input.TuanBatDau; m.TuanKetThuc = input.TuanKetThuc;
            var errors = await KiemTraLichAsync(m);
            if (errors.Count > 0) { db.Entry(m).State = EntityState.Unchanged; return Conflict(new { message = "Xung đột hoặc dữ liệu không hợp lệ", errors }); }
            await db.SaveChangesAsync();
            await GhiNhatKyAsync("Sửa (API)", TkbHelper.Mo(m));
            return NoContent();
        }

        [HttpDelete("api/schedules/{id:int}"), Authorize(Roles = "GiaoVu,Admin")]
        public async Task<IActionResult> DeleteSchedule(int id)
        {
            var m = await db.LichHocs.FindAsync(id);
            if (m == null) return NotFound();
            db.LichHocs.Remove(m);
            await db.SaveChangesAsync();
            await GhiNhatKyAsync("Xóa (API)", "Lịch #" + id);
            return NoContent();
        }
    }
}
