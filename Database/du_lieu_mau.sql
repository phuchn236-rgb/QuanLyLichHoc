
USE QuanLyLichHoc;
GO
SET NOCOUNT ON;
SET XACT_ABORT ON;

IF EXISTS (SELECT 1 FROM dbo.NguoiDungs)
    PRINT N'Cơ sở dữ liệu đã có dữ liệu - bỏ qua. Chạy xoa_csdl.sql rồi tao_csdl.sql nếu muốn nạp lại.';
ELSE
BEGIN

BEGIN TRANSACTION;

SET IDENTITY_INSERT dbo.Khoas ON;
INSERT INTO dbo.Khoas (MaKhoa, TenKhoa) VALUES (1, N'Công nghệ thông tin'), (2, N'Toán - Tin');
SET IDENTITY_INSERT dbo.Khoas OFF;

SET IDENTITY_INSERT dbo.Lops ON;
INSERT INTO dbo.Lops (MaLop, TenLop, MaKhoa, KhoaHoc) VALUES (1, N'CNTT1-K66', 1, 66), (2, N'CNTT2-K66', 1, 66), (3, N'TOAN1-K66', 2, 66);
SET IDENTITY_INSERT dbo.Lops OFF;

SET IDENTITY_INSERT dbo.HocKys ON;
INSERT INTO dbo.HocKys (MaHK, TenHK, NamHoc, NgayBatDau, NgayKetThuc) VALUES
  (1, N'Học kỳ 1', N'2026-2027', '2026-09-07', '2027-01-24'),
  (2, N'Học kỳ 2', N'2026-2027', '2027-02-15', '2027-06-13');
SET IDENTITY_INSERT dbo.HocKys OFF;

SET IDENTITY_INSERT dbo.PhongHocs ON;
INSERT INTO dbo.PhongHocs (MaPhong, TenPhong, LoaiPhong, SucChua, ToaNha) VALUES
  (1, N'P.101', N'LT', 60, N'A1'),
  (2, N'P.102', N'LT', 70, N'A1'),
  (3, N'P.201', N'LT', 100, N'A2'),
  (4, N'P.202', N'LT', 100, N'A2'),
  (5, N'P.301', N'LT', 60, N'A3'),
  (6, N'P.302', N'LT', 40, N'A3'),
  (7, N'LAB1', N'TH', 40, N'B1'),
  (8, N'LAB2', N'TH', 40, N'B1');
SET IDENTITY_INSERT dbo.PhongHocs OFF;

INSERT INTO dbo.MonHocs (MaMH, TenMH, SoTinChi, SoTiet, ThucHanh, MaKhoa) VALUES
  (N'INT3306', N'Lập trình Web', 3, 45, 0, 1),
  (N'INT3307', N'Thực hành Lập trình Web', 1, 30, 1, 1),
  (N'INT2211', N'Cơ sở dữ liệu', 3, 45, 0, 1),
  (N'INT2213', N'Mạng máy tính', 3, 45, 0, 1),
  (N'INT1306', N'Cấu trúc dữ liệu và giải thuật', 3, 45, 0, 1),
  (N'MAT1100', N'Toán rời rạc', 3, 45, 0, 2),
  (N'MAT1093', N'Đại số tuyến tính', 3, 45, 0, 2),
  (N'MAT1101', N'Giải tích', 3, 45, 0, 2);

