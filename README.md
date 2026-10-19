# Hệ thống Quản lý lịch học (Thời khóa biểu)

Bài tập lớn học phần Lập trình Web — ASP.NET Core MVC (.NET 8) + Entity Framework Core + Bootstrap 5 + jQuery/AJAX.

## 1. Yêu cầu
- **.NET 8 SDK** (https://dotnet.microsoft.com/download/dotnet/8.0)
- Máy có Internet (Bootstrap, jQuery, Chart.js nạp từ CDN, và khôi phục gói NuGet lần đầu)
- Visual Studio 2022 / VS Code / Rider đều được

## 2. Chạy ngay (không cần cài CSDL)
```bash
cd QuanLyLichHoc
dotnet run
```
Mở trình duyệt: **http://localhost:5100** (hoặc https://localhost:7100; nếu trình duyệt cảnh báo chứng chỉ, chạy `dotnet dev-certs https --trust`).

Lần chạy đầu tiên, hệ thống tự tạo CSDL SQLite `lichhoc.db` và nạp dữ liệu mẫu (khoa, lớp, môn, phòng, giảng viên, sinh viên, lớp học phần, lịch học).
Muốn nạp lại dữ liệu mẫu: dừng ứng dụng, xóa tệp `lichhoc.db`, chạy lại.

## 3. Tài khoản dùng thử
| Vai trò | Tên đăng nhập | Mật khẩu |
|---|---|---|
| Quản trị viên | admin | Admin@123 |
| Giáo vụ | giaovu | GiaoVu@123 |
| Giảng viên | gv001 … gv005 | GiangVien@123 |
| Sinh viên | sv001 … sv020 | SinhVien@123 |

Giảng viên/sinh viên thêm mới qua giao diện sẽ tự có tài khoản (tên = mã viết thường, mật khẩu mặc định như bảng trên).

## 4. Chuyển sang SQL Server
Sửa `appsettings.json`:
```json
"Database": { "Provider": "SqlServer" },
"ConnectionStrings": { "SqlServer": "Server=(localdb)\\MSSQLLocalDB;Database=QuanLyLichHoc;Trusted_Connection=True;TrustServerCertificate=True" }
```
CSDL được tạo tự động khi chạy (`EnsureCreated`). Nếu sau này sửa Entity, hãy xóa CSDL cũ để tạo lại (hoặc chuyển sang EF Migrations).

## 5. Cấu trúc thư mục
```
QuanLyLichHoc/
├─ Program.cs                      Cấu hình: DB, cookie auth, session, CSRF, đa ngôn ngữ, bảo mật header
├─ Models/                         Entities (có DataAnnotations), ViewModels, PaginatedList
├─ Data/                           AppDbContext, DbSeeder (dữ liệu mẫu)
├─ Services/                       PasswordHasher (PBKDF2), ScheduleService (kiểm tra xung đột),
│                                  RegistrationService (đăng ký học phần), AuditService (nhật ký)
├─ Infrastructure/                 CrudController<T> (CRUD AJAX dùng chung), TkbHelper
├─ Controllers/                    Account, Home, Timetable, Registration, Teaching, Reports, Logs,
│                                  CatalogControllers (Rooms, Subjects, Semesters, Faculties, Classes,
│                                  Lecturers, Students, Sections, Schedules, Users)
├─ Controllers/Api/                Web API RESTful
├─ Views/                          Razor views (mỗi danh mục có _Table, _Form, _Filter)
├─ Resources/                      SharedResource.vi.resx / .en.resx (quốc tế hóa)
└─ wwwroot/                        site.css, site.js (AJAX, CRUD modal), timetable.js (lưới TKB)
```

## 6. Đối chiếu với yêu cầu của đề
| Yêu cầu | Cài đặt |
|---|---|
| Template, Responsive | Bootstrap 5, `_Layout.cshtml`, bảng bọc `table-responsive`, menu thu gọn |
| Paging, Filtering | `PaginatedList<T>` (Skip/Take phía server) + bộ lọc theo từ khóa, học kỳ, giảng viên, phòng, thứ, lớp… |
| AJAX / jQuery | Lọc, phân trang, thêm/sửa/xóa trong modal, kiểm tra xung đột lịch, lưới TKB, đăng ký học phần |
| CRUD | 10 màn hình danh mục qua `CrudController<T>` |
| Session, Cookie | Cookie xác thực (ghi nhớ 7 ngày), Session lưu UserId/Role/FullName, cookie ngôn ngữ |
| Annotation | `[Required] [StringLength] [Range] [EmailAddress] [Display] [DataType]` trong `Models/Entities.cs` |
| Web API RESTful | Xem mục 7 |
| Bảo mật | Băm PBKDF2, `[Authorize(Roles)]`, chống CSRF (token cho form + header cho AJAX), chống XSS (Razor mã hóa), EF tham số hóa (chống SQL Injection), khóa tài khoản sau 5 lần sai (10 phút), cookie HttpOnly, header bảo mật |
| Quốc tế hóa | Nút VI/EN; menu, nhãn chung, thông báo chính dùng `IStringLocalizer<SharedResource>` |
| Kiểm tra xung đột | `ScheduleService.KiemTraAsync`: trùng phòng, trùng giảng viên, trùng lịch sinh viên, lịch bận GV, sức chứa phòng, loại phòng (thực hành), tiết ≤ 12 |

## 7. Web API
| Method | Endpoint | Quyền |
|---|---|---|
| GET | `/api/schedules?hocKy=&gv=&phong=&page=&size=` | Đã đăng nhập |
| GET | `/api/schedules/{id}` | Đã đăng nhập |
| POST | `/api/schedules` (201 / 409 Conflict) | Giáo vụ, Admin |
| PUT | `/api/schedules/{id}` (204 / 409) | Giáo vụ, Admin |
| DELETE | `/api/schedules/{id}` (204) | Giáo vụ, Admin |
| GET | `/api/students/{maSV}/timetable?maHK=&tuan=` | SV (của mình), Giáo vụ, Admin |
| GET | `/api/lecturers/{maGV}/timetable?maHK=&tuan=` | GV (của mình), Giáo vụ, Admin |
| GET | `/api/rooms/available?maHK=&thu=&tiet=&soTiet=&tuan=` | Giáo vụ, Admin |
| GET | `/api/csrf` | Đã đăng nhập |

API dùng cookie đăng nhập. **Gọi từ Postman:** (1) POST form `/Account/Login` (lấy token từ trang đăng nhập) hoặc đăng nhập bằng trình duyệt rồi dùng cùng cookie;
(2) với POST/PUT/DELETE, gọi `GET /api/csrf` để lấy token rồi gửi header `RequestVerificationToken: <token>`.
Body mẫu POST `/api/schedules`:
```json
{ "maLHP": "LHP01", "maPhong": 5, "thu": 4, "tietBatDau": 7, "soTiet": 3, "tuanBatDau": 1, "tuanKetThuc": 15 }
```

## 8. Lưu ý
- Khi triển khai thật: bật HTTPS, đặt `CookieSecurePolicy.Always` trong `Program.cs`, đổi mật khẩu các tài khoản mẫu.
- Xuất báo cáo: CSV (mở bằng Excel). PDF: dùng nút "In / Lưu PDF" của trình duyệt (đã có CSS in).
- Nhãn của các biểu mẫu danh mục lấy từ `[Display(Name=...)]` (tiếng Việt); muốn đa ngôn ngữ hóa thêm, bổ sung khóa vào `Resources/*.resx` và dùng `@L["Khoa"]` trong view.
