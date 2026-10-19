using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyLichHoc.Data;
using QuanLyLichHoc.Infrastructure;
using QuanLyLichHoc.Models;
using QuanLyLichHoc.Services;

namespace QuanLyLichHoc.Controllers
{
    // ===================== Phòng học =====================
    public class RoomsController : CrudController<PhongHoc>
    {
        public RoomsController(AppDbContext db, AuditService a) : base(db, a) { }
        protected override string Title => "Quản lý phòng học";
        protected override string FilterPartial => "_Filter";
        protected override string KeyOf(PhongHoc m) => m.MaPhong.ToString();
        protected override Task<PhongHoc> FindAsync(string id) => Db.PhongHocs.FindAsync(I(id)).AsTask();
        protected override string Describe(PhongHoc m) => "Phòng " + m.TenPhong;

        protected override IQueryable<PhongHoc> Items(string q, IQueryCollection f)
        {
            var x = Db.PhongHocs.AsNoTracking();
            if (!string.IsNullOrWhiteSpace(q)) x = x.Where(p => p.TenPhong.Contains(q) || p.ToaNha.Contains(q));
            string loai = f["loai"].ToString();
            if (!string.IsNullOrEmpty(loai)) x = x.Where(p => p.LoaiPhong == loai);
            return x.OrderBy(p => p.TenPhong);
        }

        protected override async Task<List<string>> ValidateAsync(PhongHoc m, bool isNew)
        {
            var e = new List<string>();
            if (m.LoaiPhong != "LT" && m.LoaiPhong != "TH") e.Add("Loại phòng phải là LT hoặc TH.");
            if (await Db.PhongHocs.AnyAsync(p => p.TenPhong == m.TenPhong && p.MaPhong != m.MaPhong)) e.Add("Tên phòng đã tồn tại.");
            return e;
        }

        protected override async Task<string> CanDeleteAsync(PhongHoc p) =>
            await Db.LichHocs.AnyAsync(l => l.MaPhong == p.MaPhong) ? "Phòng đang có lịch học, hãy xóa/đổi lịch trước." : null;
    }

    // ===================== Môn học =====================
    public class SubjectsController : CrudController<MonHoc>
    {
        public SubjectsController(AppDbContext db, AuditService a) : base(db, a) { }
        protected override string Title => "Quản lý môn học";
        protected override string KeyOf(MonHoc m) => m.MaMH;
        protected override Task<MonHoc> FindAsync(string id) => Db.MonHocs.FindAsync(id).AsTask();
        protected override string Describe(MonHoc m) => "Môn " + m.MaMH + " - " + m.TenMH;

        protected override IQueryable<MonHoc> Items(string q, IQueryCollection f)
        {
            var x = Db.MonHocs.AsNoTracking().Include(m => m.Khoa).AsQueryable();
            if (!string.IsNullOrWhiteSpace(q)) x = x.Where(m => m.MaMH.Contains(q) || m.TenMH.Contains(q));
            return x.OrderBy(m => m.MaMH);
        }

        protected override async Task PrepareFormAsync(MonHoc m, bool isNew) =>
            ViewBag.Khoas = Sel(await Db.Khoas.OrderBy(k => k.TenKhoa).ToListAsync(), "MaKhoa", "TenKhoa", m.MaKhoa);

        protected override async Task<List<string>> ValidateAsync(MonHoc m, bool isNew)
        {
            var e = new List<string>();
            if (isNew && await Db.MonHocs.AnyAsync(x => x.MaMH == m.MaMH)) e.Add("Mã môn học đã tồn tại.");
            if (!await Db.Khoas.AnyAsync(k => k.MaKhoa == m.MaKhoa)) e.Add("Vui lòng chọn khoa.");
            return e;
        }
    }

    // ===================== Học kỳ =====================
    public class SemestersController : CrudController<HocKy>
    {
        public SemestersController(AppDbContext db, AuditService a) : base(db, a) { }
        protected override string Title => "Quản lý học kỳ";
        protected override string KeyOf(HocKy m) => m.MaHK.ToString();
        protected override Task<HocKy> FindAsync(string id) => Db.HocKys.FindAsync(I(id)).AsTask();
        protected override string Describe(HocKy m) => "Học kỳ " + m.TenHK + " " + m.NamHoc;

        protected override IQueryable<HocKy> Items(string q, IQueryCollection f)
        {
            var x = Db.HocKys.AsNoTracking();
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
            await Db.LopHocPhans.AnyAsync(l => l.MaHK == h.MaHK) ? "Học kỳ đã có lớp học phần, không thể xóa." : null;
    }

