using QuanLyLichHoc.Data;
using QuanLyLichHoc.Models;

namespace QuanLyLichHoc.Services
{
    public class AuditService
    {
        private readonly AppDbContext db;
        public AuditService(AppDbContext db) { this.db = db; }

        public async Task LogAsync(string user, string action, string detail)
        {
            db.NhatKys.Add(new NhatKy { NguoiDung = user, HanhDong = action, ChiTiet = detail?.Length > 500 ? detail.Substring(0, 500) : detail });
            await db.SaveChangesAsync();
        }
    }
}
