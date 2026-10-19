using QuanLyLichHoc.Models;
using QuanLyLichHoc.Services;

namespace QuanLyLichHoc.Data
{
    public static class DbSeeder
    {
        public static void Seed(AppDbContext db)
        {
            if (db.NguoiDungs.Any()) return;

            // Không gán ID thủ công cho cột identity (SQL Server không cho phép) - dùng thuộc tính điều hướng
            var k1 = new Khoa { TenKhoa = "Công nghệ thông tin" };
            var k2 = new Khoa { TenKhoa = "Toán - Tin" };
            var lops = new[]
            {
                new Lop { TenLop = "CNTT1-K66", Khoa = k1, KhoaHoc = 66 },
                new Lop { TenLop = "CNTT2-K66", Khoa = k1, KhoaHoc = 66 },
                new Lop { TenLop = "TOAN1-K66", Khoa = k2, KhoaHoc = 66 }
            };
            var hk1 = new HocKy { TenHK = "Học kỳ 1", NamHoc = "2026-2027", NgayBatDau = new DateTime(2026, 9, 7), NgayKetThuc = new DateTime(2027, 1, 24) };
            var hk2 = new HocKy { TenHK = "Học kỳ 2", NamHoc = "2026-2027", NgayBatDau = new DateTime(2027, 2, 15), NgayKetThuc = new DateTime(2027, 6, 13) };
            PhongHoc P(string ten, string loai, int sc, string toa) => new PhongHoc { TenPhong = ten, LoaiPhong = loai, SucChua = sc, ToaNha = toa };
            var rooms = new[] { P("P.101", "LT", 60, "A1"), P("P.102", "LT", 70, "A1"), P("P.201", "LT", 100, "A2"), P("P.202", "LT", 100, "A2"),
                                P("P.301", "LT", 60, "A3"), P("P.302", "LT", 40, "A3"), P("LAB1", "TH", 40, "B1"), P("LAB2", "TH", 40, "B1") };
            MonHoc M(string ma, string ten, int tc, int tiet, Khoa k, bool th = false) => new MonHoc { MaMH = ma, TenMH = ten, SoTinChi = tc, SoTiet = tiet, ThucHanh = th, Khoa = k };
            db.AddRange(k1, k2, hk1, hk2);
            db.AddRange(lops);
            db.AddRange(rooms);
            db.MonHocs.AddRange(M("INT3306", "Lập trình Web", 3, 45, k1), M("INT3307", "Thực hành Lập trình Web", 1, 30, k1, true),
                M("INT2211", "Cơ sở dữ liệu", 3, 45, k1), M("INT2213", "Mạng máy tính", 3, 45, k1), M("INT1306", "Cấu trúc dữ liệu và giải thuật", 3, 45, k1),
                M("MAT1100", "Toán rời rạc", 3, 45, k2), M("MAT1093", "Đại số tuyến tính", 3, 45, k2), M("MAT1101", "Giải tích", 3, 45, k2));
            db.SaveChanges();

            // Tài khoản
            var users = new List<NguoiDung>
            {
                new NguoiDung { TenDangNhap = "admin", HoTen = "Quản trị hệ thống", VaiTro = VaiTro.Admin, MatKhauHash = PasswordHasher.Hash("Admin@123") },
                new NguoiDung { TenDangNhap = "giaovu", HoTen = "Phòng Đào tạo", VaiTro = VaiTro.GiaoVu, MatKhauHash = PasswordHasher.Hash("GiaoVu@123") }
            };
            var gvNames = new[] { "Nguyễn Văn An", "Trần Thị Bình", "Lê Minh Châu", "Phạm Quốc Dũng", "Hoàng Thu Hà" };
            var gvs = new List<GiangVien>();
            for (int i = 0; i < gvNames.Length; i++)
            {
                var nd = new NguoiDung { TenDangNhap = $"gv00{i + 1}", HoTen = gvNames[i], VaiTro = VaiTro.GiangVien, MatKhauHash = PasswordHasher.Hash("GiangVien@123") };
                users.Add(nd);
                gvs.Add(new GiangVien { MaGV = $"GV00{i + 1}", HoTen = gvNames[i], Email = $"gv00{i + 1}@example.edu.vn", SoDienThoai = $"09010000{i + 1}", Khoa = i == 3 ? k2 : k1, NguoiDung = nd });
            }
            var svNames = new[] { "Nguyễn Hoàng Nam", "Trần Minh Anh", "Lê Thu Trang", "Phạm Đức Huy", "Vũ Thanh Hương", "Đặng Quang Vinh", "Bùi Ngọc Lan", "Đỗ Gia Bảo", "Hồ Thị Mai", "Ngô Văn Tuấn",
                                       "Dương Khánh Linh", "Lý Hải Đăng", "Phan Thảo Nguyên", "Trịnh Công Thành", "Mai Phương Thảo", "Cao Xuân Bách", "Đinh Thùy Dung", "Lương Quốc Khánh", "Tạ Bảo Ngọc", "Chu Minh Quân" };
            var svs = new List<SinhVien>();
            for (int i = 0; i < svNames.Length; i++)
            {
                string ma = $"SV{i + 1:000}";
                var nd = new NguoiDung { TenDangNhap = ma.ToLower(), HoTen = svNames[i], VaiTro = VaiTro.SinhVien, MatKhauHash = PasswordHasher.Hash("SinhVien@123") };
                users.Add(nd);
                svs.Add(new SinhVien { MaSV = ma, HoTen = svNames[i], NgaySinh = new DateTime(2004, 1 + i % 12, 5 + i % 20), Email = ma.ToLower() + "@example.edu.vn", Lop = lops[i < 7 ? 0 : i < 14 ? 1 : 2], NguoiDung = nd });
            }
            db.NguoiDungs.AddRange(users);
            db.GiangViens.AddRange(gvs);
            db.SinhViens.AddRange(svs);
            db.SaveChanges();

            LopHocPhan L(string ma, string mh, string gv, int ss, string tt = TrangThaiLHP.Mo) => new LopHocPhan { MaLHP = ma, MaMH = mh, MaGV = gv, HocKy = hk1, SiSoToiDa = ss, TrangThai = tt };
            db.LopHocPhans.AddRange(L("LHP01", "INT3306", "GV001", 40), L("LHP02", "INT3307", "GV001", 30), L("LHP03", "INT2211", "GV002", 40),
                L("LHP04", "INT2213", "GV003", 40), L("LHP05", "INT1306", "GV005", 40), L("LHP06", "MAT1100", "GV004", 50),
                L("LHP07", "MAT1093", "GV004", 50), L("LHP08", "MAT1101", "GV004", 50, TrangThaiLHP.Dong));
            db.SaveChanges();

            LichHoc S(string lhp, PhongHoc phong, int thu, int tiet, int so = 3) => new LichHoc { MaLHP = lhp, PhongHoc = phong, Thu = thu, TietBatDau = tiet, SoTiet = so, TuanBatDau = 1, TuanKetThuc = 15 };
            db.LichHocs.AddRange(S("LHP01", rooms[4], 2, 1), S("LHP02", rooms[6], 3, 1), S("LHP03", rooms[1], 2, 4), S("LHP04", rooms[2], 3, 4),
                S("LHP05", rooms[0], 4, 1), S("LHP06", rooms[2], 5, 1), S("LHP07", rooms[2], 5, 4), S("LHP08", rooms[3], 6, 1));
            db.LichBans.Add(new LichBan { MaGV = "GV002", Thu = 6, TietBatDau = 7, SoTiet = 6, GhiChu = "Họp khoa chiều thứ 6" });

            foreach (var sv in svs)
            {
                int n = int.Parse(sv.MaSV.Substring(2));
                if (n <= 10) foreach (var l in new[] { "LHP01", "LHP03", "LHP04" }) db.DangKys.Add(new DangKyHocPhan { MaSV = sv.MaSV, MaLHP = l });
                else foreach (var l in new[] { "LHP03", "LHP05", "LHP06" }) db.DangKys.Add(new DangKyHocPhan { MaSV = sv.MaSV, MaLHP = l });
                if (n <= 5) db.DangKys.Add(new DangKyHocPhan { MaSV = sv.MaSV, MaLHP = "LHP02" });
            }
            db.SaveChanges();
        }
    }
}