SET IDENTITY_INSERT dbo.NguoiDungs ON;
INSERT INTO dbo.NguoiDungs (MaND, TenDangNhap, HoTen, MatKhauHash, VaiTro, TrangThai, SoLanSai) VALUES
  (1, N'admin', N'Quản trị hệ thống', N'v1.100000.adGdDASJ958czz720o4Bww==.DIgtRjnjpi/XHISzG5RvGLx42ZK0bPNvxKIUlmt6lIg=', N'Admin', 1, 0),
  (2, N'giaovu', N'Phòng Đào tạo', N'v1.100000.TkmjInu10ThIXBXY8XCNyQ==.DiJzxhFDdTzk2eWatXhQ+eBxno+FTyYPU9mbcWILjHA=', N'GiaoVu', 1, 0),
  (3, N'gv001', N'Nguyễn Văn An', N'v1.100000.Urj/WGz5dZyrz9rEREd7+w==.j/382zPZ4qDWB8jFFMAAJIa0HRD5KzQVOJah5KMBbu8=', N'GiangVien', 1, 0),
  (4, N'gv002', N'Trần Thị Bình', N'v1.100000.Urj/WGz5dZyrz9rEREd7+w==.j/382zPZ4qDWB8jFFMAAJIa0HRD5KzQVOJah5KMBbu8=', N'GiangVien', 1, 0),
  (5, N'gv003', N'Lê Minh Châu', N'v1.100000.Urj/WGz5dZyrz9rEREd7+w==.j/382zPZ4qDWB8jFFMAAJIa0HRD5KzQVOJah5KMBbu8=', N'GiangVien', 1, 0),
  (6, N'gv004', N'Phạm Quốc Dũng', N'v1.100000.Urj/WGz5dZyrz9rEREd7+w==.j/382zPZ4qDWB8jFFMAAJIa0HRD5KzQVOJah5KMBbu8=', N'GiangVien', 1, 0),
  (7, N'gv005', N'Hoàng Thu Hà', N'v1.100000.Urj/WGz5dZyrz9rEREd7+w==.j/382zPZ4qDWB8jFFMAAJIa0HRD5KzQVOJah5KMBbu8=', N'GiangVien', 1, 0),
  (8, N'sv001', N'Nguyễn Hoàng Nam', N'v1.100000.zUXqxZTJKCLhr7ZFFHPhag==.E7szacGYPnkPY1b2kq6Dj09BNw/0O/GZXJSWTZTp52A=', N'SinhVien', 1, 0),
  (9, N'sv002', N'Trần Minh Anh', N'v1.100000.zUXqxZTJKCLhr7ZFFHPhag==.E7szacGYPnkPY1b2kq6Dj09BNw/0O/GZXJSWTZTp52A=', N'SinhVien', 1, 0),
  (10, N'sv003', N'Lê Thu Trang', N'v1.100000.zUXqxZTJKCLhr7ZFFHPhag==.E7szacGYPnkPY1b2kq6Dj09BNw/0O/GZXJSWTZTp52A=', N'SinhVien', 1, 0),
  (11, N'sv004', N'Phạm Đức Huy', N'v1.100000.zUXqxZTJKCLhr7ZFFHPhag==.E7szacGYPnkPY1b2kq6Dj09BNw/0O/GZXJSWTZTp52A=', N'SinhVien', 1, 0),
  (12, N'sv005', N'Vũ Thanh Hương', N'v1.100000.zUXqxZTJKCLhr7ZFFHPhag==.E7szacGYPnkPY1b2kq6Dj09BNw/0O/GZXJSWTZTp52A=', N'SinhVien', 1, 0),
  (13, N'sv006', N'Đặng Quang Vinh', N'v1.100000.zUXqxZTJKCLhr7ZFFHPhag==.E7szacGYPnkPY1b2kq6Dj09BNw/0O/GZXJSWTZTp52A=', N'SinhVien', 1, 0),
  (14, N'sv007', N'Bùi Ngọc Lan', N'v1.100000.zUXqxZTJKCLhr7ZFFHPhag==.E7szacGYPnkPY1b2kq6Dj09BNw/0O/GZXJSWTZTp52A=', N'SinhVien', 1, 0),
  (15, N'sv008', N'Đỗ Gia Bảo', N'v1.100000.zUXqxZTJKCLhr7ZFFHPhag==.E7szacGYPnkPY1b2kq6Dj09BNw/0O/GZXJSWTZTp52A=', N'SinhVien', 1, 0),
  (16, N'sv009', N'Hồ Thị Mai', N'v1.100000.zUXqxZTJKCLhr7ZFFHPhag==.E7szacGYPnkPY1b2kq6Dj09BNw/0O/GZXJSWTZTp52A=', N'SinhVien', 1, 0),
  (17, N'sv010', N'Ngô Văn Tuấn', N'v1.100000.zUXqxZTJKCLhr7ZFFHPhag==.E7szacGYPnkPY1b2kq6Dj09BNw/0O/GZXJSWTZTp52A=', N'SinhVien', 1, 0),
  (18, N'sv011', N'Dương Khánh Linh', N'v1.100000.zUXqxZTJKCLhr7ZFFHPhag==.E7szacGYPnkPY1b2kq6Dj09BNw/0O/GZXJSWTZTp52A=', N'SinhVien', 1, 0),
  (19, N'sv012', N'Lý Hải Đăng', N'v1.100000.zUXqxZTJKCLhr7ZFFHPhag==.E7szacGYPnkPY1b2kq6Dj09BNw/0O/GZXJSWTZTp52A=', N'SinhVien', 1, 0),
  (20, N'sv013', N'Phan Thảo Nguyên', N'v1.100000.zUXqxZTJKCLhr7ZFFHPhag==.E7szacGYPnkPY1b2kq6Dj09BNw/0O/GZXJSWTZTp52A=', N'SinhVien', 1, 0),
  (21, N'sv014', N'Trịnh Công Thành', N'v1.100000.zUXqxZTJKCLhr7ZFFHPhag==.E7szacGYPnkPY1b2kq6Dj09BNw/0O/GZXJSWTZTp52A=', N'SinhVien', 1, 0),
  (22, N'sv015', N'Mai Phương Thảo', N'v1.100000.zUXqxZTJKCLhr7ZFFHPhag==.E7szacGYPnkPY1b2kq6Dj09BNw/0O/GZXJSWTZTp52A=', N'SinhVien', 1, 0),
  (23, N'sv016', N'Cao Xuân Bách', N'v1.100000.zUXqxZTJKCLhr7ZFFHPhag==.E7szacGYPnkPY1b2kq6Dj09BNw/0O/GZXJSWTZTp52A=', N'SinhVien', 1, 0),
  (24, N'sv017', N'Đinh Thùy Dung', N'v1.100000.zUXqxZTJKCLhr7ZFFHPhag==.E7szacGYPnkPY1b2kq6Dj09BNw/0O/GZXJSWTZTp52A=', N'SinhVien', 1, 0),
  (25, N'sv018', N'Lương Quốc Khánh', N'v1.100000.zUXqxZTJKCLhr7ZFFHPhag==.E7szacGYPnkPY1b2kq6Dj09BNw/0O/GZXJSWTZTp52A=', N'SinhVien', 1, 0),
  (26, N'sv019', N'Tạ Bảo Ngọc', N'v1.100000.zUXqxZTJKCLhr7ZFFHPhag==.E7szacGYPnkPY1b2kq6Dj09BNw/0O/GZXJSWTZTp52A=', N'SinhVien', 1, 0),
  (27, N'sv020', N'Chu Minh Quân', N'v1.100000.zUXqxZTJKCLhr7ZFFHPhag==.E7szacGYPnkPY1b2kq6Dj09BNw/0O/GZXJSWTZTp52A=', N'SinhVien', 1, 0);
