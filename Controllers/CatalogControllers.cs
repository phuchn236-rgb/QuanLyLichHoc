using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyLichHoc.Data;
using QuanLyLichHoc.Infrastructure;
using QuanLyLichHoc.Models;

namespace QuanLyLichHoc.Controllers
{
    // ===================== Phòng học =====================
    public class RoomsController : CrudController<PhongHoc>
    {
        private readonly AppDbContext db;
        public RoomsController(AppDbContext db) : base(db) { this.db = db; }
        protected override string Title => "Quản lý phòng học";
        protected override string FilterPartial => "_Filter";
        protected override string KeyOf(PhongHoc m) => m.MaPhong.ToString();
        protected override Task<PhongHoc> FindAsync(string id) => db.PhongHocs.FindAsync(I(id)).AsTask();
        protected override string Describe(PhongHoc m) => "Phòng " + m.TenPhong;

        protected override IQueryable<PhongHoc> Items(string q, IQueryCollection f)
        {
            var x = db.PhongHocs.AsNoTracking();
            if (!string.IsNullOrWhiteSpace(q)) x = x.Where(p => p.TenPhong.Contains(q) || p.ToaNha.Contains(q));
            string loai = f["loai"].ToString();
            if (!string.IsNullOrEmpty(loai)) x = x.Where(p => p.LoaiPhong == loai);
            return x.OrderBy(p => p.TenPhong);
        }

        protected override async Task<List<string>> ValidateAsync(PhongHoc m, bool isNew)
        {
            var e = new List<string>();
            if (m.LoaiPhong != "LT" && m.LoaiPhong != "TH") e.Add("Loại phòng phải là LT hoặc TH.");
            if (await db.PhongHocs.AnyAsync(p => p.TenPhong == m.TenPhong && p.MaPhong != m.MaPhong)) e.Add("Tên phòng đã tồn tại.");
            return e;
        }

        protected override async Task<string> CanDeleteAsync(PhongHoc p) =>
            await db.LichHocs.AnyAsync(l => l.MaPhong == p.MaPhong) ? "Phòng đang có lịch học, hãy xóa/đổi lịch trước." : null;
    }

    // ===================== Môn học =====================
    public class SubjectsController : CrudController<MonHoc>
    {
        private readonly AppDbContext db;
        public SubjectsController(AppDbContext db) : base(db) { this.db = db; }
        protected override string Title => "Quản lý môn học";
        protected override string KeyOf(MonHoc m) => m.MaMH;
        protected override Task<MonHoc> FindAsync(string id) => db.MonHocs.FindAsync(id).AsTask();
        protected override string Describe(MonHoc m) => "Môn " + m.MaMH + " - " + m.TenMH;

        protected override IQueryable<MonHoc> Items(string q, IQueryCollection f)
        {
            var x = db.MonHocs.AsNoTracking().Include(m => m.Khoa).AsQueryable();
            if (!string.IsNullOrWhiteSpace(q)) x = x.Where(m => m.MaMH.Contains(q) || m.TenMH.Contains(q));
            return x.OrderBy(m => m.MaMH);
        }

        protected override async Task PrepareFormAsync(MonHoc m, bool isNew) =>
            ViewBag.Khoas = Sel(await db.Khoas.OrderBy(k => k.TenKhoa).ToListAsync(), "MaKhoa", "TenKhoa", m.MaKhoa);

        protected override async Task<List<string>> ValidateAsync(MonHoc m, bool isNew)
        {
            var e = new List<string>();
            if (isNew && await db.MonHocs.AnyAsync(x => x.MaMH == m.MaMH)) e.Add("Mã môn học đã tồn tại.");
            if (!await db.Khoas.AnyAsync(k => k.MaKhoa == m.MaKhoa)) e.Add("Vui lòng chọn khoa.");
            return e;
        }
    }

    // ===================== Học kỳ =====================
    public class SemestersController : CrudController<HocKy>
    {
        private readonly AppDbContext db;
        public SemestersController(AppDbContext db) : base(db) { this.db = db; }
        protected override string Title => "Quản lý học kỳ";
        protected override string KeyOf(HocKy m) => m.MaHK.ToString();
        protected override Task<HocKy> FindAsync(string id) => db.HocKys.FindAsync(I(id)).AsTask();
        protected override string Describe(HocKy m) => "Học kỳ " + m.TenHK + " " + m.NamHoc;

