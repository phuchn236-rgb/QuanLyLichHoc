using QuanLyLichHoc.Models;

namespace QuanLyLichHoc.Infrastructure
{
    public static class TkbHelper
    {
        public static readonly (string Bd, string Kt)[] Tiets =
        {
            ("07:00","07:50"),("07:55","08:45"),("08:50","09:40"),("10:00","10:50"),("10:55","11:45"),("12:30","13:20"),
            ("13:25","14:15"),("14:20","15:10"),("15:50","16:40"),("16:45","17:35"),("18:00","18:50"),("18:55","19:45")
        };

        public static string ThuText(int thu) => thu == 8 ? "CN" : "Thứ " + thu;

        public static string ThoiGian(int tietBd, int soTiet)
        {
            int a = Math.Clamp(tietBd, 1, 12), b = Math.Clamp(tietBd + soTiet - 1, 1, 12);
            return Tiets[a - 1].Bd + " – " + Tiets[b - 1].Kt;
        }

        public static string Mo(LichHoc x) =>
            $"{x.MaLHP} ({ThuText(x.Thu)}, tiết {x.TietBatDau}-{x.TietBatDau + x.SoTiet - 1}, tuần {x.TuanBatDau}-{x.TuanKetThuc})";

        public static bool GiaoNhau(LichHoc a, LichHoc b) =>
            a.Thu == b.Thu &&
            a.TietBatDau <= b.TietBatDau + b.SoTiet - 1 && b.TietBatDau <= a.TietBatDau + a.SoTiet - 1 &&
            a.TuanBatDau <= b.TuanKetThuc && b.TuanBatDau <= a.TuanKetThuc;

        public static LichDto ToDto(LichHoc x) => new LichDto(x.MaLich, x.MaLHP, x.LopHocPhan?.MaMH, x.LopHocPhan?.MonHoc?.TenMH,
            x.LopHocPhan?.GiangVien?.HoTen, x.PhongHoc?.TenPhong, x.Thu, ThuText(x.Thu), x.TietBatDau, x.SoTiet,
            x.TuanBatDau, x.TuanKetThuc, ThoiGian(x.TietBatDau, x.SoTiet));
    }
}
