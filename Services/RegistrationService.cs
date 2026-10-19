using Microsoft.EntityFrameworkCore;
using QuanLyLichHoc.Data;
using QuanLyLichHoc.Infrastructure;
using QuanLyLichHoc.Models;

namespace QuanLyLichHoc.Services
{
    public class RegistrationService
    {
        private readonly AppDbContext db;
        public RegistrationService(AppDbContext db) { this.db = db; }

        public async Task<(bool ok, string msg)> DangKyAsync(string maSV, string maLHP)
        {
            if (!await db.SinhViens.AnyAsync(s => s.MaSV == maSV)) return (false, "Không tìm thấy sinh viên.");
            var lhp = await db.LopHocPhans.Include(x => x.LichHocs).FirstOrDefaultAsync(x => x.MaLHP == maLHP);
            if (lhp == null) return (false, "Lớp học phần không tồn tại.");
            if (lhp.TrangThai != TrangThaiLHP.Mo) return (false, "Lớp học phần hiện không mở đăng ký.");
            if (await db.DangKys.AnyAsync(d => d.MaSV == maSV && d.MaLHP == maLHP)) return (false, "Bạn đã đăng ký lớp học phần này.");
            if (await db.DangKys.CountAsync(d => d.MaLHP == maLHP) >= lhp.SiSoToiDa) return (false, "Lớp đã đủ sĩ số tối đa.");
            if (await db.DangKys.AnyAsync(d => d.MaSV == maSV && d.LopHocPhan.MaMH == lhp.MaMH && d.LopHocPhan.MaHK == lhp.MaHK))
                return (false, "Bạn đã đăng ký một lớp khác của môn học này trong học kỳ.");

            var daDangKy = await db.LichHocs.Include(l => l.LopHocPhan)
                .Where(l => l.LopHocPhan.MaHK == lhp.MaHK && l.LopHocPhan.DangKys.Any(d => d.MaSV == maSV)).ToListAsync();
            foreach (var moi in lhp.LichHocs)
                foreach (var cu in daDangKy.Where(c => TkbHelper.GiaoNhau(moi, c)))
                    return (false, "Trùng lịch với " + TkbHelper.Mo(cu) + ".");

            db.DangKys.Add(new DangKyHocPhan { MaSV = maSV, MaLHP = maLHP, NgayDangKy = DateTime.Now });
            try { await db.SaveChangesAsync(); }
            catch (DbUpdateException) { return (false, "Không thể đăng ký, vui lòng thử lại."); }
            return (true, "Đăng ký thành công.");
        }

        public async Task<(bool ok, string msg)> HuyAsync(string maSV, string maLHP)
        {
            var dk = await db.DangKys.FindAsync(maSV, maLHP);
            if (dk == null) return (false, "Bạn chưa đăng ký lớp học phần này.");
            db.DangKys.Remove(dk);
            await db.SaveChangesAsync();
            return (true, "Đã hủy đăng ký.");
        }
    }
}