        protected override IQueryable<HocKy> Items(string q, IQueryCollection f)
        {
            var x = db.HocKys.AsNoTracking();
            if (!string.IsNullOrWhiteSpace(q)) x = x.Where(h => h.TenHK.Contains(q) || h.NamHoc.Contains(q));
            return x.OrderByDescending(h => h.NgayBatDau);
        }

        protected override Task<List<string>> ValidateAsync(HocKy m, bool isNew)
        {
            var e = new List<string>();
            if (m.NgayKetThuc <= m.NgayBatDau) e.Add("Ngày kết thúc phải sau ngày bắt đầu.");
            return Task.FromResult(e);
        }

        protected override async Task<string> CanDeleteAsync(HocKy h) =>
            await db.LopHocPhans.AnyAsync(l => l.MaHK == h.MaHK) ? "Học kỳ đã có lớp học phần, không thể xóa." : null;
    }

    // ===================== Khoa =====================
    public class FacultiesController : CrudController<Khoa>
    {
        private readonly AppDbContext db;
        public FacultiesController(AppDbContext db) : base(db) { this.db = db; }
        protected override string Title => "Quản lý khoa";
        protected override string KeyOf(Khoa m) => m.MaKhoa.ToString();
        protected override Task<Khoa> FindAsync(string id) => db.Khoas.FindAsync(I(id)).AsTask();
        protected override string Describe(Khoa m) => "Khoa " + m.TenKhoa;

        protected override IQueryable<Khoa> Items(string q, IQueryCollection f)
        {
            var x = db.Khoas.AsNoTracking();
            if (!string.IsNullOrWhiteSpace(q)) x = x.Where(k => k.TenKhoa.Contains(q));
            return x.OrderBy(k => k.TenKhoa);
        }

        protected override async Task<List<string>> ValidateAsync(Khoa m, bool isNew)
        {
            var e = new List<string>();
            if (await db.Khoas.AnyAsync(k => k.TenKhoa == m.TenKhoa && k.MaKhoa != m.MaKhoa)) e.Add("Tên khoa đã tồn tại.");
            return e;
        }
    }

    // ===================== Lớp (hành chính) =====================
    public class ClassesController : CrudController<Lop>
    {
        private readonly AppDbContext db;
        public ClassesController(AppDbContext db) : base(db) { this.db = db; }
        protected override string Title => "Quản lý lớp";
        protected override string KeyOf(Lop m) => m.MaLop.ToString();
        protected override Task<Lop> FindAsync(string id) => db.Lops.FindAsync(I(id)).AsTask();
        protected override string Describe(Lop m) => "Lớp " + m.TenLop;

        protected override IQueryable<Lop> Items(string q, IQueryCollection f)
        {
            var x = db.Lops.AsNoTracking().Include(l => l.Khoa).AsQueryable();
            if (!string.IsNullOrWhiteSpace(q)) x = x.Where(l => l.TenLop.Contains(q));
            return x.OrderBy(l => l.TenLop);
        }

        protected override async Task PrepareFormAsync(Lop m, bool isNew) =>
            ViewBag.Khoas = Sel(await db.Khoas.OrderBy(k => k.TenKhoa).ToListAsync(), "MaKhoa", "TenKhoa", m.MaKhoa);

        protected override async Task<List<string>> ValidateAsync(Lop m, bool isNew)
        {
            var e = new List<string>();
            if (!await db.Khoas.AnyAsync(k => k.MaKhoa == m.MaKhoa)) e.Add("Vui lòng chọn khoa.");
            return e;
        }
    }

    // ===================== Giảng viên =====================
    public class LecturersController : CrudController<GiangVien>
    {
        private readonly AppDbContext db;
        public LecturersController(AppDbContext db) : base(db) { this.db = db; }
        protected override string Title => "Quản lý giảng viên";
        protected override string KeyOf(GiangVien m) => m.MaGV;
        protected override Task<GiangVien> FindAsync(string id) => db.GiangViens.FindAsync(id).AsTask();
        protected override string Describe(GiangVien m) => "Giảng viên " + m.MaGV + " - " + m.HoTen;

        protected override IQueryable<GiangVien> Items(string q, IQueryCollection f)
        {
            var x = db.GiangViens.AsNoTracking().Include(g => g.Khoa).AsQueryable();
            if (!string.IsNullOrWhiteSpace(q)) x = x.Where(g => g.MaGV.Contains(q) || g.HoTen.Contains(q) || g.Email.Contains(q));
            return x.OrderBy(g => g.MaGV);
        }

