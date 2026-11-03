/* =====================================================================
   HỆ THỐNG QUẢN LÝ LỊCH HỌC - Script tạo cơ sở dữ liệu (SQL Server)
   Chạy trong SSMS / Azure Data Studio / sqlcmd.
   - Script an toàn khi chạy lại: chỉ tạo đối tượng nếu chưa tồn tại (không xóa dữ liệu).
   - Muốn xóa toàn bộ để làm lại: chạy xoa_csdl.sql
   - Sau đó chạy du_lieu_mau.sql để nạp dữ liệu mẫu.
   Tên bảng/cột khớp với các lớp Model của ứng dụng (Entity Framework Core).
   ===================================================================== */

IF DB_ID(N'QuanLyLichHoc') IS NULL
    CREATE DATABASE QuanLyLichHoc;
GO

USE QuanLyLichHoc;
GO

/* ---------------------------- 1. NGƯỜI DÙNG ---------------------------- */
IF OBJECT_ID(N'dbo.NguoiDungs', N'U') IS NULL
CREATE TABLE dbo.NguoiDungs (
    MaND         INT IDENTITY(1,1) NOT NULL,
    TenDangNhap  NVARCHAR(50)  NOT NULL,
    HoTen        NVARCHAR(100) NULL,
    MatKhauHash  NVARCHAR(200) NULL,         
    VaiTro       NVARCHAR(20)  NOT NULL,       
    TrangThai    BIT           NOT NULL CONSTRAINT DF_NguoiDungs_TrangThai DEFAULT 1,
    SoLanSai     INT           NOT NULL CONSTRAINT DF_NguoiDungs_SoLanSai DEFAULT 0,
    KhoaDen      DATETIME2     NULL,
    CONSTRAINT PK_NguoiDungs PRIMARY KEY (MaND),
    CONSTRAINT UQ_NguoiDungs_TenDangNhap UNIQUE (TenDangNhap),
    CONSTRAINT CK_NguoiDungs_VaiTro CHECK (VaiTro IN (N'Admin', N'GiaoVu', N'GiangVien', N'SinhVien'))
);
GO

/* ---------------------------- 2. DANH MỤC ---------------------------- */
IF OBJECT_ID(N'dbo.Khoas', N'U') IS NULL
CREATE TABLE dbo.Khoas (
    MaKhoa  INT IDENTITY(1,1) NOT NULL,
    TenKhoa NVARCHAR(100) NOT NULL,
    CONSTRAINT PK_Khoas PRIMARY KEY (MaKhoa),
    CONSTRAINT UQ_Khoas_TenKhoa UNIQUE (TenKhoa)
);
GO

IF OBJECT_ID(N'dbo.Lops', N'U') IS NULL
CREATE TABLE dbo.Lops (
    MaLop   INT IDENTITY(1,1) NOT NULL,
    TenLop  NVARCHAR(50) NOT NULL,
    MaKhoa  INT NOT NULL,
    KhoaHoc INT NOT NULL,
    CONSTRAINT PK_Lops PRIMARY KEY (MaLop),
    CONSTRAINT FK_Lops_Khoas FOREIGN KEY (MaKhoa) REFERENCES dbo.Khoas (MaKhoa),
    CONSTRAINT CK_Lops_KhoaHoc CHECK (KhoaHoc BETWEEN 1 AND 200)
);
GO

IF OBJECT_ID(N'dbo.HocKys', N'U') IS NULL
CREATE TABLE dbo.HocKys (
    MaHK        INT IDENTITY(1,1) NOT NULL,
    TenHK       NVARCHAR(50) NOT NULL,
    NamHoc      NVARCHAR(20) NOT NULL,
    NgayBatDau  DATETIME2 NOT NULL,
    NgayKetThuc DATETIME2 NOT NULL,
    CONSTRAINT PK_HocKys PRIMARY KEY (MaHK),
    CONSTRAINT CK_HocKys_Ngay CHECK (NgayKetThuc > NgayBatDau)
);
GO

