using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuanLyLichHoc.Data;
using QuanLyLichHoc.Models;

namespace QuanLyLichHoc.Controllers
{
    [Authorize(Roles = "GiaoVu,Admin")]
    public class ReportsController : Controller
    {
        private readonly AppDbContext db;
        public ReportsController(AppDbContext db) { this.db = db; }

        private async Task<HocKy> HocKyHienTaiAsync()
        {
            var today = DateTime.Today;
            return await db.HocKys.AsNoTracking().Where(h => h.NgayBatDau <= today && today <= h.NgayKetThuc).FirstOrDefaultAsync()
                   ?? await db.HocKys.AsNoTracking().OrderByDescending(h => h.NgayBatDau).FirstOrDefaultAsync();
        }

        private async Task<(List<ReportRow> rooms, List<ReportRow> lecturers)> BuildAsync(int maHK)
        {
            var lhps = await db.LopHocPhans.AsNoTracking().Where(h => h.MaHK == maHK).Select(h => new { h.MaLHP, h.MaGV }).ToListAsync();
            var lhpIds = lhps.Select(h => h.MaLHP).ToList();
            var raw = await db.LichHocs.AsNoTracking().Where(l => lhpIds.Contains(l.MaLHP))
                .Select(l => new { l.MaLHP, l.MaPhong, l.SoTiet, l.TuanBatDau, l.TuanKetThuc }).ToListAsync();
            var gvIds = lhps.Select(h => h.MaGV).Distinct().ToList();
            var gvs = await db.GiangViens.AsNoTracking().Where(g => gvIds.Contains(g.MaGV)).Select(g => new { g.MaGV, g.HoTen }).ToListAsync();
            var phongs = await db.PhongHocs.AsNoTracking().OrderBy(p => p.TenPhong).Select(p => new { p.MaPhong, p.TenPhong }).ToListAsync();

            const double tietTuan = 12 * 6; // 12 tiết x 6 ngày (T2–T7)
            int Tuans(int a, int b) => Math.Max(1, b - a + 1);

            var rr = phongs.Select(p =>
            {
                var l = raw.Where(x => x.MaPhong == p.MaPhong).ToList();
                int tt = l.Sum(x => x.SoTiet);
                return new ReportRow { Ten = p.TenPhong, SoBuoi = l.Count, TietTuan = tt, TongTiet = l.Sum(x => x.SoTiet * Tuans(x.TuanBatDau, x.TuanKetThuc)), TyLe = Math.Round(tt / tietTuan * 100, 1) };
            }).ToList();

            var lr = raw.GroupBy(x => lhps.FirstOrDefault(h => h.MaLHP == x.MaLHP)?.MaGV ?? "(không rõ)").Select(g => new ReportRow
            {
                Ten = gvs.FirstOrDefault(v => v.MaGV == g.Key)?.HoTen ?? g.Key,
                SoBuoi = g.Count(), TietTuan = g.Sum(x => x.SoTiet), TongTiet = g.Sum(x => x.SoTiet * Tuans(x.TuanBatDau, x.TuanKetThuc))
            }).OrderByDescending(x => x.TongTiet).ToList();
            return (rr, lr);
        }