SET IDENTITY_INSERT dbo.NguoiDungs OFF;

INSERT INTO dbo.GiangViens (MaGV, HoTen, Email, SoDienThoai, MaKhoa, MaND) VALUES
  (N'GV001', N'Nguyễn Văn An', N'gv001@example.edu.vn', N'090100001', 1, 3),
  (N'GV002', N'Trần Thị Bình', N'gv002@example.edu.vn', N'090100002', 1, 4),
  (N'GV003', N'Lê Minh Châu', N'gv003@example.edu.vn', N'090100003', 1, 5),
  (N'GV004', N'Phạm Quốc Dũng', N'gv004@example.edu.vn', N'090100004', 2, 6),
  (N'GV005', N'Hoàng Thu Hà', N'gv005@example.edu.vn', N'090100005', 1, 7);

INSERT INTO dbo.SinhViens (MaSV, HoTen, NgaySinh, Email, MaLop, MaND) VALUES
  (N'SV001', N'Nguyễn Hoàng Nam', '2004-01-05', N'sv001@example.edu.vn', 1, 8),
  (N'SV002', N'Trần Minh Anh', '2004-02-06', N'sv002@example.edu.vn', 1, 9),
  (N'SV003', N'Lê Thu Trang', '2004-03-07', N'sv003@example.edu.vn', 1, 10),
  (N'SV004', N'Phạm Đức Huy', '2004-04-08', N'sv004@example.edu.vn', 1, 11),
  (N'SV005', N'Vũ Thanh Hương', '2004-05-09', N'sv005@example.edu.vn', 1, 12),
  (N'SV006', N'Đặng Quang Vinh', '2004-06-10', N'sv006@example.edu.vn', 1, 13),
  (N'SV007', N'Bùi Ngọc Lan', '2004-07-11', N'sv007@example.edu.vn', 1, 14),
  (N'SV008', N'Đỗ Gia Bảo', '2004-08-12', N'sv008@example.edu.vn', 2, 15),
  (N'SV009', N'Hồ Thị Mai', '2004-09-13', N'sv009@example.edu.vn', 2, 16),
  (N'SV010', N'Ngô Văn Tuấn', '2004-10-14', N'sv010@example.edu.vn', 2, 17),
  (N'SV011', N'Dương Khánh Linh', '2004-11-15', N'sv011@example.edu.vn', 2, 18),
  (N'SV012', N'Lý Hải Đăng', '2004-12-16', N'sv012@example.edu.vn', 2, 19),
  (N'SV013', N'Phan Thảo Nguyên', '2004-01-17', N'sv013@example.edu.vn', 2, 20),
  (N'SV014', N'Trịnh Công Thành', '2004-02-18', N'sv014@example.edu.vn', 2, 21),
  (N'SV015', N'Mai Phương Thảo', '2004-03-19', N'sv015@example.edu.vn', 3, 22),
  (N'SV016', N'Cao Xuân Bách', '2004-04-20', N'sv016@example.edu.vn', 3, 23),
  (N'SV017', N'Đinh Thùy Dung', '2004-05-21', N'sv017@example.edu.vn', 3, 24),
  (N'SV018', N'Lương Quốc Khánh', '2004-06-22', N'sv018@example.edu.vn', 3, 25),
  (N'SV019', N'Tạ Bảo Ngọc', '2004-07-23', N'sv019@example.edu.vn', 3, 26),
  (N'SV020', N'Chu Minh Quân', '2004-08-24', N'sv020@example.edu.vn', 3, 27);