IF OBJECT_ID(N'dbo.PhongHocs', N'U') IS NULL
CREATE TABLE dbo.PhongHocs (
    MaPhong   INT IDENTITY(1,1) NOT NULL,
    TenPhong  NVARCHAR(30) NOT NULL,
    LoaiPhong NVARCHAR(2)  NOT NULL CONSTRAINT DF_PhongHocs_Loai DEFAULT N'LT',   -- LT: lý thuyết, TH: thực hành
    SucChua   INT NOT NULL,
    ToaNha    NVARCHAR(50) NULL,
    CONSTRAINT PK_PhongHocs PRIMARY KEY (MaPhong),
    CONSTRAINT UQ_PhongHocs_TenPhong UNIQUE (TenPhong),
    CONSTRAINT CK_PhongHocs_Loai CHECK (LoaiPhong IN (N'LT', N'TH')),
    CONSTRAINT CK_PhongHocs_SucChua CHECK (SucChua > 0)
);
GO

IF OBJECT_ID(N'dbo.MonHocs', N'U') IS NULL
CREATE TABLE dbo.MonHocs (
    MaMH     NVARCHAR(20)  NOT NULL,
    TenMH    NVARCHAR(150) NOT NULL,
    SoTinChi INT NOT NULL,
    SoTiet   INT NOT NULL,
    ThucHanh BIT NOT NULL CONSTRAINT DF_MonHocs_ThucHanh DEFAULT 0,
    MaKhoa   INT NOT NULL,
    CONSTRAINT PK_MonHocs PRIMARY KEY (MaMH),
    CONSTRAINT FK_MonHocs_Khoas FOREIGN KEY (MaKhoa) REFERENCES dbo.Khoas (MaKhoa),
    CONSTRAINT CK_MonHocs_SoTinChi CHECK (SoTinChi BETWEEN 1 AND 10),
    CONSTRAINT CK_MonHocs_SoTiet CHECK (SoTiet BETWEEN 1 AND 200)
);
GO

/* ---------------------------- 3. SINH VIÊN / GIẢNG VIÊN ---------------------------- */
IF OBJECT_ID(N'dbo.SinhViens', N'U') IS NULL
CREATE TABLE dbo.SinhViens (
    MaSV     NVARCHAR(15)  NOT NULL,
    HoTen    NVARCHAR(100) NOT NULL,
    NgaySinh DATETIME2     NOT NULL,
    Email    NVARCHAR(100) NOT NULL,
    MaLop    INT NOT NULL,
    MaND     INT NULL,
    CONSTRAINT PK_SinhViens PRIMARY KEY (MaSV),
    CONSTRAINT UQ_SinhViens_Email UNIQUE (Email),
    CONSTRAINT FK_SinhViens_Lops FOREIGN KEY (MaLop) REFERENCES dbo.Lops (MaLop),
    CONSTRAINT FK_SinhViens_NguoiDungs FOREIGN KEY (MaND) REFERENCES dbo.NguoiDungs (MaND)
);
GO

IF OBJECT_ID(N'dbo.GiangViens', N'U') IS NULL
CREATE TABLE dbo.GiangViens (
    MaGV        NVARCHAR(15)  NOT NULL,
    HoTen       NVARCHAR(100) NOT NULL,
    Email       NVARCHAR(100) NOT NULL,
    SoDienThoai NVARCHAR(20)  NULL,
    MaKhoa      INT NOT NULL,
    MaND        INT NULL,
    CONSTRAINT PK_GiangViens PRIMARY KEY (MaGV),
    CONSTRAINT UQ_GiangViens_Email UNIQUE (Email),
    CONSTRAINT FK_GiangViens_Khoas FOREIGN KEY (MaKhoa) REFERENCES dbo.Khoas (MaKhoa),
    CONSTRAINT FK_GiangViens_NguoiDungs FOREIGN KEY (MaND) REFERENCES dbo.NguoiDungs (MaND)
);
GO

