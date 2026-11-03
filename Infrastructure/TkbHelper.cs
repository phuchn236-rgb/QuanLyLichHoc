using QuanLyLichHoc.Models;

namespace QuanLyLichHoc.Infrastructure
{
    public static class TkbHelper
    {
        /// <summary>
        /// Khung giờ 12 tiết, mỗi tiết 45 phút, nghỉ 5 phút giữa các tiết (nghỉ 10 phút sau tiết 3 và tiết 9).
        /// Buổi sáng: tiết 1–6 (07:00–12:00). Buổi chiều: tiết 7–12 (13:00–18:00).
        /// Ví dụ: tiết 1–3 = 07:00–09:25; tiết 4–6 = 09:35–12:00; tiết 7–9 = 13:00–15:25.
        /// </summary>
        public static readonly (string Bd, string Kt)[] Tiets =
        {
            ("07:00","07:45"),("07:50","08:35"),("08:40","09:25"),("09:35","10:20"),("10:25","11:10"),("11:15","12:00"),
            ("13:00","13:45"),("13:50","14:35"),("14:40","15:25"),("15:35","16:20"),("16:25","17:10"),("17:15","18:00")
        };

        /// <summary>Số tiết cuối của buổi sáng; một buổi học không được kéo dài từ sáng sang chiều.</summary>
        public const int TietCuoiBuoiSang = 6;

        /// <summary>Tuần học hiện tại (1..20) tính từ ngày bắt đầu học kỳ.</summary>
        public static int TuanHienTai(HocKy hk)
        {
            if (hk == null) return 1;
            int t = (int)Math.Floor((DateTime.Today - hk.NgayBatDau.Date).TotalDays / 7) + 1;
            return Math.Clamp(t, 1, 20);
        }

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
