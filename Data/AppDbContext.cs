using Microsoft.EntityFrameworkCore;
using QuanLyLichHoc.Models;

namespace QuanLyLichHoc.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<NguoiDung> NguoiDungs { get; set; }
        public DbSet<Khoa> Khoas { get; set; }
        public DbSet<Lop> Lops { get; set; }
        public DbSet<SinhVien> SinhViens { get; set; }
        public DbSet<GiangVien> GiangViens { get; set; }
        public DbSet<MonHoc> MonHocs { get; set; }
        public DbSet<HocKy> HocKys { get; set; }
        public DbSet<PhongHoc> PhongHocs { get; set; }
        public DbSet<LopHocPhan> LopHocPhans { get; set; }
        public DbSet<LichHoc> LichHocs { get; set; }
        public DbSet<DangKyHocPhan> DangKys { get; set; }
        public DbSet<LichBan> LichBans { get; set; }
        public DbSet<NhatKy> NhatKys { get; set; }

        protected override void OnModelCreating(ModelBuilder mb)
        {
            mb.Entity<DangKyHocPhan>().HasKey(x => new { x.MaSV, x.MaLHP });
            mb.Entity<NguoiDung>().HasIndex(x => x.TenDangNhap).IsUnique();
            mb.Entity<Khoa>().HasIndex(x => x.TenKhoa).IsUnique();
            mb.Entity<PhongHoc>().HasIndex(x => x.TenPhong).IsUnique();
            mb.Entity<SinhVien>().HasIndex(x => x.Email).IsUnique();
            mb.Entity<GiangVien>().HasIndex(x => x.Email).IsUnique();

            // ===== Khai báo TƯỜNG MINH mọi khóa ngoại (không phụ thuộc quy ước đặt tên) =====
            // Restrict: tránh lỗi "multiple cascade paths" của SQL Server và không xóa dây chuyền ngoài ý muốn.
            mb.Entity<Lop>().HasOne(x => x.Khoa).WithMany().HasForeignKey(x => x.MaKhoa).IsRequired().OnDelete(DeleteBehavior.Restrict);
            mb.Entity<SinhVien>().HasOne(x => x.Lop).WithMany().HasForeignKey(x => x.MaLop).IsRequired().OnDelete(DeleteBehavior.Restrict);
            mb.Entity<SinhVien>().HasOne(x => x.NguoiDung).WithMany().HasForeignKey(x => x.MaND).OnDelete(DeleteBehavior.Restrict);
            mb.Entity<GiangVien>().HasOne(x => x.Khoa).WithMany().HasForeignKey(x => x.MaKhoa).IsRequired().OnDelete(DeleteBehavior.Restrict);
            mb.Entity<GiangVien>().HasOne(x => x.NguoiDung).WithMany().HasForeignKey(x => x.MaND).OnDelete(DeleteBehavior.Restrict);
            mb.Entity<MonHoc>().HasOne(x => x.Khoa).WithMany().HasForeignKey(x => x.MaKhoa).IsRequired().OnDelete(DeleteBehavior.Restrict);
            mb.Entity<LopHocPhan>().HasOne(x => x.MonHoc).WithMany().HasForeignKey(x => x.MaMH).IsRequired().OnDelete(DeleteBehavior.Restrict);
            mb.Entity<LopHocPhan>().HasOne(x => x.GiangVien).WithMany().HasForeignKey(x => x.MaGV).IsRequired().OnDelete(DeleteBehavior.Restrict);
            mb.Entity<LopHocPhan>().HasOne(x => x.HocKy).WithMany().HasForeignKey(x => x.MaHK).IsRequired().OnDelete(DeleteBehavior.Restrict);
            mb.Entity<LichHoc>().HasOne(x => x.LopHocPhan).WithMany(x => x.LichHocs).HasForeignKey(x => x.MaLHP).IsRequired().OnDelete(DeleteBehavior.Restrict);
            mb.Entity<LichHoc>().HasOne(x => x.PhongHoc).WithMany().HasForeignKey(x => x.MaPhong).IsRequired().OnDelete(DeleteBehavior.Restrict);
            mb.Entity<DangKyHocPhan>().HasOne(x => x.SinhVien).WithMany().HasForeignKey(x => x.MaSV).IsRequired().OnDelete(DeleteBehavior.Restrict);
            mb.Entity<DangKyHocPhan>().HasOne(x => x.LopHocPhan).WithMany(x => x.DangKys).HasForeignKey(x => x.MaLHP).IsRequired().OnDelete(DeleteBehavior.Restrict);
            mb.Entity<LichBan>().HasOne(x => x.GiangVien).WithMany().HasForeignKey(x => x.MaGV).IsRequired().OnDelete(DeleteBehavior.Restrict);
        }
    }
}