/* ---------------------------- 4. LỚP HỌC PHẦN - LỊCH HỌC - ĐĂNG KÝ ---------------------------- */
IF OBJECT_ID(N'dbo.LopHocPhans', N'U') IS NULL
CREATE TABLE dbo.LopHocPhans (
    MaLHP     NVARCHAR(20) NOT NULL,
    MaMH      NVARCHAR(20) NOT NULL,
    MaGV      NVARCHAR(15) NOT NULL,
    MaHK      INT NOT NULL,
    SiSoToiDa INT NOT NULL CONSTRAINT DF_LopHocPhans_SiSo DEFAULT 40,
    TrangThai NVARCHAR(20) NOT NULL CONSTRAINT DF_LopHocPhans_TrangThai DEFAULT N'Mở đăng ký',
    CONSTRAINT PK_LopHocPhans PRIMARY KEY (MaLHP),
    CONSTRAINT FK_LopHocPhans_MonHocs FOREIGN KEY (MaMH) REFERENCES dbo.MonHocs (MaMH),
    CONSTRAINT FK_LopHocPhans_GiangViens FOREIGN KEY (MaGV) REFERENCES dbo.GiangViens (MaGV),
    CONSTRAINT FK_LopHocPhans_HocKys FOREIGN KEY (MaHK) REFERENCES dbo.HocKys (MaHK),
    CONSTRAINT CK_LopHocPhans_SiSo CHECK (SiSoToiDa BETWEEN 1 AND 500),
    CONSTRAINT CK_LopHocPhans_TrangThai CHECK (TrangThai IN (N'Mở đăng ký', N'Đóng', N'Hủy'))
);
GO

IF OBJECT_ID(N'dbo.LichHocs', N'U') IS NULL
CREATE TABLE dbo.LichHocs (
    MaLich      INT IDENTITY(1,1) NOT NULL,
    MaLHP       NVARCHAR(20) NOT NULL,
    MaPhong     INT NOT NULL,
    Thu         INT NOT NULL,        -- 2..7 = Thứ 2..Thứ 7, 8 = Chủ nhật
    TietBatDau  INT NOT NULL,        -- 1..12
    SoTiet      INT NOT NULL,        -- 1..6
    TuanBatDau  INT NOT NULL,
    TuanKetThuc INT NOT NULL,
    CONSTRAINT PK_LichHocs PRIMARY KEY (MaLich),
    CONSTRAINT FK_LichHocs_LopHocPhans FOREIGN KEY (MaLHP) REFERENCES dbo.LopHocPhans (MaLHP),
    CONSTRAINT FK_LichHocs_PhongHocs FOREIGN KEY (MaPhong) REFERENCES dbo.PhongHocs (MaPhong),
    CONSTRAINT CK_LichHocs_Thu CHECK (Thu BETWEEN 2 AND 8),
    CONSTRAINT CK_LichHocs_Tiet CHECK (TietBatDau BETWEEN 1 AND 12 AND SoTiet BETWEEN 1 AND 6 AND TietBatDau + SoTiet - 1 <= 12),
    CONSTRAINT CK_LichHocs_Buoi CHECK (NOT (TietBatDau <= 6 AND TietBatDau + SoTiet - 1 > 6)),   -- không kéo dài từ sáng sang chiều
    CONSTRAINT CK_LichHocs_Tuan CHECK (TuanBatDau BETWEEN 1 AND 20 AND TuanKetThuc BETWEEN 1 AND 20 AND TuanKetThuc >= TuanBatDau)
);
GO

