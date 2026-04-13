# InteractHub 🌐

Ứng dụng mạng xã hội full-stack — giao diện giống Facebook, backend ASP.NET Core 8.

---

## Kiến trúc tổng quan

```
┌──────────────────────────────────────────────────────────────┐
│                        CLIENT BROWSER                        │
│           React 18 + TypeScript + Tailwind CSS               │
│  ┌──────────┐ ┌──────────┐ ┌───────────┐ ┌──────────────┐  │
│  │ HomePage │ │ Profile  │ │  Friends  │ │ Notifications│  │
│  └──────────┘ └──────────┘ └───────────┘ └──────────────┘  │
│  ┌────────────────────────────────────────────────────────┐  │
│  │          Axios HTTP Client  +  JWT Interceptor         │  │
│  └────────────────────────────────────────────────────────┘  │
└──────────────────────┬───────────────────────────────────────┘
                       │ HTTP/REST + JSON
                       │ (Bearer JWT token)
┌──────────────────────▼───────────────────────────────────────┐
│                    ASP.NET Core 8 Web API                     │
│                                                              │
│  ┌─────────────────────────────────────────────────────────┐ │
│  │                  MIDDLEWARE PIPELINE                    │ │
│  │  HTTPS → CORS → Authentication (JWT) → Authorization   │ │
│  └─────────────────────────────────────────────────────────┘ │
│                                                              │
│  ┌─────────────────────────────────────────────────────────┐ │
│  │                    CONTROLLERS                          │ │
│  │  AuthController  │ PostController  │ CommentController  │ │
│  │  FriendController│ StoryController │ UserController     │ │
│  │  NotificationController           │ ReportController    │ │
│  └──────────────────────┬──────────────────────────────────┘ │
│                         │ gọi                                │
│  ┌──────────────────────▼──────────────────────────────────┐ │
│  │                     SERVICES                            │ │
│  │  AuthService  │ PostService    │ CommentService         │ │
│  │  FriendService│ StoryService   │ UserService            │ │
│  │  NotificationService                                    │ │
│  └──────────────────────┬──────────────────────────────────┘ │
│                         │ EF Core                            │
│  ┌──────────────────────▼──────────────────────────────────┐ │
│  │                   AppDbContext                          │ │
│  └──────────────────────┬──────────────────────────────────┘ │
└─────────────────────────┼────────────────────────────────────┘
                          │
              ┌───────────▼──────────┐
              │     SQL Server       │
              │   InteractHubDb      │
              └──────────────────────┘
```

---

## Cấu trúc thư mục

```
InteractHub/
│
├── backend/
│   ├── InteractHub.sln
│   ├── InteractHub.API/
│   │   ├── Controllers/
│   │   │   └── Controllers.cs          ← 8 controllers trong 1 file
│   │   ├── Data/
│   │   │   └── AppDbContext.cs         ← EF Core DbContext
│   │   ├── DTOs/
│   │   │   └── DTOs.cs                 ← Tất cả DTOs
│   │   ├── Models/
│   │   │   └── Models.cs               ← 9 entities
│   │   ├── Services/
│   │   │   ├── AuthService.cs          ← Đăng ký / đăng nhập / JWT
│   │   │   ├── PostService.cs          ← CRUD bài đăng, like, search
│   │   │   └── OtherServices.cs        ← Comment, Friend, Story, Notif, User
│   │   ├── Program.cs                  ← Cấu hình toàn bộ app
│   │   └── appsettings.json
│   │
│   └── InteractHub.Tests/
│       └── ServiceTests.cs             ← 20+ unit tests (xUnit + Moq)
│
└── frontend/
    ├── src/
    │   ├── components/
    │   │   ├── Navbar.tsx              ← Thanh điều hướng giống Facebook
    │   │   └── ui/
    │   │       └── Avatar.tsx          ← Avatar component
    │   ├── contexts/
    │   │   └── AuthContext.tsx         ← Global auth state
    │   ├── pages/
    │   │   ├── HomePage.tsx            ← Feed + tạo bài + infinite scroll
    │   │   ├── AuthPages.tsx           ← Login + Register (React Hook Form)
    │   │   └── ProfileAndFriends.tsx   ← Trang cá nhân + bạn bè
    │   ├── services/
    │   │   └── api.ts                  ← Axios service layer
    │   ├── types/
    │   │   └── index.ts                ← TypeScript interfaces
    │   ├── App.tsx                     ← Routes + layout
    │   └── main.tsx
    └── (config files)
```

---

## Database Schema (9 Entities)

