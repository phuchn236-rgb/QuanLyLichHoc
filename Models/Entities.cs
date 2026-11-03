using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyLichHoc.Models
{
    public static class VaiTro
    {
        public const string Admin = "Admin";
        public const string GiaoVu = "GiaoVu";
        public const string GiangVien = "GiangVien";
        public const string SinhVien = "SinhVien";
        public static readonly string[] All = { Admin, GiaoVu, GiangVien, SinhVien };
    }

    public class NguoiDung
    {
        [Key] public int MaND { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập"), StringLength(50)]
        [Display(Name = "Tên đăng nhập")]
        public string TenDangNhap { get; set; }

        [StringLength(100), Display(Name = "Họ tên")]
        public string HoTen { get; set; }

        [StringLength(200)] public string MatKhauHash { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn vai trò"), StringLength(20), Display(Name = "Vai trò")]
        public string VaiTro { get; set; }

        [Display(Name = "Hoạt động")] public bool TrangThai { get; set; } = true;
        public int SoLanSai { get; set; }
        public DateTime? KhoaDen { get; set; }

        [NotMapped, Display(Name = "Mật khẩu")] public string MatKhauMoi { get; set; }
    }

    public class Khoa
    {
        [Key] public int MaKhoa { get; set; }
        [Required(ErrorMessage = "Vui lòng nhập tên khoa"), StringLength(100), Display(Name = "Tên khoa")]
        public string TenKhoa { get; set; }
    }

    public class Lop
    {
        [Key] public int MaLop { get; set; }
        [Required(ErrorMessage = "Vui lòng nhập tên lớp"), StringLength(50), Display(Name = "Tên lớp")]
        public string TenLop { get; set; }
        [Display(Name = "Khoa")] public int MaKhoa { get; set; }
        [Range(1, 200, ErrorMessage = "Khóa học không hợp lệ"), Display(Name = "Khóa học")]
        public int KhoaHoc { get; set; }
        public virtual Khoa Khoa { get; set; }
    }

    public class SinhVien
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
        [Required(ErrorMessage = "Vui lòng nhập mã sinh viên"), StringLength(15), Display(Name = "Mã SV")]
        public string MaSV { get; set; }
        [Required(ErrorMessage = "Vui lòng nhập họ tên"), StringLength(100), Display(Name = "Họ tên")]
        public string HoTen { get; set; }
        [DataType(DataType.Date), Display(Name = "Ngày sinh")]
        public DateTime NgaySinh { get; set; } = new DateTime(2004, 1, 1);
        [Required(ErrorMessage = "Vui lòng nhập email"), EmailAddress(ErrorMessage = "Email không đúng định dạng"), StringLength(100)]
        [Display(Name = "Email")]
        public string Email { get; set; }
        [Display(Name = "Lớp")] public int MaLop { get; set; }
        public int? MaND { get; set; }
        public virtual Lop Lop { get; set; }
        public virtual NguoiDung NguoiDung { get; set; }
    }

    public class GiangVien
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
        [Required(ErrorMessage = "Vui lòng nhập mã giảng viên"), StringLength(15), Display(Name = "Mã GV")]
        public string MaGV { get; set; }
        [Required(ErrorMessage = "Vui lòng nhập họ tên"), StringLength(100), Display(Name = "Họ tên")]
        public string HoTen { get; set; }
        [Required(ErrorMessage = "Vui lòng nhập email"), EmailAddress(ErrorMessage = "Email không đúng định dạng"), StringLength(100)]
        [Display(Name = "Email")]
        public string Email { get; set; }
        [StringLength(20), Display(Name = "Số điện thoại")] public string SoDienThoai { get; set; }
        [Display(Name = "Khoa")] public int MaKhoa { get; set; }
        public int? MaND { get; set; }
        public virtual Khoa Khoa { get; set; }
        public virtual NguoiDung NguoiDung { get; set; }
    }

    public class MonHoc
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
        [Required(ErrorMessage = "Vui lòng nhập mã môn học"), StringLength(20), Display(Name = "Mã môn")]
        public string MaMH { get; set; }
        [Required(ErrorMessage = "Vui lòng nhập tên môn học"), StringLength(150), Display(Name = "Tên môn học")]
        public string TenMH { get; set; }
        [Range(1, 10, ErrorMessage = "Số tín chỉ từ 1 đến 10"), Display(Name = "Số tín chỉ")]
        public int SoTinChi { get; set; } = 3;
        [Range(1, 200, ErrorMessage = "Số tiết phải lớn hơn 0"), Display(Name = "Số tiết")]
        public int SoTiet { get; set; } = 45;
        [Display(Name = "Môn thực hành")] public bool ThucHanh { get; set; }
        [Display(Name = "Khoa")] public int MaKhoa { get; set; }
        public virtual Khoa Khoa { get; set; }
    }

    public class HocKy
    {
        [Key] public int MaHK { get; set; }
        [Required(ErrorMessage = "Vui lòng nhập tên học kỳ"), StringLength(50), Display(Name = "Tên học kỳ")]
        public string TenHK { get; set; }
        [Required(ErrorMessage = "Vui lòng nhập năm học"), StringLength(20), Display(Name = "Năm học")]
        public string NamHoc { get; set; }
        [DataType(DataType.Date), Display(Name = "Ngày bắt đầu")] public DateTime NgayBatDau { get; set; } = DateTime.Today;
        [DataType(DataType.Date), Display(Name = "Ngày kết thúc")] public DateTime NgayKetThuc { get; set; } = DateTime.Today.AddDays(140);
        [NotMapped] public string Ten => TenHK + " (" + NamHoc + ")";
    }

    public class PhongHoc
    {
        [Key] public int MaPhong { get; set; }
        [Required(ErrorMessage = "Vui lòng nhập tên phòng"), StringLength(30), Display(Name = "Tên phòng")]
        public string TenPhong { get; set; }
        [Required, StringLength(2), Display(Name = "Loại phòng")] public string LoaiPhong { get; set; } = "LT";
        [Range(1, 1000, ErrorMessage = "Sức chứa phải lớn hơn 0"), Display(Name = "Sức chứa")]
        public int SucChua { get; set; } = 50;
        [StringLength(50), Display(Name = "Tòa nhà")] public string ToaNha { get; set; }
    }

    public class LopHocPhan
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
        [Required(ErrorMessage = "Vui lòng nhập mã lớp học phần"), StringLength(20), Display(Name = "Mã LHP")]
        public string MaLHP { get; set; }
        [Required(ErrorMessage = "Vui lòng chọn môn học"), Display(Name = "Môn học")] public string MaMH { get; set; }
        [Required(ErrorMessage = "Vui lòng chọn giảng viên"), Display(Name = "Giảng viên")] public string MaGV { get; set; }
        [Display(Name = "Học kỳ")] public int MaHK { get; set; }
        [Range(1, 500, ErrorMessage = "Sĩ số tối đa từ 1 đến 500"), Display(Name = "Sĩ số tối đa")]
        public int SiSoToiDa { get; set; } = 40;
        [Required, StringLength(20), Display(Name = "Trạng thái")] public string TrangThai { get; set; } = TrangThaiLHP.Mo;
        public virtual MonHoc MonHoc { get; set; }
        public virtual GiangVien GiangVien { get; set; }
        public virtual HocKy HocKy { get; set; }
        public virtual ICollection<LichHoc> LichHocs { get; set; } = new List<LichHoc>();
        public virtual ICollection<DangKyHocPhan> DangKys { get; set; } = new List<DangKyHocPhan>();
    }

    public static class TrangThaiLHP
    {
        public const string Mo = "Mở đăng ký";
        public const string Dong = "Đóng";
        public const string Huy = "Hủy";
        public static readonly string[] All = { Mo, Dong, Huy };
    }

    public class LichHoc
    {
        [Key] public int MaLich { get; set; }
        [Required(ErrorMessage = "Vui lòng chọn lớp học phần"), Display(Name = "Lớp học phần")] public string MaLHP { get; set; }
        [Display(Name = "Phòng học")] public int MaPhong { get; set; }
        [Range(2, 8, ErrorMessage = "Thứ phải từ 2 đến 8 (8 = Chủ nhật)"), Display(Name = "Thứ")] public int Thu { get; set; } = 2;
        [Range(1, 12, ErrorMessage = "Tiết bắt đầu từ 1 đến 12"), Display(Name = "Tiết bắt đầu")] public int TietBatDau { get; set; } = 1;
        [Range(1, 6, ErrorMessage = "Số tiết từ 1 đến 6"), Display(Name = "Số tiết")] public int SoTiet { get; set; } = 3;
        [Range(1, 20, ErrorMessage = "Tuần từ 1 đến 20"), Display(Name = "Từ tuần")] public int TuanBatDau { get; set; } = 1;
        [Range(1, 20, ErrorMessage = "Tuần từ 1 đến 20"), Display(Name = "Đến tuần")] public int TuanKetThuc { get; set; } = 15;
        public virtual LopHocPhan LopHocPhan { get; set; }
        public virtual PhongHoc PhongHoc { get; set; }
    }

    public class DangKyHocPhan
    {
        public string MaSV { get; set; }
        public string MaLHP { get; set; }
        public DateTime NgayDangKy { get; set; } = DateTime.Now;
        public virtual SinhVien SinhVien { get; set; }
        public virtual LopHocPhan LopHocPhan { get; set; }
    }

    public class LichBan
    {
        [Key] public int MaLichBan { get; set; }
        public string MaGV { get; set; }
        public int Thu { get; set; }
        public int TietBatDau { get; set; }
        public int SoTiet { get; set; }
        [StringLength(200)] public string GhiChu { get; set; }
        public virtual GiangVien GiangVien { get; set; }
    }

    public class NhatKy
    {
        [Key] public int MaNK { get; set; }
        public DateTime ThoiGian { get; set; } = DateTime.Now;
        [StringLength(50)] public string NguoiDung { get; set; }
        [StringLength(50)] public string HanhDong { get; set; }
        [StringLength(500)] public string ChiTiet { get; set; }
    }
}
