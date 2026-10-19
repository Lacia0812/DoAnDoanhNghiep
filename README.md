# Website quản lý đơn hàng và giao nước uống Gia Phú

Đồ án xây dựng hệ thống quản lý sản phẩm, khách hàng, đơn hàng, tồn kho và giao
hàng cho Công ty TNHH Sản xuất Nước uống Tinh khiết Gia Phú. Hệ thống được thiết
kế ở mức độ phù hợp với đồ án tốt nghiệp: kiến trúc dễ đọc, phân quyền theo bốn
vai trò và không phụ thuộc dịch vụ bên ngoài.

## Công nghệ

- Backend: ASP.NET Core Web API 8, Entity Framework Core, SQL Server, JWT,
  Swagger/OpenAPI và QuestPDF.
- Frontend: React, TypeScript, Vite, React Router, Axios, Ant Design,
  Ant Design Charts và Day.js.
- Kiểm thử: xUnit cho backend; Vitest và React Testing Library cho frontend.

## Cấu trúc

```text
TTDN/
├── GiaPhuWaterDelivery.sln
├── backend/
│   ├── Controllers/
│   ├── Data/
│   ├── DTOs/
│   ├── Helpers/
│   ├── Migrations/
│   ├── Models/
│   ├── Services/
│   ├── Program.cs
│   ├── appsettings.json
│   └── GiaPhuWaterDelivery.Api.csproj
├── backend.Tests/
│   ├── Data/
│   ├── DTOs/
│   ├── Helpers/
│   ├── Models/
│   ├── Services/
│   └── GiaPhuWaterDelivery.Api.Tests.csproj
├── frontend/
│   ├── src/
│   │   ├── components/
│   │   ├── contexts/
│   │   ├── pages/
│   │   ├── routes/
│   │   ├── services/
│   │   ├── test/
│   │   ├── types/
│   │   ├── utils/
│   │   ├── App.tsx
│   │   └── main.tsx
│   ├── .env.example
│   └── package.json
├── docs/
│   └── organization-data-sources.md
├── scripts/
│   └── smoke-test.ps1
└── README.md
```

## Yêu cầu môi trường

- .NET SDK hỗ trợ target `net8.0`.
- Node.js 20 trở lên và npm.
- SQL Server 2019 trở lên; cấu hình mặc định dùng SQL Server local với Windows
  Authentication.
- EF Core CLI nếu máy chưa có:

```bash
dotnet tool install --global dotnet-ef --version 8.*
```

Connection string phát triển mặc định:

```text
Server=localhost;Database=GiaPhuWaterDeliveryDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true
```

Có thể thay đổi `ConnectionStrings:DefaultConnection` trong
`backend/appsettings.Development.json` hoặc bằng biến môi trường
`ConnectionStrings__DefaultConnection`.

## Tạo cơ sở dữ liệu và chạy backend

Tại thư mục gốc dự án:

```bash
cd backend
dotnet restore
dotnet ef database update
dotnet run
```

Backend mặc định chạy tại `http://localhost:5000`. Swagger có tại:

```text
http://localhost:5000/swagger
```

Migration tạo schema; bộ khởi tạo dữ liệu chạy idempotent khi backend khởi động
và tạo tài khoản, sản phẩm, khách hàng, đơn hàng, giao hàng cùng dữ liệu biểu đồ
mẫu.

## Dữ liệu tổ chức phục vụ demo

Bộ seed có 28 doanh nghiệp, tổ chức và cơ sở giáo dục có thật, sử dụng tên cùng
thông tin liên hệ đã công khai và được xác minh ngày 27/07/2026. Các thông tin
này chỉ được đưa vào hệ thống để tạo dữ liệu trình diễn có bối cảnh thực tế; việc
xuất hiện trong bộ seed **không khẳng định các đơn vị này là khách hàng thực tế
của Gia Phú hoặc có bất kỳ quan hệ thương mại nào với dự án**. Đơn hàng, giao
dịch kho, thanh toán và giao hàng minh họa đều là dữ liệu giả lập.

Danh sách đầy đủ, nguồn công khai và lưu ý về địa chỉ hành chính được ghi tại
[Nguồn dữ liệu tổ chức phục vụ demo](docs/organization-data-sources.md). Thông
tin công khai có thể thay đổi; cần xác minh lại trước khi sử dụng ngoài mục đích
demo hoặc kiểm thử.