```
AppUser ──────┬──< Post >──────┬──< Comment
(IdentityUser)│                ├──< Like
              │                ├──< PostHashtag >── Hashtag
              │                └──< PostReport
              ├──< Story
              ├──< Notification
              └──< Friendship (Sender / Receiver)
```

| Entity       | Mô tả |
|---|---|
| `AppUser`    | Người dùng, mở rộng từ IdentityUser |
| `Post`       | Bài đăng (soft delete với IsDeleted) |
| `Comment`    | Bình luận (soft delete) |
| `Like`       | Like — unique theo (UserId, PostId) |
| `Hashtag`    | Từ khoá — unique theo Name |
| `PostHashtag`| Bảng nối Post ↔ Hashtag (many-to-many) |
| `Story`      | Story 24h |
| `Notification`| Thông báo in-app |
| `Friendship` | Kết bạn với Status: pending / accepted / rejected |
| `PostReport` | Báo cáo bài đăng |

---

## API Endpoints

| Method | Endpoint | Mô tả | Auth |
|---|---|---|---|
| POST | `/api/auth/register` | Đăng ký | — |
| POST | `/api/auth/login` | Đăng nhập → JWT | — |
| GET | `/api/auth/me` | Thông tin user hiện tại | ✅ |
| GET | `/api/posts/feed` | News feed (bạn bè + bản thân) | ✅ |
| GET | `/api/posts/user/{id}` | Bài viết của 1 user | ✅ |
| GET | `/api/posts/search?keyword=` | Tìm kiếm bài viết | ✅ |
| POST | `/api/posts` | Tạo bài viết | ✅ |
| PUT | `/api/posts/{id}` | Sửa bài viết | ✅ |
| DELETE | `/api/posts/{id}` | Xóa bài viết | ✅ |
| POST | `/api/posts/{id}/like` | Like / Unlike | ✅ |
| GET | `/api/posts/{id}/comments` | Xem bình luận | ✅ |
| POST | `/api/posts/{id}/comments` | Thêm bình luận | ✅ |
| DELETE | `/api/posts/{id}/comments/{cid}` | Xóa bình luận | ✅ |
| GET | `/api/friends` | Danh sách bạn bè | ✅ |
| GET | `/api/friends/requests` | Lời mời đang chờ | ✅ |
| POST | `/api/friends/request` | Gửi lời mời kết bạn | ✅ |
| PUT | `/api/friends/request/{id}?accept=` | Chấp nhận / từ chối | ✅ |
| DELETE | `/api/friends/{id}` | Xóa bạn bè | ✅ |
| GET | `/api/stories` | Story feed | ✅ |
| POST | `/api/stories` | Tạo story | ✅ |
| DELETE | `/api/stories/{id}` | Xóa story | ✅ |
| GET | `/api/notifications` | Lấy thông báo | ✅ |
| PUT | `/api/notifications/{id}/read` | Đánh dấu đã đọc | ✅ |
| PUT | `/api/notifications/read-all` | Đọc tất cả | ✅ |
| GET | `/api/users/{id}` | Xem profile | ✅ |
| PUT | `/api/users/me` | Cập nhật profile | ✅ |
| GET | `/api/users/search?keyword=` | Tìm kiếm người dùng | ✅ |
| POST | `/api/reports/{postId}` | Báo cáo bài viết | ✅ |
| GET | `/api/reports` | Xem báo cáo (Admin) | ✅ Admin |

---

## Hướng dẫn cài đặt và chạy

### Yêu cầu hệ thống

| Phần mềm | Phiên bản | Link |
|---|---|---|
| .NET SDK | 8.x | https://dotnet.microsoft.com/download |
| SQL Server | 2019+ hoặc Express | https://www.microsoft.com/sql-server |
| Node.js | 20+ | https://nodejs.org |
| Git | Bất kỳ | https://git-scm.com |

---

### Bước 1 — Clone hoặc giải nén project

```bash
# Giải nén vào thư mục không có khoảng trắng
# Ví dụ: D:\Projects\InteractHub
```

---

### Bước 2 — Cấu hình Backend

#### 2.1 Sửa connection string

