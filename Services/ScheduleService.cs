using Microsoft.EntityFrameworkCore;
using QuanLyLichHoc.Data;
using QuanLyLichHoc.Infrastructure;
using QuanLyLichHoc.Models;

namespace QuanLyLichHoc.Services
{
    public class ScheduleService
    {
        private readonly AppDbContext db;
        public ScheduleService(AppDbContext db) { this.db = db; }

        /// <summary>Kiểm tra mọi ràng buộc của một buổi học. Trả về danh sách lỗi (rỗng = hợp lệ).</summary>
        public async Task<List<string>> KiemTraAsync(LichHoc m)
        {
            var err = new List<string>();
            if (m.TuanKetThuc < m.TuanBatDau) err.Add("Tuần kết thúc phải lớn hơn hoặc bằng tuần bắt đầu.");
            int ketThuc = m.TietBatDau + m.SoTiet - 1;
            if (ketThuc > 12) err.Add("Lịch học vượt quá tiết 12 (tiết bắt đầu + số tiết − 1 phải ≤ 12).");
            if (err.Count > 0) return err;

            var lhp = await db.LopHocPhans.Include(x => x.MonHoc).FirstOrDefaultAsync(x => x.MaLHP == m.MaLHP);
            var phong = await db.PhongHocs.FindAsync(m.MaPhong);
            if (lhp == null) err.Add("Lớp học phần không tồn tại.");
            if (phong == null) err.Add("Phòng học không tồn tại.");
            if (err.Count > 0) return err;

            // RB5: loại phòng, RB4: sức chứa
            if (lhp.MonHoc.ThucHanh && phong.LoaiPhong != "TH") err.Add("Môn thực hành phải xếp vào phòng thực hành.");
            if (lhp.SiSoToiDa > phong.SucChua)
                err.Add($"Sức chứa phòng {phong.TenPhong} ({phong.SucChua}) nhỏ hơn sĩ số tối đa của lớp ({lhp.SiSoToiDa}).");

            // Các buổi học cùng học kỳ giao nhau về thứ, tiết, tuần
            var cands = await db.LichHocs.Include(x => x.LopHocPhan).Include(x => x.PhongHoc)
                .Where(x => x.MaLich != m.MaLich && x.LopHocPhan.MaHK == lhp.MaHK && x.Thu == m.Thu
                            && x.TietBatDau <= ketThuc && m.TietBatDau <= x.TietBatDau + x.SoTiet - 1
                            && x.TuanBatDau <= m.TuanKetThuc && m.TuanBatDau <= x.TuanKetThuc)
                .ToListAsync();

            foreach (var x in cands.Where(x => x.MaPhong == m.MaPhong))
                err.Add($"Trùng phòng {phong.TenPhong} với {TkbHelper.Mo(x)}.");
            foreach (var x in cands.Where(x => x.LopHocPhan.MaGV == lhp.MaGV))
                err.Add($"Giảng viên bị trùng lịch dạy với {TkbHelper.Mo(x)}.");

            // Trùng lịch sinh viên (RB3): có sinh viên đăng ký cả hai lớp
            var others = cands.Where(x => x.MaLHP != m.MaLHP).Select(x => x.MaLHP).Distinct().ToList();
            if (others.Count > 0)
            {
                int chung = await db.DangKys
                    .Where(d => d.MaLHP == m.MaLHP && db.DangKys.Any(e => e.MaSV == d.MaSV && others.Contains(e.MaLHP)))
                    .CountAsync();
                if (chung > 0) err.Add($"Có {chung} sinh viên đã đăng ký lớp khác trùng khung giờ này.");
            }

            // Lịch bận của giảng viên
            bool ban = await db.LichBans.AnyAsync(b => b.MaGV == lhp.MaGV && b.Thu == m.Thu
                        && b.TietBatDau <= ketThuc && m.TietBatDau <= b.TietBatDau + b.SoTiet - 1);
            if (ban) err.Add("Giảng viên đã đăng ký lịch bận trong khung giờ này.");
            return err;
        }

        public IQueryable<LichHoc> Query() => db.LichHocs.AsNoTracking()
            .Include(x => x.LopHocPhan).ThenInclude(l => l.MonHoc)
            .Include(x => x.LopHocPhan).ThenInclude(l => l.GiangVien)
            .Include(x => x.PhongHoc);

        private static IQueryable<LichHoc> Tuan(IQueryable<LichHoc> q, int? tuan) =>
            tuan == null ? q : q.Where(x => x.TuanBatDau <= tuan && tuan <= x.TuanKetThuc);

        public Task<List<LichHoc>> TheoSinhVienAsync(string maSV, int maHK, int? tuan) =>
            Tuan(Query().Where(x => x.LopHocPhan.MaHK == maHK && x.LopHocPhan.DangKys.Any(d => d.MaSV == maSV)), tuan)
                .OrderBy(x => x.Thu).ThenBy(x => x.TietBatDau).ToListAsync();

        public Task<List<LichHoc>> TheoGiangVienAsync(string maGV, int maHK, int? tuan) =>
            Tuan(Query().Where(x => x.LopHocPhan.MaHK == maHK && x.LopHocPhan.MaGV == maGV), tuan)
                .OrderBy(x => x.Thu).ThenBy(x => x.TietBatDau).ToListAsync();

        public async Task<HocKy> HocKyHienTaiAsync()
        {
            var today = DateTime.Today;
            return await db.HocKys.AsNoTracking().Where(h => h.NgayBatDau <= today && today <= h.NgayKetThuc).FirstOrDefaultAsync()
                   ?? await db.HocKys.AsNoTracking().OrderByDescending(h => h.NgayBatDau).FirstOrDefaultAsync();
        }

        public static int TuanHienTai(HocKy hk)
        {
            if (hk == null) return 1;
            int t = (int)Math.Floor((DateTime.Today - hk.NgayBatDau.Date).TotalDays / 7) + 1;
            return Math.Clamp(t, 1, 20);
        }
    }
}