        protected override async Task PrepareFormAsync(GiangVien m, bool isNew) =>
            ViewBag.Khoas = Sel(await db.Khoas.OrderBy(k => k.TenKhoa).ToListAsync(), "MaKhoa", "TenKhoa", m.MaKhoa);

        protected override void CopyValues(GiangVien existing, GiangVien model)
        {
            var nd = existing.MaND;
            base.CopyValues(existing, model);
            existing.MaND = nd;
        }

        protected override async Task<List<string>> ValidateAsync(GiangVien m, bool isNew)
        {
            var e = new List<string>();
            if (isNew && await db.GiangViens.AnyAsync(x => x.MaGV == m.MaGV)) e.Add("Mã giảng viên đã tồn tại.");
            if (await db.GiangViens.AnyAsync(x => x.Email == m.Email && x.MaGV != m.MaGV)) e.Add("Email đã được sử dụng.");
            if (!await db.Khoas.AnyAsync(k => k.MaKhoa == m.MaKhoa)) e.Add("Vui lòng chọn khoa.");
            return e;
        }

        protected override async Task AfterCreateAsync(GiangVien m)
        {
            // Tự tạo tài khoản đăng nhập: tên = mã GV (chữ thường), mật khẩu mặc định GiangVien@123
            var user = new NguoiDung { TenDangNhap = m.MaGV.ToLower(), HoTen = m.HoTen, VaiTro = VaiTro.GiangVien, MatKhauHash = PasswordHasher.Hash("GiangVien@123") };
            if (!await db.NguoiDungs.AnyAsync(u => u.TenDangNhap == user.TenDangNhap))
            {
                db.NguoiDungs.Add(user);
                await db.SaveChangesAsync();
                m.MaND = user.MaND;
                await db.SaveChangesAsync();
            }
        }

        protected override async Task<string> CanDeleteAsync(GiangVien g) =>
            await db.LopHocPhans.AnyAsync(l => l.MaGV == g.MaGV) ? "Giảng viên đang phụ trách lớp học phần, không thể xóa." : null;
    }

    // ===================== Sinh viên =====================
    public class StudentsController : CrudController<SinhVien>
    {
        private readonly AppDbContext db;
        public StudentsController(AppDbContext db) : base(db) { this.db = db; }
        protected override string Title => "Quản lý sinh viên";
        protected override string FilterPartial => "_Filter";
        protected override string KeyOf(SinhVien m) => m.MaSV;
        protected override Task<SinhVien> FindAsync(string id) => db.SinhViens.FindAsync(id).AsTask();
        protected override string Describe(SinhVien m) => "Sinh viên " + m.MaSV + " - " + m.HoTen;

        protected override async Task PrepareIndexAsync() =>
            ViewBag.Lops = Sel(await db.Lops.OrderBy(l => l.TenLop).ToListAsync(), "MaLop", "TenLop");

        protected override IQueryable<SinhVien> Items(string q, IQueryCollection f)
        {
            var x = db.SinhViens.AsNoTracking().Include(s => s.Lop).AsQueryable();
            if (!string.IsNullOrWhiteSpace(q)) x = x.Where(s => s.MaSV.Contains(q) || s.HoTen.Contains(q) || s.Email.Contains(q));
            int lop = I(f["maLop"].ToString());
            if (lop > 0) x = x.Where(s => s.MaLop == lop);
            return x.OrderBy(s => s.MaSV);
        }

        protected override async Task PrepareFormAsync(SinhVien m, bool isNew) =>
            ViewBag.Lops = Sel(await db.Lops.OrderBy(l => l.TenLop).ToListAsync(), "MaLop", "TenLop", m.MaLop);

        protected override void CopyValues(SinhVien existing, SinhVien model)
        {
            var nd = existing.MaND;
            base.CopyValues(existing, model);
            existing.MaND = nd;
        }

        protected override async Task<List<string>> ValidateAsync(SinhVien m, bool isNew)
        {
            var e = new List<string>();
            if (m.NgaySinh >= DateTime.Today) e.Add("Ngày sinh phải nhỏ hơn ngày hiện tại.");
            if (isNew && await db.SinhViens.AnyAsync(x => x.MaSV == m.MaSV)) e.Add("Mã sinh viên đã tồn tại.");
            if (await db.SinhViens.AnyAsync(x => x.Email == m.Email && x.MaSV != m.MaSV)) e.Add("Email đã được sử dụng.");
            if (!await db.Lops.AnyAsync(l => l.MaLop == m.MaLop)) e.Add("Vui lòng chọn lớp.");
            return e;
        }