INSERT INTO dbo.LopHocPhans (MaLHP, MaMH, MaGV, MaHK, SiSoToiDa, TrangThai) VALUES
  (N'LHP01', N'INT3306', N'GV001', 1, 40, N'Mở đăng ký'),
  (N'LHP02', N'INT3307', N'GV001', 1, 30, N'Mở đăng ký'),
  (N'LHP03', N'INT2211', N'GV002', 1, 40, N'Mở đăng ký'),
  (N'LHP04', N'INT2213', N'GV003', 1, 40, N'Mở đăng ký'),
  (N'LHP05', N'INT1306', N'GV005', 1, 40, N'Mở đăng ký'),
  (N'LHP06', N'MAT1100', N'GV004', 1, 50, N'Mở đăng ký'),
  (N'LHP07', N'MAT1093', N'GV004', 1, 50, N'Mở đăng ký'),
  (N'LHP08', N'MAT1101', N'GV004', 1, 50, N'Đóng'),
  (N'LHP09', N'INT3306', N'GV005', 1, 40, N'Mở đăng ký'),
  (N'LHP10', N'INT2211', N'GV002', 1, 40, N'Mở đăng ký'),
  (N'LHP11', N'INT2213', N'GV003', 1, 40, N'Mở đăng ký'),
  (N'LHP12', N'MAT1100', N'GV004', 1, 50, N'Mở đăng ký'),
  (N'LHP13', N'INT1306', N'GV001', 1, 40, N'Mở đăng ký');

INSERT INTO dbo.LichHocs (MaLHP, MaPhong, Thu, TietBatDau, SoTiet, TuanBatDau, TuanKetThuc) VALUES
  (N'LHP01', 5, 2, 1, 3, 1, 15),
  (N'LHP02', 7, 3, 1, 3, 1, 15),
  (N'LHP03', 2, 2, 4, 3, 1, 15),
  (N'LHP04', 3, 3, 4, 3, 1, 15),
  (N'LHP05', 1, 4, 1, 3, 1, 15),
  (N'LHP06', 3, 5, 1, 3, 1, 15),
  (N'LHP07', 3, 5, 4, 3, 1, 15),
  (N'LHP08', 4, 6, 1, 3, 1, 15),
  (N'LHP09', 2, 4, 4, 3, 1, 15),
  (N'LHP10', 1, 3, 7, 3, 1, 15),
  (N'LHP11', 4, 5, 7, 3, 1, 15),
  (N'LHP12', 3, 4, 7, 3, 1, 15),
  (N'LHP13', 1, 5, 4, 3, 1, 15);

INSERT INTO dbo.LichBans (MaGV, Thu, TietBatDau, SoTiet, GhiChu) VALUES (N'GV002', 6, 7, 6, N'Họp khoa chiều thứ 6');

