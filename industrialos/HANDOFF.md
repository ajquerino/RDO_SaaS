# HANDOFF — IndustrialOS (continuar em outra máquina)

> Este arquivo permite ao Claude Code retomar o projeto de onde paramos. Leia-o inteiro ao abrir o projeto numa máquina nova. Os documentos de referência estão na pasta acima (`../PRD.md`, `../ARQUITETURA.md`, `../MODELO_DADOS_POSTGRES.md`, `../DDL_POSTGRES.sql`, `../ROADMAP_SPRINTS.md`, `../ANALISE_DO_MVP.md`, `../AUDITORIA_DOCS_VS_MVP.md`, `../PERMISSOES.md`, etc.).

## O que é
SaaS multi-tenant de **Gestão Operacional de Obras Industriais** (montagem, caldeiraria, tubulação, pintura…). O **RDO** (Relatório Diário de Obra) é o módulo central. Evolução de um MVP validado (`github.com/ajquerino/RDO`, HTML/Apps Script) para arquitetura moderna. **Não escrever código antes de aprovar documentação; nunca supor regra de negócio — perguntar ao usuário.**

## Stack
- **Backend:** .NET 10 (Clean Architecture: Domain/Application/Infrastructure/Api), EF Core, JWT, Swagger, multi-tenant (Global Query Filter + TenantMiddleware). Em `backend/`.
- **Frontend:** React + TS + Vite + Tailwind + PWA, React Query + zustand, mobile-first. Em `frontend/`.
- **Banco:** PostgreSQL. **Armazenamento:** Cloudflare R2 (S3-compat). **PDF:** QuestPDF. **Import:** ClosedXML.

## Estado (Sprints)
- **0 Fundação** ✅ · **1 Auth/RBAC/Usuários** ✅ · **2 Clientes/Obras/EAP/importar cronograma** ✅ · **3 RDO núcleo + PDF** ✅
- **4 RDO parte 2** 🟡 — FEITO: fotos/vídeos (R2), catálogo de 61 funções (Direta/Indireta), clima por GPS, hora extra automática, seções (retrabalho, segurança, dificuldades, próximo dia, pendências, planejamento), PDF completo com todas as seções. **FALTA: Assinaturas (canvas — campo backend `rdos.Assinaturas` jsonb já existe), Offline-first (IndexedDB + fila de sync).**
- **5 Aprovação por token + revisão** 🟡 (o "Finalizar" já gera `tokenAprovacao`; falta endpoint público `/aprovacao/{token}` + tela do fiscal Aprovar/Solicitar revisão).
- **6 Dashboards/Curva S/Faróis/Pareto** ❌ (avanço por item já calculado em `AvancoCalculo`; falta agregar + telas).
- **7 Medição/Faturamento/Custos** ❌ (modelo desenhado; obras já têm valor e prazo de pagamento).
- **8 Equipamentos/Produtividade/Documentos** ❌ · **9 Hardening/LGPD/billing** ❌ · **IA** (fase futura).

## Regras de negócio já confirmadas pelo usuário
- **Hora extra** (jornada única por RDO): expediente seg–sex 07:00–16:48 = 8h48 (almoço descontado pelo horário real). Extra dia útil = após 16:48 (**50% até 10h/semana, 70% acima** — aplicar no fechamento semanal). Sáb/Dom/**Feriado** (checkbox) = **100% até 8h, 150% acima**. Cálculo no backend em `Domain/Services/JornadaCalculo.cs` (e espelhado no front em `Rdo.tsx`).
- **RBAC:** gerir Obra/Contrato/EAP/Clientes/importar/vincular/faturamento + ver valores R$ + ver/editar RDOs de TODAS as obras = **Planejador + Gestor + Admin**. Criar usuário = só Gestor+Admin. Encarregado/Líder/Supervisor: só obras vinculadas, sem R$.
- **Faturamento:** configurável por contrato (eventos: assinatura/entrada/mobilização/canteiro mensal/medição/comissionamento/entrega; condições 21/42, 30/45/60/180, à vista). Não implementado ainda (Sprint 7).
- **Avanço do item** = max(concluído?1, %informado, qtd_exec/qtd_prev). **Farol** = amarelo 10–25, vermelho >25 (a confirmar/configurável).

## Pendências de decisão do usuário
- Assinaturas: quem assina (Encarregado sempre? + Fiscal? + Supervisor?).
- Logo da empresa no PDF (opcional — usuário enviaria um PNG).

## Como rodar (nova máquina)
Pré-requisitos: **.NET 10 SDK**, **Node 20+**, **PostgreSQL 16+** (ou Docker), **dotnet-ef** (`dotnet tool install --global dotnet-ef --version 10.0.0`).

1. **Banco:** criar role/db (senha usada no dev = `industrialos`):
   ```sql
   CREATE ROLE industrialos LOGIN PASSWORD 'industrialos';
   CREATE DATABASE industrialos OWNER industrialos;
   ```
   Ajustar `backend/src/IndustrialOS.Api/appsettings.json` (ConnectionStrings:Postgres) se a senha do seu Postgres for outra.
2. **Segredos R2 (não versionados):** criar `backend/src/IndustrialOS.Api/appsettings.Local.json` com:
   ```json
   { "R2": { "AccountId": "<...>", "AccessKeyId": "<...>", "SecretAccessKey": "<...>", "Bucket": "industrialos" } }
   ```
   (As chaves do R2 ficam com o usuário — bucket Cloudflare `industrialos`, endpoint `<AccountId>.r2.cloudflarestorage.com`.)
3. **Backend:** `cd backend && dotnet run --project src/IndustrialOS.Api` (aplica migrations e faz o seed: tenant demo + admin `admin@demo.com`/`admin123`, + Planejador de teste `maria@demo.com`/`maria123` se recriado, + Encarregado `joao@demo.com`/`joao123`, + 61 funções).
4. **Frontend:** `cd frontend && npm install && npm run dev` → http://localhost:5173.

## Convenções importantes
- Filhos owned do RDO (efetivo, paralisações, recursos, serviços, retrabalho) **não podem ter `Id = Guid.NewGuid()` no construtor** (o EF trataria como update → `DbUpdateConcurrencyException`). O Id fica sem default; EF gera.
- R2 com AWS SDK v4: no `AmazonS3Config` usar `ForcePathStyle=true`, `AuthenticationRegion="auto"`, `RequestChecksumCalculation=WHEN_REQUIRED`, `ResponseChecksumValidation=WHEN_REQUIRED`; no `PutObjectRequest` usar `DisablePayloadSigning=true`.
- Autosave do RDO (`Rdo.tsx`) faz PUT full-replace — a UI **precisa enviar TODAS as seções** senão zera as omitidas.
- Enums da API serializados como string (JsonStringEnumConverter). Provider do banco fixo em Postgres.