    // ===================== Khoa =====================
    public class FacultiesController : CrudController<Khoa>
    {
        public FacultiesController(AppDbContext db, AuditService a) : base(db, a) { }
        protected override string Title => "Quản lý khoa";
        protected override string KeyOf(Khoa m) => m.MaKhoa.ToString();
        protected override Task<Khoa> FindAsync(string id) => Db.Khoas.FindAsync(I(id)).AsTask();
        protected override string Describe(Khoa m) => "Khoa " + m.TenKhoa;

        protected override IQueryable<Khoa> Items(string q, IQueryCollection f)
        {
            var x = Db.Khoas.AsNoTracking();
            if (!string.IsNullOrWhiteSpace(q)) x = x.Where(k => k.TenKhoa.Contains(q));
            return x.OrderBy(k => k.TenKhoa);
        }

        protected override async Task<List<string>> ValidateAsync(Khoa m, bool isNew)
        {
            var e = new List<string>();
            if (await Db.Khoas.AnyAsync(k => k.TenKhoa == m.TenKhoa && k.MaKhoa != m.MaKhoa)) e.Add("Tên khoa đã tồn tại.");
            return e;
        }
    }

    // ===================== Lớp (hành chính) =====================
    public class ClassesController : CrudController<Lop>
    {
        public ClassesController(AppDbContext db, AuditService a) : base(db, a) { }
        protected override string Title => "Quản lý lớp";
        protected override string KeyOf(Lop m) => m.MaLop.ToString();
        protected override Task<Lop> FindAsync(string id) => Db.Lops.FindAsync(I(id)).AsTask();
        protected override string Describe(Lop m) => "Lớp " + m.TenLop;

        protected override IQueryable<Lop> Items(string q, IQueryCollection f)
        {
            var x = Db.Lops.AsNoTracking().Include(l => l.Khoa).AsQueryable();
            if (!string.IsNullOrWhiteSpace(q)) x = x.Where(l => l.TenLop.Contains(q));
            return x.OrderBy(l => l.TenLop);
        }

        protected override async Task PrepareFormAsync(Lop m, bool isNew) =>
            ViewBag.Khoas = Sel(await Db.Khoas.OrderBy(k => k.TenKhoa).ToListAsync(), "MaKhoa", "TenKhoa", m.MaKhoa);

        protected override async Task<List<string>> ValidateAsync(Lop m, bool isNew)
        {
            var e = new List<string>();
            if (!await Db.Khoas.AnyAsync(k => k.MaKhoa == m.MaKhoa)) e.Add("Vui lòng chọn khoa.");
            return e;
        }
    }

    // ===================== Giảng viên =====================
    public class LecturersController : CrudController<GiangVien>
    {
        public LecturersController(AppDbContext db, AuditService a) : base(db, a) { }
        protected override string Title => "Quản lý giảng viên";
        protected override string KeyOf(GiangVien m) => m.MaGV;
        protected override Task<GiangVien> FindAsync(string id) => Db.GiangViens.FindAsync(id).AsTask();
        protected override string Describe(GiangVien m) => "Giảng viên " + m.MaGV + " - " + m.HoTen;

        protected override IQueryable<GiangVien> Items(string q, IQueryCollection f)
        {
            var x = Db.GiangViens.AsNoTracking().Include(g => g.Khoa).AsQueryable();
            if (!string.IsNullOrWhiteSpace(q)) x = x.Where(g => g.MaGV.Contains(q) || g.HoTen.Contains(q) || g.Email.Contains(q));
            return x.OrderBy(g => g.MaGV);
        }

        protected override async Task PrepareFormAsync(GiangVien m, bool isNew) =>
            ViewBag.Khoas = Sel(await Db.Khoas.OrderBy(k => k.TenKhoa).ToListAsync(), "MaKhoa", "TenKhoa", m.MaKhoa);

        protected override void CopyValues(GiangVien existing, GiangVien model)
        {
            var nd = existing.MaND;
            base.CopyValues(existing, model);
            existing.MaND = nd;
        }

        protected override async Task<List<string>> ValidateAsync(GiangVien m, bool isNew)
        {
            var e = new List<string>();
            if (isNew && await Db.GiangViens.AnyAsync(x => x.MaGV == m.MaGV)) e.Add("Mã giảng viên đã tồn tại.");
            if (await Db.GiangViens.AnyAsync(x => x.Email == m.Email && x.MaGV != m.MaGV)) e.Add("Email đã được sử dụng.");
            if (!await Db.Khoas.AnyAsync(k => k.MaKhoa == m.MaKhoa)) e.Add("Vui lòng chọn khoa.");
            return e;
        }

