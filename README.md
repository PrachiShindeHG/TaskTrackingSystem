# Task Management Platform - Full-Stack Microservices

A scalable, resilient, and production-ready **Task Management System** built with **.NET 8**, **MongoDB**, **Angular 17+**, and **Docker**.

Fully implements **User Management**, **Task Workflow**, **Activity Logging**, **SLA Monitoring**, **Reporting**, and **Real-time Charts**.

---

## Microservices & Local Ports (Visual Studio)

| Service             | Local Port | Swagger URL                            |
|---------------------|------------|----------------------------------------|
| **UserService**     | 7251       | https://localhost:7251/swagger         |
| **TaskService**     | 7252       | https://localhost:7252/swagger         |
| **ReportingService**| 7253       | https://localhost:7253/swagger         |

> Note: These ports are from your current `launchSettings.json` in Visual Studio.

---

## Features Implemented (100% Requirement Coverage)

### UserService
- Full CRUD (Create, Read, Update, Delete)
- `/api/auth/login` → returns JWT-like token: `userId_role`
- Roles: **Admin**, **Manager**, **Employee**
- **Admin-only delete** with proper role check
- Swagger with Bearer token support

### TaskService
- Full CRUD + Delete (Admin-only)
- Status workflow: **New → Open → In Progress → Blocked → Completed**
- **Activity log** on every status change
- Filters: `status`, `assigneeId`, `from`, `to`
- SLA flag: `IsOverdue` when `DueDate < today && Status != Completed`
- Token-based authorization

### ReportingService
- `GET /api/reports/tasks-by-user` → Tasks per user with status breakdown
- `GET /api/reports/tasks-by-status` → Count by status
- `GET /api/reports/sla-breaches` → Overdue tasks with **DaysOverdue**
- Real-time aggregation from MongoDB

### Frontend (Angular 17+)
- Modern SPA with standalone components
- Role-based UI (Admin sees Users tab)
- Dashboard with live stats
- Task list with filters, edit, delete (Admin-only)
- Interactive Reports with **Highcharts**
- Export Reports to PDF
- Responsive Material Design

---

## Technology Stack

| Layer        | Technology                          |
|-------------|-------------------------------------|
| Backend     | .NET 8.0 (Minimal APIs + Controllers) |
| Database    | MongoDB                             |
| Frontend    | Angular 17+, TypeScript, Material, Highcharts |
| Container   | Docker + Docker Compose             |
| Auth        | JWT-like token (`userId_role`)      |
| Docs        | Swagger/OpenAPI                     |

---

## How to Run

### Option 1: Visual Studio (Recommended for Development)

1. Open `TaskManagementPlatform.sln`
2. Set **Multiple startup projects**:
   - UserService
   - TaskService
   - ReportingService
3. Press **F5**

Swagger URLs:
- User: https://localhost:7251/swagger
- Task: https://localhost:7252/swagger
- Report: https://localhost:7253/swagger

### Option 2: Docker (Full Stack - Production Ready)

```bash
# From project root
cd D:\TaskManagementPlatform

# First time / after changes
docker-compose up --build

# Normal run (faster)
docker-compose up