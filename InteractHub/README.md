# InteractHub 🌐

A full-stack social media web application built with **ASP.NET Core 8** (Backend) and **React 18 + TypeScript** (Frontend).

---

## 📋 Project Overview

| Category | Details |
|---|---|
| Course | C# and .NET Development – Spring 2026 |
| Stack | ASP.NET Core 8 + React 18 + TypeScript + SQL Server |
| Auth | JWT + ASP.NET Core Identity |
| Cloud | Microsoft Azure (App Service + SQL + Blob Storage) |
| CI/CD | GitHub Actions |

---

## 🏗️ Project Structure

```
InteractHub/
├── backend/
│   ├── InteractHub.API/           # ASP.NET Core Web API
│   │   ├── Controllers/           # API Controllers (6 controllers, 25+ endpoints)
│   │   ├── Data/                  # AppDbContext + EF Core
│   │   ├── DTOs/                  # Request & Response DTOs
│   │   ├── Hubs/                  # SignalR NotificationHub
│   │   ├── Models/                # Entity classes (9 entities)
│   │   ├── Services/              # Business logic layer
│   │   │   ├── Interfaces/        # Service interfaces
│   │   │   └── *.cs               # Implementations
│   │   ├── Program.cs             # App configuration
│   │   └── appsettings.json       # Configuration
│   └── InteractHub.Tests/         # xUnit test project (15+ tests)
│
├── frontend/
│   └── src/
│       ├── components/
│       │   ├── layout/            # Navbar
│       │   ├── posts/             # PostCard, CreatePost, Comments
│       │   ├── stories/           # StoriesBar
│       │   └── ui/                # Button, Input, Avatar, Modal, etc.
│       ├── contexts/              # AuthContext (global state)
│       ├── hooks/                 # Custom hooks (usePosts, useComments, etc.)
│       ├── pages/                 # HomePage, ProfilePage, FriendsPage, Auth pages
│       ├── services/              # Axios API service layer
│       └── types/                 # TypeScript interfaces
│
└── .github/
    └── workflows/
        └── ci-cd.yml              # GitHub Actions pipeline
```

---

## 🚀 Getting Started

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [Node.js 20+](https://nodejs.org)
- [SQL Server](https://www.microsoft.com/sql-server) (or SQL Server Express)
- [Git](https://git-scm.com)

---

### Backend Setup

```bash
cd backend/InteractHub.API

# 1. Update connection string in appsettings.json
# "DefaultConnection": "Server=localhost;Database=InteractHubDb;..."

# 2. Run database migrations (auto-runs on startup, or manually):
dotnet ef database update

# 3. Start the API
dotnet run

# API runs at: https://localhost:5000
# Swagger UI:  https://localhost:5000/swagger
```

**appsettings.json** – key settings to configure:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=InteractHubDb;Trusted_Connection=True;TrustServerCertificate=True;"
  },
  "Jwt": {
    "Key": "YourSecretKeyMinimum32Characters!!",
    "Issuer": "InteractHub",
    "Audience": "InteractHubClient",
    "ExpiresHours": "24"
  }
}
```

---

### Frontend Setup

```bash
cd frontend

# 1. Install dependencies
npm install

# 2. Create .env file
echo "VITE_API_URL=http://localhost:5000" > .env

# 3. Start dev server
npm run dev

# App runs at: http://localhost:5173
```

---

### Run Tests

```bash
cd backend/InteractHub.Tests
dotnet test --collect:"XPlat Code Coverage"
```

---

## 🗄️ Database Entities (9 total)

| Entity | Description |
|---|---|
| **AppUser** | Extended IdentityUser with FullName, Bio, Avatar |
| **Post** | User posts with content, image, hashtags |
| **Comment** | Comments on posts |
| **Like** | User likes on posts (unique per user/post) |
| **Friendship** | Friend requests with status (pending/accepted) |
| **Story** | 24-hour temporary stories |
| **Notification** | In-app notifications |
| **Hashtag** | Post hashtags (many-to-many via PostHashtag) |
| **PostReport** | Content moderation reports |

---

## 🔌 API Endpoints (25+)

### Auth
| Method | Endpoint | Description |
|---|---|---|
| POST | `/api/auth/register` | Register new user |
| POST | `/api/auth/login` | Login → returns JWT |
| GET | `/api/auth/me` | Get current user info |

### Posts
| Method | Endpoint | Description |
|---|---|---|
| GET | `/api/posts/feed` | Paginated friend feed |
| GET | `/api/posts/search?q=` | Search posts |
| GET | `/api/posts/{id}` | Get single post |
| GET | `/api/posts/user/{userId}` | Get user's posts |
| POST | `/api/posts` | Create post |
| PUT | `/api/posts/{id}` | Update post |
| DELETE | `/api/posts/{id}` | Delete post |
| POST | `/api/posts/{id}/like` | Toggle like |

### Comments, Friends, Stories, Notifications, Reports → see Swagger UI

---

## 🔒 Authentication Flow

1. User registers/logs in → receives JWT token
2. Token stored in `localStorage`
3. Axios interceptor adds `Authorization: Bearer <token>` to all requests
4. On 401 response → auto-redirect to `/login`
5. Protected routes in React Router check `isAuthenticated` from `AuthContext`

---

## ☁️ Azure Deployment

### Resources needed
- **Azure App Service** – Backend API
- **Azure SQL Database** – Production database
- **Azure Blob Storage** – Image uploads
- **Azure Static Web Apps** – Frontend

### GitHub Secrets required
```
AZURE_BACKEND_APP_NAME       # App Service name
AZURE_BACKEND_PUBLISH_PROFILE # From Azure Portal
AZURE_STATIC_WEB_APPS_TOKEN  # Static Web Apps token
AZURE_BACKEND_URL             # https://your-app.azurewebsites.net
```

### Deploy
Push to `main` branch → GitHub Actions automatically builds, tests, and deploys.

---

## ✅ Assignment Coverage

| Requirement | Status |
|---|---|
| F1: React Component Architecture | ✅ 15+ components, TypeScript interfaces, custom hooks |
| F2: State Management & API Integration | ✅ AuthContext, Axios service layer, interceptors |
| F3: React Forms & Validation | ✅ React Hook Form, password strength, real-time validation |
| F4: Routing, Protected Routes, Dynamic Features | ✅ React Router v6, lazy loading, infinite scroll, SignalR |
| B1: Database Design & EF Core | ✅ 9 entities, relationships, migrations, seed data |
| B2: RESTful API Controllers & DTOs | ✅ 6 controllers, 25+ endpoints, Swagger |
| B3: JWT Authentication & Authorization | ✅ JWT, Identity, roles, [Authorize] |
| B4: Business Logic & Services Layer | ✅ 6 services, interfaces, DI, SOLID |
| T1: Unit Testing | ✅ 15+ xUnit tests, Moq, InMemory DB |
| D1: Azure + CI/CD | ✅ GitHub Actions pipeline, Azure deployment |
