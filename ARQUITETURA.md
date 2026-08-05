# Arquitetura — IndustrialOS

## Visao geral
SaaS multi-tenant. Frontend PWA (React/TS) offline-first consumindo API REST (.NET 10)
com PostgreSQL, midia em Cloudflare R2, cache Redis, PDFs com QuestPDF, tudo em Docker.

## Backend (.NET 10 LTS — Clean Architecture)
- **Domain**: entidades, value objects, regras de dominio puras (formulas de avanco,
  medicao, farol, curva S). Sem dependencia de infra.
- **Application**: casos de uso (CQRS opcional com MediatR), DTOs, validacoes
  (FluentValidation), interfaces de repositorio/servicos.
- **Infrastructure**: EF Core (PostgreSQL), repositorios, R2 (S3 SDK), Redis, JWT,
  QuestPDF, envio de e-mail, storage.
- **API (WebApi)**: controllers/minimal APIs, autenticacao JWT, Swagger, middlewares
  (tenant resolver, tratamento de erro RFC7807, logging, rate limit).
- Padroes: Repository + Unit of Work, Dependency Injection, Result pattern.

## Multi-tenant
tenant_id em todas as entidades; Global Query Filter no EF; tenant resolvido do claim
do JWT (middleware). Migrations versionadas. Isolamento total de dados.

## Frontend (React + TS + Vite + Tailwind)
- Mobile-first, componentizado, PWA (service worker), estado com Zustand/Redux Toolkit,
  data-fetching com React Query.
- **Offline-first**: IndexedDB (Dexie) guarda RDOs/rascunhos e uma **fila de
  sincronizacao**; ao reconectar, envia em lote (endpoint idempotente) e resolve
  conflitos (last-write + versao/etag).
- Camera/GPS nativos; upload de midia direto ao R2 (URL pre-assinada).

## Integracoes / Infra
- **R2** (S3-compat) para fotos/videos/PDF/documentos, com thumbnails.
- **Redis** para cache de dashboards/agregacoes e sessoes.
- **QuestPDF** para RDO e boletim.
- **Docker Compose** (api, db, redis) + CI/CD (GitHub Actions) + observabilidade
  (Serilog/OpenTelemetry).

## Preparacao para IA (nao implementar)
Camada de eventos/outbox (RDO enviado, medicao emitida) + dados normalizados,
permitindo futuros servicos de resumo/previsao/NLP sem reescrever o nucleo.

## Autenticacao (reconciliacao PIN x senha — do MVP para o SaaS)
Modelo unico: o usuario e autenticado por **e-mail/nome + senha (hash) -> JWT** ao
registrar o dispositivo. O **PIN pessoal** do MVP e preservado como **atalho rapido de
campo** (desbloqueio/troca de usuario no mesmo dispositivo ja autenticado), NAO como
credencial primaria. Acoes destrutivas/financeiras exigem senha de administrador separada.

## Midia
Fotos e **videos** (rdo_midia, tipo foto|video) com upload direto ao R2 e thumbnails.

## Seguranca
JWT (access+refresh), hash argon2/bcrypt, RBAC por policy, auditoria (tabela de logs),
LGPD (consentimento, exclusao), URLs de midia assinadas e temporarias.