IF OBJECT_ID(N'dbo.DangKys', N'U') IS NULL
CREATE TABLE dbo.DangKys (
    MaSV        NVARCHAR(15) NOT NULL,
    MaLHP       NVARCHAR(20) NOT NULL,
    NgayDangKy  DATETIME2    NOT NULL CONSTRAINT DF_DangKys_Ngay DEFAULT SYSDATETIME(),
    CONSTRAINT PK_DangKys PRIMARY KEY (MaSV, MaLHP),
    CONSTRAINT FK_DangKys_SinhViens FOREIGN KEY (MaSV) REFERENCES dbo.SinhViens (MaSV),
    CONSTRAINT FK_DangKys_LopHocPhans FOREIGN KEY (MaLHP) REFERENCES dbo.LopHocPhans (MaLHP)
);
GO

IF OBJECT_ID(N'dbo.LichBans', N'U') IS NULL
CREATE TABLE dbo.LichBans (
    MaLichBan  INT IDENTITY(1,1) NOT NULL,
    MaGV       NVARCHAR(15) NOT NULL,
    Thu        INT NOT NULL,
    TietBatDau INT NOT NULL,
    SoTiet     INT NOT NULL,
    GhiChu     NVARCHAR(200) NULL,
    CONSTRAINT PK_LichBans PRIMARY KEY (MaLichBan),
    CONSTRAINT FK_LichBans_GiangViens FOREIGN KEY (MaGV) REFERENCES dbo.GiangViens (MaGV),
    CONSTRAINT CK_LichBans_Thu CHECK (Thu BETWEEN 2 AND 8),
    CONSTRAINT CK_LichBans_Tiet CHECK (TietBatDau BETWEEN 1 AND 12 AND SoTiet >= 1 AND TietBatDau + SoTiet - 1 <= 12)
);
GO

IF OBJECT_ID(N'dbo.NhatKys', N'U') IS NULL
CREATE TABLE dbo.NhatKys (
    MaNK      INT IDENTITY(1,1) NOT NULL,
    ThoiGian  DATETIME2 NOT NULL CONSTRAINT DF_NhatKys_ThoiGian DEFAULT SYSDATETIME(),
    NguoiDung NVARCHAR(50)  NULL,
    HanhDong  NVARCHAR(50)  NULL,
    ChiTiet   NVARCHAR(500) NULL,
    CONSTRAINT PK_NhatKys PRIMARY KEY (MaNK)
);
GO

/* ---------------------------- 5. CHỈ MỤC (tăng tốc truy vấn lịch / đăng ký) ---------------------------- */
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_LichHocs_MaLHP' AND object_id = OBJECT_ID(N'dbo.LichHocs'))
    CREATE INDEX IX_LichHocs_MaLHP ON dbo.LichHocs (MaLHP);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_LichHocs_Phong_Thu' AND object_id = OBJECT_ID(N'dbo.LichHocs'))
    CREATE INDEX IX_LichHocs_Phong_Thu ON dbo.LichHocs (MaPhong, Thu, TietBatDau);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_DangKys_MaLHP' AND object_id = OBJECT_ID(N'dbo.DangKys'))
    CREATE INDEX IX_DangKys_MaLHP ON dbo.DangKys (MaLHP);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_LopHocPhans_HK_MaMH' AND object_id = OBJECT_ID(N'dbo.LopHocPhans'))
    CREATE INDEX IX_LopHocPhans_HK_MaMH ON dbo.LopHocPhans (MaHK, MaMH);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_LopHocPhans_MaGV' AND object_id = OBJECT_ID(N'dbo.LopHocPhans'))
    CREATE INDEX IX_LopHocPhans_MaGV ON dbo.LopHocPhans (MaGV);
GO

