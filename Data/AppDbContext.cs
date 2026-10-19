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
            mb.Entity<SinhVien>().HasOne(x => x.NguoiDung).WithMany().HasForeignKey(x => x.MaND);
            mb.Entity<GiangVien>().HasOne(x => x.NguoiDung).WithMany().HasForeignKey(x => x.MaND);
            // Tránh lỗi "multiple cascade paths" của SQL Server: mọi khóa ngoại đều Restrict
            foreach (var fk in mb.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
                fk.DeleteBehavior = DeleteBehavior.Restrict;
        }
    }
}