        protected override async Task AfterCreateAsync(SinhVien m)
        {
            var user = new NguoiDung { TenDangNhap = m.MaSV.ToLower(), HoTen = m.HoTen, VaiTro = VaiTro.SinhVien, MatKhauHash = PasswordHasher.Hash("SinhVien@123") };
            if (!await db.NguoiDungs.AnyAsync(u => u.TenDangNhap == user.TenDangNhap))
            {
                db.NguoiDungs.Add(user);
                await db.SaveChangesAsync();
                m.MaND = user.MaND;
                await db.SaveChangesAsync();
            }
        }

        protected override async Task<string> CanDeleteAsync(SinhVien s) =>
            await db.DangKys.AnyAsync(d => d.MaSV == s.MaSV) ? "Sinh viên đã đăng ký học phần, không thể xóa." : null;
    }

    // ===================== Lớp học phần =====================
    public class SectionsController : CrudController<LopHocPhan>
    {
        private readonly AppDbContext db;
        public SectionsController(AppDbContext db) : base(db) { this.db = db; }
        protected override string Title => "Quản lý lớp học phần";
        protected override string FilterPartial => "_Filter";
        protected override string KeyOf(LopHocPhan m) => m.MaLHP;
        protected override Task<LopHocPhan> FindAsync(string id) => db.LopHocPhans.FindAsync(id).AsTask();
        protected override string Describe(LopHocPhan m) => "Lớp học phần " + m.MaLHP;

        protected override async Task PrepareIndexAsync() =>
            ViewBag.HocKys = Sel(await db.HocKys.OrderByDescending(h => h.NgayBatDau).ToListAsync(), "MaHK", "Ten");

        protected override IQueryable<LopHocPhan> Items(string q, IQueryCollection f)
        {
            var x = db.LopHocPhans.AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(q))
            {
                var mhKeys = db.MonHocs.Where(m => m.TenMH.Contains(q)).Select(m => m.MaMH);
                var gvKeys = db.GiangViens.Where(g => g.HoTen.Contains(q)).Select(g => g.MaGV);
                x = x.Where(l => l.MaLHP.Contains(q) || mhKeys.Contains(l.MaMH) || gvKeys.Contains(l.MaGV));
            }
            int hk = I(f["maHK"].ToString());
            if (hk > 0) x = x.Where(l => l.MaHK == hk);
            return x.OrderBy(l => l.MaLHP);
        }

        // Tên môn, giảng viên, học kỳ và sĩ số thực tế được tra cứu theo khóa (đảm bảo hiển thị đúng và sĩ số cập nhật theo đăng ký)
        protected override async Task AfterListAsync(PaginatedList<LopHocPhan> list)
        {
            var ids = list.Select(l => l.MaLHP).ToList();
            var mh = list.Select(l => l.MaMH).Distinct().ToList();
            var gv = list.Select(l => l.MaGV).Distinct().ToList();
            var hk = list.Select(l => l.MaHK).Distinct().ToList();
            ViewBag.MonNames = (await db.MonHocs.AsNoTracking().Where(m => mh.Contains(m.MaMH)).Select(m => new { m.MaMH, m.TenMH }).ToListAsync()).ToDictionary(x => x.MaMH, x => x.TenMH);
            ViewBag.GvNames = (await db.GiangViens.AsNoTracking().Where(g => gv.Contains(g.MaGV)).Select(g => new { g.MaGV, g.HoTen }).ToListAsync()).ToDictionary(x => x.MaGV, x => x.HoTen);
            ViewBag.HkNames = (await db.HocKys.AsNoTracking().Where(h => hk.Contains(h.MaHK)).Select(h => new { h.MaHK, h.TenHK, h.NamHoc }).ToListAsync()).ToDictionary(x => x.MaHK, x => x.TenHK + " (" + x.NamHoc + ")");
            ViewBag.SiSoMap = (await db.DangKys.AsNoTracking().Where(d => ids.Contains(d.MaLHP)).GroupBy(d => d.MaLHP).Select(g => new { g.Key, N = g.Count() }).ToListAsync()).ToDictionary(x => x.Key, x => x.N);
        }

