# TOEIC Reading backend

ASP.NET Boilerplate 10.2.0, ASP.NET Core MVC/Razor/jQuery, .NET 9, EF Core 9.0.5 và SQL Server. Giữ nguyên solution và các layer của template. Cổng giảng viên và Reading API nằm trong project Web.Mvc; Web.Host là host riêng của template.

## Phạm vi source

Repository được chuẩn bị để chỉ chứa `backend/` và `frontend/`. Không kèm database thật, tài liệu/Word/PDF, script và log ở thư mục gốc. SQL nghiệp vụ đã nhúng trong EF migrations nên tạo schema không phụ thuộc thư mục `database/` bên ngoài.

Các `appsettings*.json` đang dùng và docker-compose.yml có thông tin môi trường được bỏ qua Git. Các file `.example.json` / `docker-compose.example.yml` chỉ chứa placeholder; không có connection string thật hoặc khóa JWT đang sử dụng. Cấu hình trên máy hiện tại không bị sửa/xóa khi chuẩn bị upload.

## Build

Cài .NET SDK 9, Node.js và Yarn Classic để build assets MVC.

Tại `backend/`:

```powershell
dotnet restore .\LeHoangMy_1381.MyProject.sln
dotnet build .\LeHoangMy_1381.MyProject.sln --no-restore
Set-Location .\src\LeHoangMy_1381.MyProject.Web.Mvc
yarn install --frozen-lockfile
yarn gulp build
```

Các thư viện frontend từ node_modules được khôi phục bằng package.json/yarn.lock; không upload node_modules hoặc output .NET. Giữ assets ứng dụng, fonts, views và migration resources.

## Cấu hình và chạy một bản clone mới

Tại `backend/`, sao chép bản mẫu thành cấu hình local (không chạy lệnh sao chép ghi đè nếu đã có cấu hình riêng):

```powershell
$mvc = '.\src\LeHoangMy_1381.MyProject.Web.Mvc'
Copy-Item "$mvc\appsettings.example.json" "$mvc\appsettings.json"
Copy-Item "$mvc\appsettings.Development.example.json" "$mvc\appsettings.Development.json"
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:ConnectionStrings__Default = '<SQL_SERVER_CONNECTION_STRING_FOR_YOUR_OWN_DATABASE>'
$randomBytes = New-Object byte[] 48
$randomSource = [System.Security.Cryptography.RandomNumberGenerator]::Create()
$randomSource.GetBytes($randomBytes)
$randomSource.Dispose()
$env:Authentication__JwtBearer__SecurityKey = [Convert]::ToBase64String($randomBytes)
```

Thay placeholder connection bằng cấu hình SQL Server của bạn, giữ bảo mật TLS phù hợp môi trường. Khóa JWT được tạo ngẫu nhiên trong session; không đưa khóa hoặc connection string vào commit. Để giữ phiên JWT qua lần khởi động khác, lưu khóa bằng cơ chế cấu hình riêng/user secrets, không dùng nguyên placeholder mẫu.

Tạo schema trên **database mới mà bạn đã chọn**, chỉ sau khi kiểm tra đúng target:

```powershell
dotnet tool install dotnet-ef --version 9.0.5 --tool-path .\.tools
& .\.tools\dotnet-ef database update --project .\src\LeHoangMy_1381.MyProject.EntityFrameworkCore --startup-project .\src\LeHoangMy_1381.MyProject.Web.Mvc --connection $env:ConnectionStrings__Default
dotnet run --project .\src\LeHoangMy_1381.MyProject.Web.Mvc --launch-profile ToeicDevelopment
```

Không reset/xóa hoặc tự migrate database đang có dữ liệu khi chưa được phép. ABP tự seed Identity vào database được cấu hình khi app khởi động. Các giá trị mật khẩu mặc định trong seed/test là dữ liệu mẫu công khai của template, không phải cấu hình bí mật của người triển khai; cần quản lý tài khoản phù hợp trước khi đưa môi trường cho người khác truy cập.

- Giảng viên: http://localhost:5100/Reading
- Flutter gọi API cùng host; CORS Development mẫu cho http://localhost:5300.
- Development/OpenAccess vào thẳng giao diện theo chế độ demo đã thiết kế. API vẫn dùng bearer/quyền/ownership. Production không cấp phiên demo.
- Database mới ban đầu chưa có đề: dùng Word có tag của bạn để import, rà soát đáp án/hình ảnh rồi publish. Word/PDF không nằm trong phạm vi upload source lần này.

Nếu dùng Web.Host hoặc Migrator riêng, sao chép file example tại đúng project tương ứng và cấp cấu hình local. Docker compose mẫu lấy connection từ biến môi trường thay vì chứa thông tin SQL sẵn.

## Kiểm thử và giới hạn

```powershell
dotnet test .\LeHoangMy_1381.MyProject.sln
```

Các tests `TaggedReadingParserTests` dùng Word thật, tìm `docs/TOEIC_Reading_v06_100_cau_co_TAG.docx` tại thư mục cha. Vì người dùng yêu cầu không upload tài liệu, bộ test này cần cung cấp fixture DOCX riêng sau clone; không mặc định nó PASS khi fixture chưa có. Build solution không cần Word này. Các kiểm thử HTTP/SQL ngoài hai thư mục source không nằm trong commit.

Đợt chuẩn bị push đã restore/build solution và chạy Flutter pub get/analyze; kết quả build không thay cho nghiệm thu mọi chức năng. Kiến trúc/API/nghiệp vụ không được sửa trong bước đóng gói source.