        protected override async Task AfterCreateAsync(GiangVien m)
        {
            // Tự tạo tài khoản đăng nhập: tên = mã GV (chữ thường), mật khẩu mặc định GiangVien@123
            var user = new NguoiDung { TenDangNhap = m.MaGV.ToLower(), HoTen = m.HoTen, VaiTro = VaiTro.GiangVien, MatKhauHash = PasswordHasher.Hash("GiangVien@123") };
            if (!await Db.NguoiDungs.AnyAsync(u => u.TenDangNhap == user.TenDangNhap))
            {
                Db.NguoiDungs.Add(user);
                await Db.SaveChangesAsync();
                m.MaND = user.MaND;
                await Db.SaveChangesAsync();
            }
        }

        protected override async Task<string> CanDeleteAsync(GiangVien g) =>
            await Db.LopHocPhans.AnyAsync(l => l.MaGV == g.MaGV) ? "Giảng viên đang phụ trách lớp học phần, không thể xóa." : null;
    }

    // ===================== Sinh viên =====================
    public class StudentsController : CrudController<SinhVien>
    {
        public StudentsController(AppDbContext db, AuditService a) : base(db, a) { }
        protected override string Title => "Quản lý sinh viên";
        protected override string FilterPartial => "_Filter";
        protected override string KeyOf(SinhVien m) => m.MaSV;
        protected override Task<SinhVien> FindAsync(string id) => Db.SinhViens.FindAsync(id).AsTask();
        protected override string Describe(SinhVien m) => "Sinh viên " + m.MaSV + " - " + m.HoTen;

        protected override async Task PrepareIndexAsync() =>
            ViewBag.Lops = Sel(await Db.Lops.OrderBy(l => l.TenLop).ToListAsync(), "MaLop", "TenLop");

        protected override IQueryable<SinhVien> Items(string q, IQueryCollection f)
        {
            var x = Db.SinhViens.AsNoTracking().Include(s => s.Lop).AsQueryable();
            if (!string.IsNullOrWhiteSpace(q)) x = x.Where(s => s.MaSV.Contains(q) || s.HoTen.Contains(q) || s.Email.Contains(q));
            int lop = I(f["maLop"].ToString());
            if (lop > 0) x = x.Where(s => s.MaLop == lop);
            return x.OrderBy(s => s.MaSV);
        }

        protected override async Task PrepareFormAsync(SinhVien m, bool isNew) =>
            ViewBag.Lops = Sel(await Db.Lops.OrderBy(l => l.TenLop).ToListAsync(), "MaLop", "TenLop", m.MaLop);

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
            if (isNew && await Db.SinhViens.AnyAsync(x => x.MaSV == m.MaSV)) e.Add("Mã sinh viên đã tồn tại.");
            if (await Db.SinhViens.AnyAsync(x => x.Email == m.Email && x.MaSV != m.MaSV)) e.Add("Email đã được sử dụng.");
            if (!await Db.Lops.AnyAsync(l => l.MaLop == m.MaLop)) e.Add("Vui lòng chọn lớp.");
            return e;
        }

        protected override async Task AfterCreateAsync(SinhVien m)
        {
            var user = new NguoiDung { TenDangNhap = m.MaSV.ToLower(), HoTen = m.HoTen, VaiTro = VaiTro.SinhVien, MatKhauHash = PasswordHasher.Hash("SinhVien@123") };
            if (!await Db.NguoiDungs.AnyAsync(u => u.TenDangNhap == user.TenDangNhap))
            {
                Db.NguoiDungs.Add(user);
                await Db.SaveChangesAsync();
                m.MaND = user.MaND;
                await Db.SaveChangesAsync();
            }
        }

        protected override async Task<string> CanDeleteAsync(SinhVien s) =>
            await Db.DangKys.AnyAsync(d => d.MaSV == s.MaSV) ? "Sinh viên đã đăng ký học phần, không thể xóa." : null;
    }

    // ===================== Lớp học phần =====================
    public class SectionsController : CrudController<LopHocPhan>
    {
        public SectionsController(AppDbContext db, AuditService a) : base(db, a) { }
        protected override string Title => "Quản lý lớp học phần";
        protected override string FilterPartial => "_Filter";
        protected override string KeyOf(LopHocPhan m) => m.MaLHP;
        protected override Task<LopHocPhan> FindAsync(string id) => Db.LopHocPhans.FindAsync(id).AsTask();
        protected override string Describe(LopHocPhan m) => "Lớp học phần " + m.MaLHP;

