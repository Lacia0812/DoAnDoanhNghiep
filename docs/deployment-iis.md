# Triển khai Gia Phú trên IIS

## 1. Chuẩn bị máy chủ

Cài SQL Server, .NET 8 Hosting Bundle, IIS URL Rewrite và IIS Management Console. Tạo hai website (hoặc hai application): một cho API, một cho frontend. App Pool của API đặt `No Managed Code`, 64-bit và tài khoản có quyền đọc/ghi thư mục `wwwroot/uploads`.

## 2. Cấu hình bí mật

Không sửa bí mật trực tiếp trong Git. Cấu hình các biến môi trường cho App Pool/API:

```text
ASPNETCORE_ENVIRONMENT=Production
ConnectionStrings__DefaultConnection=Server=SQLSERVER;Database=GiaPhuWaterDeliveryDb;Trusted_Connection=True;TrustServerCertificate=True
Jwt__Key=<chuỗi ngẫu nhiên tối thiểu 32 ký tự>
Jwt__Issuer=GiaPhuWaterDelivery
Jwt__Audience=GiaPhuWaterDeliveryClient
Cors__AllowedOrigins__0=https://ten-mien-frontend.example
```

Sau khi đổi biến môi trường, recycle App Pool. Nếu dùng Windows Authentication đến SQL Server, cấp quyền database cho identity của App Pool; nếu không, dùng tài khoản SQL trong connection string và lưu bằng IIS Configuration Editor/biến môi trường.

## 3. Publish backend và migration

```powershell
dotnet restore
dotnet test GiaPhuWaterDelivery.sln -c Release
dotnet publish backend/GiaPhuWaterDelivery.Api.csproj -c Release -o C:\Deploy\GiaPhuApi

$env:ASPNETCORE_ENVIRONMENT = "Production"
$env:ConnectionStrings__DefaultConnection = "<connection string>"
$env:Jwt__Key = "<secret>"
dotnet ef database update --project backend --startup-project backend
```

Trỏ IIS API site vào `C:\Deploy\GiaPhuApi`. Production không tự chạy migration khi khởi động nhằm tránh hai instance cùng thay đổi schema.

## 4. Build frontend

Tạo `frontend/.env.production` (không commit):

```text
VITE_API_URL=https://api.example.com/api
```

Sau đó:

```powershell
Set-Location frontend
npm ci
npm run build
```

Trỏ website frontend vào thư mục `frontend/dist`. Tệp `public/web.config` đã cấu hình fallback cho React Router; cần cài IIS URL Rewrite.

## 5. Sao lưu và vận hành

Chạy thủ công hoặc lên lịch Task Scheduler bằng tài khoản có quyền `BACKUP DATABASE`:

```powershell
.\scripts\backup-database.ps1 -ServerInstance localhost -Database GiaPhuWaterDeliveryDb -OutputDirectory D:\SqlBackups\GiaPhu
```

Script tạo `.bak` có timestamp và chạy `RESTORE VERIFYONLY`. Nên sao chép bản sao lưu sang thiết bị/vùng lưu trữ khác, áp dụng chính sách giữ bản và diễn tập khôi phục định kỳ. Trước mỗi lần triển khai: sao lưu database, dừng App Pool, chạy migration, thay file publish, khởi động lại và kiểm tra `/api/auth/login`.

## 6. Checklist sau triển khai

- HTTPS hợp lệ cho frontend và API; chỉ origin thật được khai báo trong CORS.
- Thư mục upload có quyền ghi nhưng không cho thực thi script.
- Tài khoản mẫu đã đổi mật khẩu hoặc vô hiệu hóa.
- Backup tự động chạy thành công và có cảnh báo khi thất bại.
- Kiểm tra bốn vai trò, xuất Excel, tải hóa đơn, ảnh bằng chứng và luồng giao hàng đầy đủ.