## Chạy frontend

Mở terminal khác:

```bash
cd frontend
copy .env.example .env
npm install
npm run dev
```

Trên macOS/Linux, dùng `cp .env.example .env`. Frontend chạy tại
`http://localhost:5173`.

## Tài khoản mẫu

| Vai trò | Tên đăng nhập | Mật khẩu |
|---|---|---|
| Quản trị viên | `admin` | `Admin@123` |
| Nhân viên bán hàng | `sales` | `Sales@123` |
| Nhân viên kho | `warehouse` | `Warehouse@123` |
| Nhân viên giao hàng | `delivery` | `Delivery@123` |

## Phân quyền

- Quản trị viên: quản lý tài khoản, sản phẩm, khách hàng; xem mọi đơn hàng,
  phân công giao hàng, xem tồn kho, công nợ, vỏ bình và dashboard.
- Nhân viên bán hàng: quản lý khách hàng; tạo, sửa, xác nhận, hủy và theo dõi
  đơn; tạo nhanh hồ sơ khách vãng lai ngay trên form bán hàng; phân công nhân
  viên giao hàng; ghi nhận thanh toán từng phần và xuất hóa đơn PDF.
- Nhân viên kho: xem đơn cần xử lý, nhập thêm tồn, xem lịch sử kho, bắt đầu
  chuẩn bị và xác nhận hàng sẵn sàng giao.
- Nhân viên giao hàng: chỉ xem các đơn được phân công; bắt đầu giao, nhập số vỏ
  khách đổi trả, xác nhận thành công/thất bại và xác nhận thanh toán khi giao
  thành công.

Admin không thay thế thao tác nghiệp vụ riêng của Sales, Warehouse hoặc
Delivery. Cả API và router frontend đều kiểm tra vai trò.

## Luồng đơn hàng

```text
Đơn mới
  ├─> Đã hủy
  └─> Đã xác nhận
        └─> Đang chuẩn bị (trừ tồn kho)
              └─> Sẵn sàng giao
                    └─> Đang giao
                          ├─> Giao thành công
                          └─> Giao thất bại (tự động hoàn kho)
```

- Chỉ đơn mới được sửa hoặc hủy.
- Xác nhận đơn kiểm tra tồn nhưng chưa giữ hàng.
- Khi bắt đầu chuẩn bị, backend kiểm tra lại và trừ tất cả sản phẩm trong một
  transaction; không trừ một phần và không cho tồn âm.
- Giao thất bại yêu cầu lý do và hoàn hàng đúng một lần.
- Chỉ đơn giao thành công được tính vào doanh thu.
- Với sản phẩm có vỏ hoàn lại, nhân viên giao nhập số vỏ khách trả; hệ thống tự
  tính tiền cọc cho số bình chưa trả vỏ và cập nhật số vỏ khách đang giữ.
- Đơn đã giao có thể thu tiền nhiều lần. Trạng thái tự chuyển `Chưa thanh toán`
  → `Thanh toán một phần` → `Đã thanh toán` và không cho thu vượt số còn nợ.

## Chức năng và màn hình

- `/login`: đăng nhập và điều hướng theo vai trò.
- `/dashboard`: tám chỉ số, doanh thu 12 tháng, trạng thái đơn, top sản phẩm và
  gợi ý nhập hàng.
- `/products`: danh sách, tìm kiếm, lọc, thêm, sửa, vô hiệu hóa, nhập tồn và cấu
  hình sản phẩm có thu hồi vỏ/tiền cọc.
- `/customers`: CRUD mềm, tìm theo tên/số điện thoại, xem số vỏ đang giữ, công
  nợ và lịch sử đơn.
- `/orders`, `/orders/create`, `/orders/:id`, `/orders/:id/edit`: quản lý và
  theo dõi toàn bộ vòng đời đơn; form tạo đơn hỗ trợ tạo và chọn ngay khách
  vãng lai với số điện thoại, địa chỉ giao hàng riêng.
- `/inventory`: tồn hiện tại, sản phẩm sắp hết, nhập kho và lịch sử thay đổi.
- `/deliveries`: danh sách giao được phân công và các thao tác giao hàng.
- `/finance`: tổng hợp công nợ, thu tiền từng phần và lịch sử giao/thu hồi vỏ.
- `/users`: quản lý, khóa/mở và đổi mật khẩu tài khoản.