        protected override async Task PrepareIndexAsync() =>
            ViewBag.HocKys = Sel(await Db.HocKys.OrderByDescending(h => h.NgayBatDau).ToListAsync(), "MaHK", "Ten");

        protected override IQueryable<LopHocPhan> Items(string q, IQueryCollection f)
        {
            var x = Db.LopHocPhans.AsNoTracking().Include(l => l.MonHoc).Include(l => l.GiangVien).Include(l => l.HocKy)
                .Include(l => l.DangKys).AsQueryable();
            if (!string.IsNullOrWhiteSpace(q)) x = x.Where(l => l.MaLHP.Contains(q) || l.MonHoc.TenMH.Contains(q) || l.GiangVien.HoTen.Contains(q));
            int hk = I(f["maHK"].ToString());
            if (hk > 0) x = x.Where(l => l.MaHK == hk);
            return x.OrderBy(l => l.MaLHP);
        }

        protected override async Task PrepareFormAsync(LopHocPhan m, bool isNew)
        {
            ViewBag.Mons = Sel((await Db.MonHocs.OrderBy(x => x.MaMH).ToListAsync()).Select(x => new { x.MaMH, Ten = x.MaMH + " - " + x.TenMH }), "MaMH", "Ten", m.MaMH);
            ViewBag.GVs = Sel((await Db.GiangViens.OrderBy(x => x.MaGV).ToListAsync()).Select(x => new { x.MaGV, Ten = x.MaGV + " - " + x.HoTen }), "MaGV", "Ten", m.MaGV);
            ViewBag.HocKys = Sel(await Db.HocKys.OrderByDescending(h => h.NgayBatDau).ToListAsync(), "MaHK", "Ten", m.MaHK);
            ViewBag.TrangThais = Sel(TrangThaiLHP.All.Select(t => new { Id = t, Ten = t }), "Id", "Ten", m.TrangThai);
        }

        protected override async Task<List<string>> ValidateAsync(LopHocPhan m, bool isNew)
        {
            var e = new List<string>();
            if (isNew && await Db.LopHocPhans.AnyAsync(x => x.MaLHP == m.MaLHP)) e.Add("Mã lớp học phần đã tồn tại.");
            if (!await Db.MonHocs.AnyAsync(x => x.MaMH == m.MaMH)) e.Add("Môn học không tồn tại.");
            if (!await Db.GiangViens.AnyAsync(x => x.MaGV == m.MaGV)) e.Add("Giảng viên không tồn tại.");
            if (!await Db.HocKys.AnyAsync(x => x.MaHK == m.MaHK)) e.Add("Học kỳ không tồn tại.");
            if (!TrangThaiLHP.All.Contains(m.TrangThai)) e.Add("Trạng thái không hợp lệ.");
            if (!isNew)
            {
                int daDk = await Db.DangKys.CountAsync(d => d.MaLHP == m.MaLHP);
                if (m.SiSoToiDa < daDk) e.Add($"Sĩ số tối đa không được nhỏ hơn số sinh viên đã đăng ký ({daDk}).");
                var nho = await Db.LichHocs.Where(l => l.MaLHP == m.MaLHP && l.PhongHoc.SucChua < m.SiSoToiDa).Select(l => l.PhongHoc.TenPhong).ToListAsync();
                if (nho.Count > 0) e.Add("Sĩ số lớn hơn sức chứa của phòng: " + string.Join(", ", nho.Distinct()) + ".");
            }
            return e;
        }

        protected override async Task<string> CanDeleteAsync(LopHocPhan l)
        {
            if (await Db.DangKys.AnyAsync(d => d.MaLHP == l.MaLHP)) return "Lớp đã có sinh viên đăng ký, hãy chuyển trạng thái sang Hủy thay vì xóa.";
            if (await Db.LichHocs.AnyAsync(x => x.MaLHP == l.MaLHP)) return "Lớp đã có lịch học, hãy xóa lịch học trước.";
            return null;
        }
    }

    // ===================== Lịch học (xếp lịch) =====================
    public class SchedulesController : CrudController<LichHoc>
    {
        private readonly ScheduleService schedule;
        public SchedulesController(AppDbContext db, AuditService a, ScheduleService s) : base(db, a) { schedule = s; }
        protected override string Title => "Xếp lịch học";
        protected override string FilterPartial => "_Filter";
        protected override string KeyOf(LichHoc m) => m.MaLich.ToString();
        protected override Task<LichHoc> FindAsync(string id) => Db.LichHocs.FindAsync(I(id)).AsTask();
        protected override string Describe(LichHoc m) => "Lịch học " + TkbHelper.Mo(m);