        private async Task<List<EnrollRow>> EnrollAsync(int maHK)
        {
            var lhps = await db.LopHocPhans.AsNoTracking().Where(l => l.MaHK == maHK).OrderBy(l => l.MaLHP).ToListAsync();
            var ids = lhps.Select(l => l.MaLHP).ToList();
            var maMHs = lhps.Select(l => l.MaMH).Distinct().ToList();
            var maGVs = lhps.Select(l => l.MaGV).Distinct().ToList();
            var mons = await db.MonHocs.AsNoTracking().Where(m => maMHs.Contains(m.MaMH)).Select(m => new { m.MaMH, m.TenMH }).ToListAsync();
            var gvs = await db.GiangViens.AsNoTracking().Where(g => maGVs.Contains(g.MaGV)).Select(g => new { g.MaGV, g.HoTen }).ToListAsync();
            var siSo = await db.DangKys.AsNoTracking().Where(d => ids.Contains(d.MaLHP)).GroupBy(d => d.MaLHP).Select(g => new { MaLHP = g.Key, N = g.Count() }).ToListAsync();
            return lhps.Select(l =>
            {
                int n = siSo.FirstOrDefault(x => x.MaLHP == l.MaLHP)?.N ?? 0;
                return new EnrollRow
                {
                    MaLHP = l.MaLHP, TenMon = mons.FirstOrDefault(m => m.MaMH == l.MaMH)?.TenMH ?? l.MaMH,
                    GiangVien = gvs.FirstOrDefault(g => g.MaGV == l.MaGV)?.HoTen ?? l.MaGV,
                    SiSo = n, SiSoToiDa = l.SiSoToiDa, TrangThai = l.TrangThai,
                    TyLe = l.SiSoToiDa > 0 ? Math.Round(n * 100.0 / l.SiSoToiDa, 1) : 0
                };
            }).ToList();
        }

        public async Task<IActionResult> Index(int maHK = 0)
        {
            var hocKys = await db.HocKys.AsNoTracking().OrderByDescending(h => h.NgayBatDau).ToListAsync();
            if (maHK == 0) maHK = (await HocKyHienTaiAsync())?.MaHK ?? 0;
            ViewBag.HocKys = new SelectList(hocKys, "MaHK", "Ten", maHK);
            ViewBag.MaHK = maHK;
            var (rooms, lecturers) = await BuildAsync(maHK);
            ViewBag.Rooms = rooms; ViewBag.Lecturers = lecturers;
            ViewBag.Enroll = await EnrollAsync(maHK);
            var lhpIds = await db.LopHocPhans.Where(l => l.MaHK == maHK).Select(l => l.MaLHP).ToListAsync();
            ViewBag.SoSVDangKy = await db.DangKys.Where(d => lhpIds.Contains(d.MaLHP)).Select(d => d.MaSV).Distinct().CountAsync();
            return View();
        }