Tại chi tiết đơn hàng, Admin/Sales có thể tải hóa đơn PDF gồm tiền hàng, tiền
cọc, số đã thanh toán, số còn phải thu và chi tiết vỏ bình.

Giao diện sử dụng tiếng Việt, có thông báo kết quả, hộp thoại xác nhận thao tác
nguy hiểm, màu theo trạng thái và bố cục responsive cơ bản.

## API chính

- Authentication: `/api/auth/login`, `/api/auth/profile`.
- Sản phẩm: `/api/products`, `/api/products/{id}`,
  `/api/products/{id}/add-stock`.
- Danh mục: `/api/product-categories`.
- Khách hàng: `/api/customers`, `/api/customers/{id}`,
  `/api/customers/{id}/orders`.
- Đơn hàng: `/api/orders`, `/api/orders/{id}` và các action `confirm`,
  `prepare`, `ready`, `cancel`, `assign-delivery`, `payments`; hóa đơn PDF tại
  `/api/orders/{id}/invoice`.
- Giao hàng: `/api/deliveries/my-orders`, `/api/deliveries/{id}` và các action
  `start`, `complete`, `fail`.
- Kho: `/api/inventory`, `/api/inventory/transactions`.
- Tài khoản: `/api/users`, `/api/users/{id}`, `/api/users/{id}/password`.
- Dashboard: `/api/dashboard/summary`, `revenue`, `order-status`,
  `top-products`, `restock-suggestions`.
- Công nợ: `/api/debts`, `/api/debts/summary`.
- Vỏ bình: `/api/containers/transactions`.

API danh sách trả dữ liệu phân trang theo mẫu:

```json
{
  "items": [],
  "totalCount": 0,
  "page": 1,
  "pageSize": 10,
  "totalPages": 0
}
```

## Kiểm thử và build

Khôi phục, build và chạy toàn bộ kiểm thử backend từ thư mục gốc:

```bash
dotnet restore GiaPhuWaterDelivery.sln
dotnet build GiaPhuWaterDelivery.sln
dotnet test GiaPhuWaterDelivery.sln
```

Frontend:

```bash
cd frontend
npm test
npm run build
```

Sau khi backend đang chạy, có thể kiểm tra tự động luồng xuyên suốt bốn vai trò:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\smoke-test.ps1
```

Kết quả kiểm tra tại thời điểm bàn giao:

| Hạng mục | Kết quả |
|---|---|
| Backend build | Thành công, 0 lỗi và 0 cảnh báo |
| Backend tests | 219/219 đạt |
| EF migration model | Không có thay đổi chưa tạo migration |
| Frontend tests | 18/18 đạt |
| Frontend production build | Thành công |
| SQL Server seed | Thành công với đủ dữ liệu minh họa |

Kịch bản demo khuyến nghị:

1. Sales chọn khách công ty hoặc tạo nhanh khách vãng lai, sau đó tạo và xác
   nhận đơn.
2. Warehouse bắt đầu chuẩn bị rồi xác nhận sẵn sàng giao.
3. Sales hoặc Admin phân công tài khoản Delivery.
4. Delivery bắt đầu giao, nhập số vỏ khách trả rồi hoàn tất; hệ thống tính tiền
   cọc cho số vỏ còn thiếu.
5. Sales mở **Tài chính**, xem công nợ, thu một phần hoặc toàn bộ tiền còn lại.
6. Sales/Admin mở chi tiết đơn và tải hóa đơn PDF.
7. Admin mở dashboard để xem chỉ số và doanh thu cập nhật.

Bộ seed có sẵn đơn `DEMO-REV-01` ở trạng thái thanh toán một phần, một giao dịch
vỏ bình và tiền cọc để màn hình Tài chính có dữ liệu ngay khi demo.

## Phạm vi không triển khai

Dự án không sử dụng Docker, Microservices, Redis, RabbitMQ, Machine Learning,
thanh toán trực tuyến qua cổng ngân hàng, nhiều kho, bản đồ/GPS, gửi email/SMS,
tối ưu tuyến đường hoặc permission chi tiết.
