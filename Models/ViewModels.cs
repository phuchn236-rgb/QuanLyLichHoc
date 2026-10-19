using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace QuanLyLichHoc.Models
{
    public interface IPaged { int PageIndex { get; } int TotalPages { get; } int TotalCount { get; } }

    public class PaginatedList<T> : List<T>, IPaged
    {
        public int PageIndex { get; private set; }
        public int TotalPages { get; private set; }
        public int TotalCount { get; private set; }

        public PaginatedList(List<T> items, int count, int page, int size)
        {
            PageIndex = page; TotalCount = count;
            TotalPages = Math.Max(1, (int)Math.Ceiling(count / (double)size));
            AddRange(items);
        }

        public static async Task<PaginatedList<T>> CreateAsync(IQueryable<T> source, int page, int size)
        {
            if (page < 1) page = 1;
            var count = await source.CountAsync();
            var pages = Math.Max(1, (int)Math.Ceiling(count / (double)size));
            if (page > pages) page = pages;
            var items = await source.Skip((page - 1) * size).Take(size).ToListAsync();
            return new PaginatedList<T>(items, count, page, size);
        }
    }

    public class LoginVM
    {
        [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập")] public string TenDangNhap { get; set; }
        [Required(ErrorMessage = "Vui lòng nhập mật khẩu"), DataType(DataType.Password)] public string MatKhau { get; set; }
        public bool GhiNho { get; set; }
        public string ReturnUrl { get; set; }
    }

    public class ChangePasswordVM
    {
        [Required(ErrorMessage = "Nhập mật khẩu hiện tại"), DataType(DataType.Password)] public string MatKhauCu { get; set; }
        [Required(ErrorMessage = "Nhập mật khẩu mới"), StringLength(100, MinimumLength = 8, ErrorMessage = "Mật khẩu mới tối thiểu 8 ký tự"), DataType(DataType.Password)]
        public string MatKhauMoi { get; set; }
        [Compare("MatKhauMoi", ErrorMessage = "Xác nhận mật khẩu không khớp"), DataType(DataType.Password)] public string XacNhan { get; set; }
    }

    public class LichHocInput
    {
        [Required] public string MaLHP { get; set; }
        [Range(1, int.MaxValue)] public int MaPhong { get; set; }
        [Range(2, 8)] public int Thu { get; set; }
        [Range(1, 12)] public int TietBatDau { get; set; }
        [Range(1, 6)] public int SoTiet { get; set; }
        [Range(1, 20)] public int TuanBatDau { get; set; }
        [Range(1, 20)] public int TuanKetThuc { get; set; }
    }

    public record LichDto(int MaLich, string MaLHP, string MaMH, string TenMon, string GiangVien, string Phong,
        int Thu, string ThuText, int TietBatDau, int SoTiet, int TuanBatDau, int TuanKetThuc, string ThoiGian);

    public class RegRow
    {
        public string MaLHP { get; set; }
        public string TenMon { get; set; }
        public int SoTinChi { get; set; }
        public string GiangVien { get; set; }
        public int SiSo { get; set; }
        public int SiSoToiDa { get; set; }
        public string TrangThai { get; set; }
        public bool DaDangKy { get; set; }
        public string Lich { get; set; }
    }

    public class ReportRow { public string Ten { get; set; } public int SoBuoi { get; set; } public int TietTuan { get; set; } public int TongTiet { get; set; } public double TyLe { get; set; } }
}
