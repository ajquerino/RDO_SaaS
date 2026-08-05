# IndustrialOS

SaaS multi-tenant de gestão operacional de obras industriais. Sprint 0 — fundação.

## Estrutura
- `backend/` — .NET 10 (Clean Architecture: Domain / Application / Infrastructure / Api). JWT, Swagger, health check, multi-tenant (Global Query Filter + TenantMiddleware).
- `frontend/` — React + TS + Vite + Tailwind, PWA (offline-first), mobile-first.
- `docker-compose.yml` — Postgres + Redis + API.

## Rodar tudo (Docker)
```bash
docker compose up --build
```
API em http://localhost:8080 (Swagger em `/swagger`, health em `/health`).

## Backend (local)
```bash
cd backend && dotnet run --project src/IndustrialOS.Api
```

## Frontend (local)
```bash
cd frontend && npm install && npm run dev
```
App em http://localhost:5173.

## Próximo (Sprint 1)
Auth (login/refresh, hash de senha), RBAC, Empresas/Usuários, vínculo usuário-obra.
Migrations EF e mapeamento das entidades conforme `../DDL_POSTGRES.sql` e `../MODELO_DADOS_POSTGRES.md`.