        protected override async Task PrepareIndexAsync()
        {
            ViewBag.HocKys = Sel(await Db.HocKys.OrderByDescending(h => h.NgayBatDau).ToListAsync(), "MaHK", "Ten");
            ViewBag.GVs = Sel((await Db.GiangViens.OrderBy(x => x.MaGV).ToListAsync()).Select(x => new { x.MaGV, Ten = x.MaGV + " - " + x.HoTen }), "MaGV", "Ten");
            ViewBag.Phongs = Sel(await Db.PhongHocs.OrderBy(p => p.TenPhong).ToListAsync(), "MaPhong", "TenPhong");
        }

        protected override IQueryable<LichHoc> Items(string q, IQueryCollection f)
        {
            var x = schedule.Query();
            if (!string.IsNullOrWhiteSpace(q))
                x = x.Where(l => l.MaLHP.Contains(q) || l.LopHocPhan.MonHoc.TenMH.Contains(q) || l.PhongHoc.TenPhong.Contains(q));
            int hk = I(f["maHK"].ToString()), phong = I(f["maPhong"].ToString()), thu = I(f["thu"].ToString());
            string gv = f["maGV"].ToString();
            if (hk > 0) x = x.Where(l => l.LopHocPhan.MaHK == hk);
            if (phong > 0) x = x.Where(l => l.MaPhong == phong);
            if (thu > 0) x = x.Where(l => l.Thu == thu);
            if (!string.IsNullOrEmpty(gv)) x = x.Where(l => l.LopHocPhan.MaGV == gv);
            return x.OrderBy(l => l.LopHocPhan.MaHK).ThenBy(l => l.Thu).ThenBy(l => l.TietBatDau);
        }

        protected override async Task PrepareFormAsync(LichHoc m, bool isNew)
        {
            var lhps = await Db.LopHocPhans.Include(l => l.MonHoc).Include(l => l.GiangVien).OrderBy(l => l.MaLHP).ToListAsync();
            ViewBag.LHPs = Sel(lhps.Select(l => new { l.MaLHP, Ten = $"{l.MaLHP} - {l.MonHoc.TenMH} ({l.GiangVien.HoTen})" }), "MaLHP", "Ten", m.MaLHP);
            ViewBag.Phongs = Sel((await Db.PhongHocs.OrderBy(p => p.TenPhong).ToListAsync()).Select(p => new { p.MaPhong, Ten = $"{p.TenPhong} ({p.LoaiPhong}, {p.SucChua} chỗ)" }), "MaPhong", "Ten", m.MaPhong);
            ViewBag.Thus = Sel(Enumerable.Range(2, 7).Select(t => new { Id = t, Ten = TkbHelper.ThuText(t) }), "Id", "Ten", m.Thu);
        }

        protected override Task<List<string>> ValidateAsync(LichHoc m, bool isNew) => schedule.KiemTraAsync(m);

        /// <summary>AJAX: kiểm tra xung đột ngay khi người dùng đổi phòng / thứ / tiết / tuần.</summary>
        [HttpPost]
        public async Task<IActionResult> Check(LichHoc model)
        {
            var errors = new List<string>();
            if (!ModelState.IsValid) errors.AddRange(ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).Where(s => !string.IsNullOrEmpty(s)));
            else errors.AddRange(await schedule.KiemTraAsync(model));
            return Json(new { ok = errors.Count == 0, errors });
        }
    }

    // ===================== Tài khoản & phân quyền (chỉ Admin) =====================
    [Authorize(Roles = "Admin")]
    public class UsersController : CrudController<NguoiDung>
    {
        public UsersController(AppDbContext db, AuditService a) : base(db, a) { }
        protected override string Title => "Quản lý tài khoản";
        protected override string FilterPartial => "_Filter";
        protected override string KeyOf(NguoiDung m) => m.MaND.ToString();
        protected override Task<NguoiDung> FindAsync(string id) => Db.NguoiDungs.FindAsync(I(id)).AsTask();
        protected override string Describe(NguoiDung m) => "Tài khoản " + m.TenDangNhap + " (" + m.VaiTro + ")";

        protected override IQueryable<NguoiDung> Items(string q, IQueryCollection f)
        {
            var x = Db.NguoiDungs.AsNoTracking().AsQueryable();
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
            if (await Db.NguoiDungs.AnyAsync(u => u.TenDangNhap == m.TenDangNhap && u.MaND != m.MaND)) e.Add("Tên đăng nhập đã tồn tại.");
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