        protected override async Task PrepareFormAsync(LopHocPhan m, bool isNew)
        {
            ViewBag.Mons = Sel((await db.MonHocs.OrderBy(x => x.MaMH).ToListAsync()).Select(x => new { x.MaMH, Ten = x.MaMH + " - " + x.TenMH }), "MaMH", "Ten", m.MaMH);
            ViewBag.GVs = Sel((await db.GiangViens.OrderBy(x => x.MaGV).ToListAsync()).Select(x => new { x.MaGV, Ten = x.MaGV + " - " + x.HoTen }), "MaGV", "Ten", m.MaGV);
            ViewBag.HocKys = Sel(await db.HocKys.OrderByDescending(h => h.NgayBatDau).ToListAsync(), "MaHK", "Ten", m.MaHK);
            ViewBag.TrangThais = Sel(TrangThaiLHP.All.Select(t => new { Id = t, Ten = t }), "Id", "Ten", m.TrangThai);
        }

        protected override async Task<List<string>> ValidateAsync(LopHocPhan m, bool isNew)
        {
            var e = new List<string>();
            if (isNew && await db.LopHocPhans.AnyAsync(x => x.MaLHP == m.MaLHP)) e.Add("Mã lớp học phần đã tồn tại.");
            if (!await db.MonHocs.AnyAsync(x => x.MaMH == m.MaMH)) e.Add("Môn học không tồn tại.");
            if (!await db.GiangViens.AnyAsync(x => x.MaGV == m.MaGV)) e.Add("Giảng viên không tồn tại.");
            if (!await db.HocKys.AnyAsync(x => x.MaHK == m.MaHK)) e.Add("Học kỳ không tồn tại.");
            if (!TrangThaiLHP.All.Contains(m.TrangThai)) e.Add("Trạng thái không hợp lệ.");
            if (!isNew)
            {
                int daDk = await db.DangKys.CountAsync(d => d.MaLHP == m.MaLHP);
                if (m.SiSoToiDa < daDk) e.Add($"Sĩ số tối đa không được nhỏ hơn số sinh viên đã đăng ký ({daDk}).");
                var nho = await db.LichHocs.Where(l => l.MaLHP == m.MaLHP && l.PhongHoc.SucChua < m.SiSoToiDa).Select(l => l.PhongHoc.TenPhong).ToListAsync();
                if (nho.Count > 0) e.Add("Sĩ số lớn hơn sức chứa của phòng: " + string.Join(", ", nho.Distinct()) + ".");
            }
            return e;
        }