Mở file `backend/InteractHub.API/appsettings.json`:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost;Database=InteractHubDb;Trusted_Connection=True;TrustServerCertificate=True;"
}
```

> **Nếu dùng SQL Server Express:** đổi `Server=localhost` → `Server=localhost\SQLEXPRESS`
>
> **Nếu dùng LocalDB:** đổi thành `Server=(localdb)\MSSQLLocalDB`

#### 2.2 Cài EF Core tools (chỉ cần làm 1 lần)

```bash
dotnet tool install --global dotnet-ef
```

#### 2.3 Tạo database và migrate

```bash
cd backend/InteractHub.API
dotnet ef database update
```

Lệnh này sẽ:
- Tạo database `InteractHubDb` trên SQL Server
- Tạo tất cả các bảng
- Seed 2 roles: `Admin` và `User`

#### 2.4 Chạy backend

```bash
dotnet run
```

Kết quả:
```
Now listening on: http://localhost:5000
Swagger UI: http://localhost:5000/swagger
```

---

### Bước 3 — Cấu hình Frontend

#### 3.1 Tạo file .env

```bash
cd frontend
copy .env.example .env      # Windows
# hoặc
cp .env.example .env         # Linux/Mac
```

Nội dung file `.env`:
```
VITE_API_URL=http://localhost:5000
```

#### 3.2 Cài packages và chạy

```bash
npm install
npm run dev
```

Kết quả:
```
Local: http://localhost:5173/
```

---

### Bước 4 — Chạy Unit Tests

```bash
cd backend/InteractHub.Tests
dotnet test --verbosity normal
```

Kết quả mong đợi:
```
Passed!  - Failed: 0, Passed: 20, Skipped: 0
```

---

## Tính năng chính

| Tính năng | Mô tả |
|---|---|
| 🔐 **Đăng ký / Đăng nhập** | JWT token, password hash với ASP.NET Core Identity |
| 📝 **Bài viết** | CRUD, hình ảnh, hashtag, soft delete |
| ❤️ **Like / Bình luận** | Like toggle, comment với soft delete |
| 👥 **Bạn bè** | Gửi / chấp nhận / từ chối / xóa kết bạn |
| 📖 **Story** | Story 24h, tự ẩn sau khi hết hạn |
| 🔔 **Thông báo** | Notification khi được like, comment, kết bạn |
| 🔍 **Tìm kiếm** | Tìm bài viết và người dùng |
| 👤 **Profile** | Xem và chỉnh sửa trang cá nhân |
| 🛡️ **Phân quyền** | Role User / Admin, bảo vệ các endpoint |
| 📱 **Responsive** | Giao diện giống Facebook, tương thích mobile |

---

## Các kỹ thuật đã sử dụng

### Backend
- **ASP.NET Core 8 Web API** — framework chính
- **Entity Framework Core 8** — ORM, LINQ queries
- **ASP.NET Core Identity** — quản lý user, password hash
- **JWT (JSON Web Tokens)** — stateless authentication
- **CORS** — cho phép React frontend gọi API
- **Soft Delete** — IsDeleted flag thay vì xóa thật
- **Async/Await** — non-blocking I/O cho mọi DB queries
- **Swagger/OpenAPI** — tự động tạo tài liệu API

### Frontend
- **React 18** — UI framework
- **TypeScript** — type safety
- **Tailwind CSS** — styling giống Facebook
- **React Hook Form** — form validation
- **Axios** — HTTP client với JWT interceptor
- **React Router v6** — routing + protected routes
- **Lazy Loading** — code splitting theo route
- **Optimistic UI** — cập nhật UI trước khi gọi API (like)
- **Infinite Scroll** — IntersectionObserver API

### Testing
- **xUnit** — test framework
- **InMemory Database** — thay thế SQL Server khi test
- **Test Isolation** — mỗi test dùng DB riêng

---

## Sử dụng Swagger để test API

1. Mở http://localhost:5000/swagger
2. Gọi `POST /api/auth/register` để tạo tài khoản
3. Gọi `POST /api/auth/login` → copy token từ response
4. Bấm nút **Authorize** (góc phải) → nhập `Bearer <token>`
5. Thử các endpoint khác

---

## Lưu ý bảo mật

- **JWT Key** trong `appsettings.json` cần đổi thành chuỗi ngẫu nhiên mạnh trước khi deploy
- **Password** không bao giờ lưu plaintext — Identity tự hash bằng PBKDF2
- Mọi endpoint (trừ register/login) đều yêu cầu JWT hợp lệ
- Role `Admin` có quyền xem báo cáo và xóa bài của người khác

---

## Troubleshooting

| Lỗi | Cách xử lý |
|---|---|
| `dotnet ef` không nhận | Chạy `dotnet tool install --global dotnet-ef` |
| Cannot connect to SQL Server | Kiểm tra connection string trong appsettings.json |
| CORS error trên frontend | Đảm bảo backend đang chạy và `.env` đúng URL |
| Package downgrade error | Các package phải cùng version, xem `.csproj` |