INSERT INTO dbo.DangKys (MaSV, MaLHP, NgayDangKy) VALUES
  (N'SV001', N'LHP01', SYSDATETIME()),
  (N'SV001', N'LHP03', SYSDATETIME()),
  (N'SV001', N'LHP04', SYSDATETIME()),
  (N'SV001', N'LHP02', SYSDATETIME()),
  (N'SV002', N'LHP01', SYSDATETIME()),
  (N'SV002', N'LHP03', SYSDATETIME()),
  (N'SV002', N'LHP04', SYSDATETIME()),
  (N'SV002', N'LHP02', SYSDATETIME()),
  (N'SV003', N'LHP01', SYSDATETIME()),
  (N'SV003', N'LHP03', SYSDATETIME()),
  (N'SV003', N'LHP04', SYSDATETIME()),
  (N'SV003', N'LHP02', SYSDATETIME()),
  (N'SV004', N'LHP01', SYSDATETIME()),
  (N'SV004', N'LHP03', SYSDATETIME()),
  (N'SV004', N'LHP04', SYSDATETIME()),
  (N'SV004', N'LHP02', SYSDATETIME()),
  (N'SV005', N'LHP01', SYSDATETIME()),
  (N'SV005', N'LHP03', SYSDATETIME()),
  (N'SV005', N'LHP04', SYSDATETIME()),
  (N'SV005', N'LHP02', SYSDATETIME()),
  (N'SV006', N'LHP01', SYSDATETIME()),
  (N'SV006', N'LHP03', SYSDATETIME()),
  (N'SV006', N'LHP04', SYSDATETIME()),
  (N'SV007', N'LHP01', SYSDATETIME()),
  (N'SV007', N'LHP03', SYSDATETIME()),
  (N'SV007', N'LHP04', SYSDATETIME()),
  (N'SV008', N'LHP01', SYSDATETIME()),
  (N'SV008', N'LHP03', SYSDATETIME()),
  (N'SV008', N'LHP04', SYSDATETIME()),
  (N'SV009', N'LHP01', SYSDATETIME()),
  (N'SV009', N'LHP03', SYSDATETIME()),
  (N'SV009', N'LHP04', SYSDATETIME()),
  (N'SV010', N'LHP01', SYSDATETIME()),
  (N'SV010', N'LHP03', SYSDATETIME()),
  (N'SV010', N'LHP04', SYSDATETIME()),
  (N'SV011', N'LHP03', SYSDATETIME()),
  (N'SV011', N'LHP05', SYSDATETIME()),
  (N'SV011', N'LHP06', SYSDATETIME()),
  (N'SV012', N'LHP03', SYSDATETIME()),
  (N'SV012', N'LHP05', SYSDATETIME()),
  (N'SV012', N'LHP06', SYSDATETIME()),
  (N'SV013', N'LHP03', SYSDATETIME()),
  (N'SV013', N'LHP05', SYSDATETIME()),
  (N'SV013', N'LHP06', SYSDATETIME()),
  (N'SV014', N'LHP03', SYSDATETIME()),
  (N'SV014', N'LHP05', SYSDATETIME()),
  (N'SV014', N'LHP06', SYSDATETIME()),
  (N'SV015', N'LHP03', SYSDATETIME()),
  (N'SV015', N'LHP05', SYSDATETIME()),
  (N'SV015', N'LHP06', SYSDATETIME()),
  (N'SV016', N'LHP03', SYSDATETIME()),
  (N'SV016', N'LHP05', SYSDATETIME()),
  (N'SV016', N'LHP06', SYSDATETIME()),
  (N'SV017', N'LHP03', SYSDATETIME()),
  (N'SV017', N'LHP05', SYSDATETIME()),
  (N'SV017', N'LHP06', SYSDATETIME()),
  (N'SV018', N'LHP03', SYSDATETIME()),
  (N'SV018', N'LHP05', SYSDATETIME()),
  (N'SV018', N'LHP06', SYSDATETIME()),
  (N'SV019', N'LHP03', SYSDATETIME()),
  (N'SV019', N'LHP05', SYSDATETIME()),
  (N'SV019', N'LHP06', SYSDATETIME()),
  (N'SV020', N'LHP03', SYSDATETIME()),
  (N'SV020', N'LHP05', SYSDATETIME()),
  (N'SV020', N'LHP06', SYSDATETIME());

COMMIT TRANSACTION;
PRINT N'Đã nạp dữ liệu mẫu.';
END
GO