        protected override async Task<string> CanDeleteAsync(LopHocPhan l)
        {
            if (await db.DangKys.AnyAsync(d => d.MaLHP == l.MaLHP)) return "Lớp đã có sinh viên đăng ký, hãy chuyển trạng thái sang Hủy thay vì xóa.";
            if (await db.LichHocs.AnyAsync(x => x.MaLHP == l.MaLHP)) return "Lớp đã có lịch học, hãy xóa lịch học trước.";
            return null;
        }
    }

    // ===================== Lịch học (xếp lịch) =====================
    public class SchedulesController : CrudController<LichHoc>
    {
        private readonly AppDbContext db;
        public SchedulesController(AppDbContext db) : base(db) { this.db = db; }
        protected override string Title => "Xếp lịch học";
        protected override string FilterPartial => "_Filter";
        protected override string KeyOf(LichHoc m) => m.MaLich.ToString();
        protected override Task<LichHoc> FindAsync(string id) => db.LichHocs.FindAsync(I(id)).AsTask();
        protected override string Describe(LichHoc m) => "Lịch học " + TkbHelper.Mo(m);

        protected override async Task PrepareIndexAsync()
        {
            ViewBag.HocKys = Sel(await db.HocKys.OrderByDescending(h => h.NgayBatDau).ToListAsync(), "MaHK", "Ten");
            ViewBag.GVs = Sel((await db.GiangViens.OrderBy(x => x.MaGV).ToListAsync()).Select(x => new { x.MaGV, Ten = x.MaGV + " - " + x.HoTen }), "MaGV", "Ten");
            ViewBag.Phongs = Sel(await db.PhongHocs.OrderBy(p => p.TenPhong).ToListAsync(), "MaPhong", "TenPhong");
        }

        protected override IQueryable<LichHoc> Items(string q, IQueryCollection f)
        {
            var x = db.LichHocs.AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(q))
                x = x.Where(l => l.MaLHP.Contains(q)
                    || db.PhongHocs.Any(p => p.MaPhong == l.MaPhong && p.TenPhong.Contains(q))
                    || db.LopHocPhans.Any(h => h.MaLHP == l.MaLHP && (db.MonHocs.Any(m => m.MaMH == h.MaMH && m.TenMH.Contains(q))
                                                                     || db.GiangViens.Any(g => g.MaGV == h.MaGV && g.HoTen.Contains(q)))));
            int hk = I(f["maHK"].ToString()), phong = I(f["maPhong"].ToString()), thu = I(f["thu"].ToString());
            string gv = f["maGV"].ToString();
            if (hk > 0) x = x.Where(l => db.LopHocPhans.Any(h => h.MaLHP == l.MaLHP && h.MaHK == hk));
            if (!string.IsNullOrEmpty(gv)) x = x.Where(l => db.LopHocPhans.Any(h => h.MaLHP == l.MaLHP && h.MaGV == gv));
            if (phong > 0) x = x.Where(l => l.MaPhong == phong);
            if (thu > 0) x = x.Where(l => l.Thu == thu);
            return x.OrderBy(l => l.Thu).ThenBy(l => l.TietBatDau).ThenBy(l => l.MaLHP);
        }

        protected override async Task AfterListAsync(PaginatedList<LichHoc> list) =>
            ViewBag.Dtos = (await ToDtosAsync(list)).ToDictionary(d => d.MaLich);

        protected override async Task PrepareFormAsync(LichHoc m, bool isNew)
        {
            // Dựng nhãn ngay trong truy vấn (an toàn khi thiếu môn học/giảng viên, không gây NullReferenceException)
            var lhps = await db.LopHocPhans.OrderBy(l => l.MaLHP)
                .Select(l => new { l.MaLHP, Ten = l.MaLHP + " - " + (l.MonHoc.TenMH ?? "?") + " (" + (l.GiangVien.HoTen ?? "?") + ")" }).ToListAsync();
            ViewBag.LHPs = Sel(lhps, "MaLHP", "Ten", m.MaLHP);
            ViewBag.Phongs = Sel((await db.PhongHocs.OrderBy(p => p.TenPhong).ToListAsync()).Select(p => new { p.MaPhong, Ten = $"{p.TenPhong} ({p.LoaiPhong}, {p.SucChua} chỗ)" }), "MaPhong", "Ten", m.MaPhong);
            ViewBag.Thus = Sel(Enumerable.Range(2, 7).Select(t => new { Id = t, Ten = TkbHelper.ThuText(t) }), "Id", "Ten", m.Thu);
        }

        protected override Task<List<string>> ValidateAsync(LichHoc m, bool isNew) => KiemTraAsync(m);

        // ===================== Nghiệp vụ lịch học (xử lý trực tiếp bằng DbContext) =====================

        /// <summary>Kiểm tra mọi ràng buộc của một buổi học. Trả về danh sách lỗi (rỗng = hợp lệ).</summary>
        private async Task<List<string>> KiemTraAsync(LichHoc m)
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

            // Các buổi học cùng học kỳ giao nhau về thứ, tiết, tuần
            var cands = await db.LichHocs.AsNoTracking()
                .Where(x => x.MaLich != m.MaLich && x.Thu == m.Thu
                            && db.LopHocPhans.Any(h => h.MaLHP == x.MaLHP && h.MaHK == lhp.MaHK)
                            && x.TietBatDau <= ketThuc && m.TietBatDau <= x.TietBatDau + x.SoTiet - 1
                            && x.TuanBatDau <= m.TuanKetThuc && m.TuanBatDau <= x.TuanKetThuc)
                .ToListAsync();
            var candIds = cands.Select(x => x.MaLHP).Distinct().ToList();
            var gvOf = await db.LopHocPhans.AsNoTracking().Where(h => candIds.Contains(h.MaLHP)).Select(h => new { h.MaLHP, h.MaGV }).ToListAsync();

            foreach (var x in cands.Where(x => x.MaPhong == m.MaPhong))
                err.Add($"Trùng phòng {phong.TenPhong} với {TkbHelper.Mo(x)}.");
            foreach (var x in cands.Where(x => gvOf.Any(g => g.MaLHP == x.MaLHP && g.MaGV == lhp.MaGV)))
                err.Add($"Giảng viên bị trùng lịch dạy với {TkbHelper.Mo(x)}.");

            // Trùng lịch sinh viên: có sinh viên đăng ký cả hai lớp
            var others = candIds.Where(id => id != m.MaLHP).ToList();
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

        /// <summary>Chuyển danh sách buổi học thành DTO có tên môn, giảng viên, phòng (tra cứu theo khóa).</summary>
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

        /// <summary>AJAX: kiểm tra xung đột ngay khi người dùng đổi phòng / thứ / tiết / tuần.</summary>
        [HttpPost]
        public async Task<IActionResult> Check(LichHoc model)
        {
            var errors = new List<string>();
            if (!ModelState.IsValid) errors.AddRange(ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).Where(s => !string.IsNullOrEmpty(s)));
            else errors.AddRange(await KiemTraAsync(model));
            return Json(new { ok = errors.Count == 0, errors });
        }
    }

    // ===================== Tài khoản & phân quyền (chỉ Admin) =====================
    [Authorize(Roles = "Admin")]
    public class UsersController : CrudController<NguoiDung>
    {
        private readonly AppDbContext db;
        public UsersController(AppDbContext db) : base(db) { this.db = db; }
        protected override string Title => "Quản lý tài khoản";
        protected override string FilterPartial => "_Filter";
        protected override string KeyOf(NguoiDung m) => m.MaND.ToString();
        protected override Task<NguoiDung> FindAsync(string id) => db.NguoiDungs.FindAsync(I(id)).AsTask();
        protected override string Describe(NguoiDung m) => "Tài khoản " + m.TenDangNhap + " (" + m.VaiTro + ")";

        protected override IQueryable<NguoiDung> Items(string q, IQueryCollection f)
        {
            var x = db.NguoiDungs.AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(q)) x = x.Where(u => u.TenDangNhap.Contains(q) || u.HoTen.Contains(q));
            string vt = f["vaiTro"].ToString();
            if (!string.IsNullOrEmpty(vt)) x = x.Where(u => u.VaiTro == vt);
            return x.OrderBy(u => u.VaiTro).ThenBy(u => u.TenDangNhap);
        }

        protected override Task PrepareFormAsync(NguoiDung m, bool isNew)
        {
            ViewBag.VaiTros = Sel(VaiTro.All.Select(v => new { Id = v, Ten = v }), "Id", "Ten", m.VaiTro);
            return Task.CompletedTask;
        }

        protected override async Task<List<string>> ValidateAsync(NguoiDung m, bool isNew)
        {
            var e = new List<string>();
            m.TenDangNhap = m.TenDangNhap?.Trim().ToLower();
            if (!VaiTro.All.Contains(m.VaiTro)) e.Add("Vai trò không hợp lệ.");
            if (await db.NguoiDungs.AnyAsync(u => u.TenDangNhap == m.TenDangNhap && u.MaND != m.MaND)) e.Add("Tên đăng nhập đã tồn tại.");
            if (isNew && (m.MatKhauMoi == null || m.MatKhauMoi.Length < 8)) e.Add("Mật khẩu tối thiểu 8 ký tự.");
            if (!isNew && !string.IsNullOrEmpty(m.MatKhauMoi) && m.MatKhauMoi.Length < 8) e.Add("Mật khẩu mới tối thiểu 8 ký tự.");
            return e;
        }

        protected override Task BeforeCreateAsync(NguoiDung m)
        {
            m.MatKhauHash = PasswordHasher.Hash(m.MatKhauMoi);
            m.MatKhauMoi = null;
            return Task.CompletedTask;
        }

        protected override void CopyValues(NguoiDung existing, NguoiDung model)
        {
            existing.TenDangNhap = model.TenDangNhap;
            existing.HoTen = model.HoTen;
            existing.VaiTro = model.VaiTro;
            existing.TrangThai = model.TrangThai;
            if (model.TrangThai) { existing.SoLanSai = 0; existing.KhoaDen = null; }
            if (!string.IsNullOrEmpty(model.MatKhauMoi)) existing.MatKhauHash = PasswordHasher.Hash(model.MatKhauMoi);
        }

        protected override Task<string> CanDeleteAsync(NguoiDung u) =>
            Task.FromResult(u.TenDangNhap == User.Identity?.Name ? "Không thể xóa chính tài khoản đang đăng nhập." : null);
    }
}