        /// <summary>Xuất CSV (UTF-8 có BOM) – mở trực tiếp bằng Excel.</summary>
        public async Task<IActionResult> ExportCsv(int maHK, string loai = "phong")
        {
            var sb = new StringBuilder();
            if (loai == "lop")
            {
                sb.AppendLine("Mã LHP,Môn học,Giảng viên,Đã đăng ký,Sĩ số tối đa,Tỷ lệ lấp đầy (%),Trạng thái");
                foreach (var e in await EnrollAsync(maHK))
                    sb.AppendLine($"{e.MaLHP},\"{e.TenMon}\",\"{e.GiangVien}\",{e.SiSo},{e.SiSoToiDa},{e.TyLe.ToString(System.Globalization.CultureInfo.InvariantCulture)},{e.TrangThai}");
            }
            else
            {
                var (rooms, lecturers) = await BuildAsync(maHK);
                var data = loai == "giangvien" ? lecturers : rooms;
                sb.AppendLine(loai == "giangvien" ? "Giảng viên,Số buổi/tuần,Số tiết/tuần,Tổng số tiết học kỳ" : "Phòng,Số buổi/tuần,Số tiết/tuần,Tổng số tiết học kỳ,Công suất (%)");
                foreach (var r in data)
                    sb.AppendLine(loai == "giangvien"
                        ? $"\"{r.Ten}\",{r.SoBuoi},{r.TietTuan},{r.TongTiet}"
                        : $"\"{r.Ten}\",{r.SoBuoi},{r.TietTuan},{r.TongTiet},{r.TyLe.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
            }
            var bytes = new UTF8Encoding(true).GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
            return File(bytes, "text/csv", $"baocao_{loai}_hk{maHK}.csv");
        }

        // ===================== Kiểm tra / sửa dữ liệu mồ côi =====================
        [HttpGet]
        public async Task<IActionResult> Health()
        {
            var vm = new Dictionary<string, List<string>>();
            vm["Lớp học phần thiếu môn học / giảng viên / học kỳ"] = (await db.LopHocPhans.AsNoTracking()
                .Where(l => !db.MonHocs.Any(m => m.MaMH == l.MaMH) || !db.GiangViens.Any(g => g.MaGV == l.MaGV) || !db.HocKys.Any(h => h.MaHK == l.MaHK))
                .Select(l => new { l.MaLHP, l.MaMH, l.MaGV, l.MaHK }).ToListAsync())
                .Select(x => $"{x.MaLHP} (môn: {x.MaMH}, giảng viên: {x.MaGV}, học kỳ: {x.MaHK})").ToList();
            vm["Buổi học thiếu lớp học phần / phòng học"] = (await db.LichHocs.AsNoTracking()
                .Where(l => !db.LopHocPhans.Any(h => h.MaLHP == l.MaLHP) || !db.PhongHocs.Any(p => p.MaPhong == l.MaPhong))
                .Select(l => new { l.MaLich, l.MaLHP, l.MaPhong }).ToListAsync())
                .Select(x => $"Lịch #{x.MaLich} (lớp: {x.MaLHP}, phòng: {x.MaPhong})").ToList();
            vm["Đăng ký thiếu sinh viên / lớp học phần"] = (await db.DangKys.AsNoTracking()
                .Where(d => !db.SinhViens.Any(s => s.MaSV == d.MaSV) || !db.LopHocPhans.Any(l => l.MaLHP == d.MaLHP))
                .Select(d => new { d.MaSV, d.MaLHP }).ToListAsync())
                .Select(x => $"{x.MaSV} → {x.MaLHP}").ToList();
            vm["Sinh viên chưa liên kết tài khoản"] = await db.SinhViens.AsNoTracking().Where(s => s.MaND == null).Select(s => s.MaSV).ToListAsync();
            vm["Giảng viên chưa liên kết tài khoản"] = await db.GiangViens.AsNoTracking().Where(g => g.MaND == null).Select(g => g.MaGV).ToListAsync();
            ViewBag.Counts = new Dictionary<string, int>
            {
                ["Sinh viên"] = await db.SinhViens.CountAsync(), ["Giảng viên"] = await db.GiangViens.CountAsync(),
                ["Môn học"] = await db.MonHocs.CountAsync(), ["Phòng học"] = await db.PhongHocs.CountAsync(),
                ["Lớp học phần"] = await db.LopHocPhans.CountAsync(), ["Buổi học"] = await db.LichHocs.CountAsync(),
                ["Lượt đăng ký"] = await db.DangKys.CountAsync()
            };
            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> Fix()
        {
            var dk = await db.DangKys.Where(d => !db.SinhViens.Any(s => s.MaSV == d.MaSV) || !db.LopHocPhans.Any(l => l.MaLHP == d.MaLHP)).ToListAsync();
            var badLhp = await db.LopHocPhans.Where(l => !db.MonHocs.Any(m => m.MaMH == l.MaMH) || !db.GiangViens.Any(g => g.MaGV == l.MaGV) || !db.HocKys.Any(h => h.MaHK == l.MaHK)).Select(l => l.MaLHP).ToListAsync();
            var lich = await db.LichHocs.Where(l => !db.LopHocPhans.Any(h => h.MaLHP == l.MaLHP) || !db.PhongHocs.Any(p => p.MaPhong == l.MaPhong) || badLhp.Contains(l.MaLHP)).ToListAsync();
            var dk2 = await db.DangKys.Where(d => badLhp.Contains(d.MaLHP)).ToListAsync();
            var lhp = await db.LopHocPhans.Where(l => badLhp.Contains(l.MaLHP)).ToListAsync();
            int n = dk.Count + dk2.Count + lich.Count + lhp.Count;
            db.RemoveRange(dk); db.RemoveRange(dk2); db.RemoveRange(lich);
            await db.SaveChangesAsync();
            db.RemoveRange(lhp);
            await db.SaveChangesAsync();
            TempData["Fixed"] = n;
            return RedirectToAction(nameof(Health));
        }
    }
}