/* ---------------------------- 6. VIEW BÁO CÁO ---------------------------- */
-- Khung giờ 12 tiết: mỗi tiết 45 phút, nghỉ 5 phút (10 phút sau tiết 3 và tiết 9). Sáng: tiết 1-6, chiều: tiết 7-12.
CREATE OR ALTER VIEW dbo.vw_KhungGioTiet AS
SELECT Tiet, CAST(GioBatDau AS TIME(0)) AS GioBatDau, CAST(GioKetThuc AS TIME(0)) AS GioKetThuc
FROM (VALUES
    (1, '07:00', '07:45'), (2, '07:50', '08:35'), (3, '08:40', '09:25'), (4, '09:35', '10:20'), (5, '10:25', '11:10'), (6, '11:15', '12:00'),
    (7, '13:00', '13:45'), (8, '13:50', '14:35'), (9, '14:40', '15:25'), (10, '15:35', '16:20'), (11, '16:25', '17:10'), (12, '17:15', '18:00')
) AS t (Tiet, GioBatDau, GioKetThuc);
GO

CREATE OR ALTER VIEW dbo.vw_LichHocChiTiet AS
SELECT  lh.MaLich, lh.MaLHP, l.MaHK, hk.TenHK, hk.NamHoc,
        m.MaMH, m.TenMH, m.SoTinChi,
        g.MaGV, g.HoTen AS TenGiangVien,
        p.MaPhong, p.TenPhong, p.LoaiPhong,
        lh.Thu, lh.TietBatDau, lh.SoTiet, lh.TuanBatDau, lh.TuanKetThuc,
        kb.GioBatDau, ke.GioKetThuc
FROM    dbo.LichHocs lh
JOIN    dbo.LopHocPhans l ON l.MaLHP = lh.MaLHP
JOIN    dbo.MonHocs m     ON m.MaMH  = l.MaMH
JOIN    dbo.GiangViens g  ON g.MaGV  = l.MaGV
JOIN    dbo.PhongHocs p   ON p.MaPhong = lh.MaPhong
JOIN    dbo.HocKys hk     ON hk.MaHK = l.MaHK
JOIN    dbo.vw_KhungGioTiet kb ON kb.Tiet = lh.TietBatDau
JOIN    dbo.vw_KhungGioTiet ke ON ke.Tiet = lh.TietBatDau + lh.SoTiet - 1;
GO

CREATE OR ALTER VIEW dbo.vw_SiSoLopHocPhan AS
SELECT  l.MaLHP, l.MaHK, m.TenMH, g.HoTen AS TenGiangVien, l.TrangThai, l.SiSoToiDa,
        COUNT(d.MaSV) AS SiSoHienTai,
        CAST(100.0 * COUNT(d.MaSV) / NULLIF(l.SiSoToiDa, 0) AS DECIMAL(5,1)) AS TyLeLapDay
FROM    dbo.LopHocPhans l
JOIN    dbo.MonHocs m    ON m.MaMH = l.MaMH
JOIN    dbo.GiangViens g ON g.MaGV = l.MaGV
LEFT JOIN dbo.DangKys d  ON d.MaLHP = l.MaLHP
GROUP BY l.MaLHP, l.MaHK, m.TenMH, g.HoTen, l.TrangThai, l.SiSoToiDa;
GO

CREATE OR ALTER VIEW dbo.vw_ThoiKhoaBieuSinhVien AS
SELECT  s.MaSV, s.HoTen AS TenSinhVien, c.*
FROM    dbo.DangKys dk
JOIN    dbo.SinhViens s ON s.MaSV = dk.MaSV
JOIN    dbo.vw_LichHocChiTiet c ON c.MaLHP = dk.MaLHP;
GO

CREATE OR ALTER VIEW dbo.vw_CongSuatPhong AS
SELECT  p.MaPhong, p.TenPhong, hk.MaHK,
        COUNT(lh.MaLich) AS SoBuoiMoiTuan,
        ISNULL(SUM(lh.SoTiet), 0) AS SoTietMoiTuan,
        CAST(100.0 * ISNULL(SUM(lh.SoTiet), 0) / 72 AS DECIMAL(5,1)) AS CongSuat   -- 12 tiết x 6 ngày
