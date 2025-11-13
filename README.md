# Task Management Platform - Microservices Architecture

A scalable, resilient task management system built with **.NET 8**, **MongoDB**, and **Docker**.  
Fully implements **User Management**, **Task Workflow**, **Activity Logging**, **SLA Monitoring**, and **Reporting**.

---

## Microservices & LOCAL PORTS (Visual Studio)

| Service            | Local Port | Swagger URL                          |
|--------------------|------------|--------------------------------------|
| **UserService**     | 7251       | https://localhost:7251/swagger       |
| **TaskService**     | 7252       | https://localhost:7252/swagger       |
| **ReportingService**| 7253       | https://localhost:7253/swagger       |

> **Note:** These ports are from your current `launchSettings.json` in Visual Studio.

---

## Features Implemented (100% Requirement Coverage)

### UserService
- Full CRUD (Create, Read, Update, Delete)
- `/api/auth/login` → returns stub JWT-like token: `userId_role`
- Roles: **Admin**, **Manager**, **Engineer**
- **Admin-only delete** with proper role check
- Swagger with Bearer token support

### TaskService
- Full CRUD + Delete
- Status workflow: **Open → In Progress → Blocked → Completed**
- **Activity log** on every status change
- Filters: `status`, `assigneeId`, `from`, `to`
- SLA flag: `IsOverdue` when `DueDate < today && Status != Completed`
- Token-based authorization on Update/Delete

### ReportingService
- `GET /api/reports/tasks-by-user` → Tasks per user with status breakdown
- `GET /api/reports/tasks-by-status` → Count by status
- `GET /api/reports/sla-breaches` → Overdue tasks with **DaysOverdue**
- Real-time aggregation from MongoDB
- Token-protected endpoints

---

## Technology Stack
- **.NET 8.0** (Minimal APIs + Controllers)
- **MongoDB** (shared database)
- **Docker** + **docker-compose** (optional)
- **Swagger/OpenAPI** with Bearer Auth
- **Git** with logical commits
- **Visual Studio 2022**

---

## How to Run

### Option 1: Visual Studio (Current Setup - RECOMMENDED)
1. Open `TaskManagementPlatform.sln`
2. Set **Multiple startup projects**
3. Start: **UserService**, **TaskService**, **ReportingService**
4. Press **F5**

**Swagger URLs:**
- User: https://localhost:7251/swagger
- Task: https://localhost:7252/swagger
- Report: https://localhost:7253/swagger

### Option 2: Docker (Optional - For Production Demo)
```bash
cd D:\TaskManagementPlatform\TaskManagementPlatform
docker-compose up --build