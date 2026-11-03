/* Xóa toàn bộ cơ sở dữ liệu QuanLyLichHoc (KHÔNG THỂ HOÀN TÁC). Chạy khi muốn làm lại từ đầu. */
USE master;
GO
IF DB_ID(N'QuanLyLichHoc') IS NOT NULL
BEGIN
    ALTER DATABASE QuanLyLichHoc SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE QuanLyLichHoc;
END
GO
PRINT N'Đã xóa cơ sở dữ liệu QuanLyLichHoc. Chạy tao_csdl.sql để tạo lại.';
GO