FROM    dbo.PhongHocs p
CROSS JOIN dbo.HocKys hk
LEFT JOIN dbo.LopHocPhans l ON l.MaHK = hk.MaHK
LEFT JOIN dbo.LichHocs lh   ON lh.MaLHP = l.MaLHP AND lh.MaPhong = p.MaPhong
GROUP BY p.MaPhong, p.TenPhong, hk.MaHK;
GO

/* ---------------------------- 7. THỦ TỤC ---------------------------- */
-- Thời khóa biểu của một sinh viên trong học kỳ (lọc theo tuần nếu truyền @Tuan)
CREATE OR ALTER PROCEDURE dbo.sp_ThoiKhoaBieuSinhVien
    @MaSV NVARCHAR(15), @MaHK INT, @Tuan INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT  Thu, TietBatDau, SoTiet, GioBatDau, GioKetThuc, MaLHP, TenMH, TenGiangVien, TenPhong, TuanBatDau, TuanKetThuc
    FROM    dbo.vw_ThoiKhoaBieuSinhVien
    WHERE   MaSV = @MaSV AND MaHK = @MaHK
            AND (@Tuan IS NULL OR (TuanBatDau <= @Tuan AND @Tuan <= TuanKetThuc))
    ORDER BY Thu, TietBatDau;
END
GO

-- Kiểm tra xung đột phòng / giảng viên cho một buổi học dự kiến (@MaLich = 0 khi thêm mới)
-- Không có dòng kết quả = không xung đột.
CREATE OR ALTER PROCEDURE dbo.sp_KiemTraXungDotLich
    @MaLHP NVARCHAR(20), @MaPhong INT, @Thu INT, @TietBatDau INT, @SoTiet INT,
    @TuanBatDau INT, @TuanKetThuc INT, @MaLich INT = 0
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @MaHK INT, @MaGV NVARCHAR(15), @TietKetThuc INT = @TietBatDau + @SoTiet - 1;
    SELECT @MaHK = MaHK, @MaGV = MaGV FROM dbo.LopHocPhans WHERE MaLHP = @MaLHP;

    SELECT  CASE WHEN c.MaPhong = @MaPhong THEN N'Trùng phòng' ELSE N'Trùng giảng viên' END AS LoaiXungDot,
            c.MaLich, c.MaLHP, c.TenPhong, c.TenGiangVien, c.Thu, c.TietBatDau, c.SoTiet, c.TuanBatDau, c.TuanKetThuc
    FROM    dbo.vw_LichHocChiTiet c
    WHERE   c.MaHK = @MaHK AND c.MaLich <> @MaLich AND c.Thu = @Thu
            AND c.TietBatDau <= @TietKetThuc AND @TietBatDau <= c.TietBatDau + c.SoTiet - 1
            AND c.TuanBatDau <= @TuanKetThuc AND @TuanBatDau <= c.TuanKetThuc
            AND (c.MaPhong = @MaPhong OR c.MaGV = @MaGV);
END
GO

-- Thống kê nhanh theo học kỳ
CREATE OR ALTER PROCEDURE dbo.sp_ThongKeHocKy
    @MaHK INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        (SELECT COUNT(*) FROM dbo.LopHocPhans WHERE MaHK = @MaHK) AS SoLopHocPhan,
        (SELECT COUNT(*) FROM dbo.LichHocs lh JOIN dbo.LopHocPhans l ON l.MaLHP = lh.MaLHP WHERE l.MaHK = @MaHK) AS SoBuoiHoc,
        (SELECT COUNT(*) FROM dbo.DangKys d JOIN dbo.LopHocPhans l ON l.MaLHP = d.MaLHP WHERE l.MaHK = @MaHK) AS SoLuotDangKy,
        (SELECT COUNT(DISTINCT d.MaSV) FROM dbo.DangKys d JOIN dbo.LopHocPhans l ON l.MaLHP = d.MaLHP WHERE l.MaHK = @MaHK) AS SoSinhVienDangKy;
END
GO

PRINT N'Đã tạo xong cơ sở dữ liệu QuanLyLichHoc.';
GO
