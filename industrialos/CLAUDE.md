# IndustrialOS — contexto para qualquer Claude que trabalhar neste repo

> Este arquivo é o **cérebro compartilhado** entre o Claude da NUVEM (escreve features em branches)
> e o Claude LOCAL (tem .NET SDK + Postgres; valida, testa, gera migrations e mergeia). Leia-o
> **antes de começar qualquer tarefa**. Não repita perguntas de contexto que já estão aqui.

## O produto
SaaS **multi-tenant** de gestão operacional de **obras industriais** (montagem, caldeiraria,
tubulação, pintura industrial, EPC). Núcleo = **RDO** (Relatório Diário de Obra). Mobile-first,
pt-BR, preço acessível, time enxuto. **Não** é ERP nem construção civil — o diferencial é o
**dado de campo industrial**. (Ver `ESTUDO_CONCORRENTES.md` — estudo competitivo + roadmap, num arquivo só.)

## Stack e arquitetura
- **Backend:** .NET 10, **Clean Architecture** em 4 projetos (deps apontam pra dentro):
  - `IndustrialOS.Domain` — entidades, `Common/BaseEntity`, `Services/` (regras puras e testáveis:
    JornadaCalculo, HhSemanalCalculo, AvancoCalculo, AssinaturaCalculo).
  - `IndustrialOS.Application` — **interfaces** (IEmailSender, IStorage, IRdoPdf, IJwtService,
    IResumoIa, IAbacatePay, ICronogramaImport…).
  - `IndustrialOS.Infrastructure` — implementações (Persistence/AppDbContext, Auth/JwtService,
    Storage/R2Storage, Email/EmailSender, Pdf/QuestPDF, Pagamento/AbacatePayClient, Ia/…).
  - `IndustrialOS.Api` — Controllers (finos), Middleware/TenantMiddleware, Filtros/, Program.cs.
- **Frontend:** React + TypeScript + Vite + Tailwind + PWA. `lib/api.ts` (fetch central: Bearer,
  trata 401 sessão/expiração, 402 assinatura, timeout), `store/auth.ts` (zustand + helpers de
  permissão), `pages/` (uma por área; `Home.tsx` shell com abas; `PlataformaConsole.tsx` = super-admin).
- **Infra:** PostgreSQL, Cloudflare R2 (arquivos, via AWSSDK.S3), QuestPDF, JWT. Deploy no **Railway**.

## Multi-tenancy (o padrão central — NÃO enfraqueça)
- `BaseEntity` = Id, **TenantId**, timestamps, **DeletadoEm** (soft delete).
- **Global Query Filter** no AppDbContext aplica sozinho `WHERE tenant_id = @atual AND deletado_em IS NULL`
  em toda BaseEntity. `TenantMiddleware` resolve o tenant do JWT (claim `tenant_id`) → `ITenantContext`.
  No SaveChanges o TenantId é carimbado automático.
- **Exceção única e explícita:** o **super-admin** (Funcao.SuperAdmin, mora no tenant de sistema
  `EhSistema`) só acessa dados cross-tenant no `PlataformaController` via `.IgnoreQueryFilters()` +
  `[Authorize(Roles="SuperAdmin")]`. Nunca use IgnoreQueryFilters fora daí sem motivo forte.
- **Modo suporte ("Acessar como"):** super-admin emite um JWT como o Admin da empresa (tenant_id da
  empresa, claim `suporte`) — auditado, 1h, isento da checagem de sessão única.

## Convenções e PEGADINHAS (já custaram tempo)
- **EF owned entities (filhos de RDO/Medição):** NÃO inicialize `Id = Guid.NewGuid()` no construtor —
  o EF trata insert como update (DbUpdateConcurrencyException). Deixe o EF gerar o Guid.
- **Migrations:** só o Claude LOCAL gera/aplica (tem SDK). A nuvem NÃO roda `dotnet ef`. Após mudar
  entidade, o local roda `dotnet ef migrations add` + `database update`, e checa
  `migrations has-pending-model-changes` (deve dar "No changes").
- **Segredos:** ficam em `backend/src/IndustrialOS.Api/appsettings.Local.json` (**GITIGNORED**) no dev
  e em variáveis de ambiente na produção (Railway). R2, Jwt:Key, AbacatePay, Email. **Nunca commite
  segredo.** `appsettings.json` só tem defaults/placeholders.
- **Auditoria** é automática no AppDbContext.SaveChanges. **Soft delete** em tudo (DeletadoEm). **Outbox**
  `EventoDominio` grava ações importantes na mesma transação (base p/ IA/integrações).
- **QuestPDF imagem:** use `.Height(H).Image(bytes).FitArea()` (senão "conflicting size constraints").
- **PowerShell 5.1** no dev quebra com acento em `.ps1` sem BOM → scripts em ASCII.

## Regras de negócio confirmadas pelo dono (NÃO suponha; se faltar, pergunte)
- **Hora extra (jornada única por RDO):** seg–sex 07:00–16:48 = 8h48 líquidas; extra dia útil = após
  16:48, **50% até 10h/semana, 70% acima** (aplicado no fechamento SEMANAL). Sáb/Dom/Feriado = **100%
  até 8h, 150% acima**. Feriado = checkbox no RDO. **As taxas são PARAMETRIZÁVEIS por empresa**
  (RegraHoraExtra + Configurações › Horas extras).
- **Faturamento:** configurável por contrato (% entrada/mobilização/medição quinzenal ou mensal/
  comissionamento/entrega; condições 21/42, 30/45/60/180…). RDO tem ciclo de revisões (mantém histórico).
- **Ver valores R$:** só Planejador/Gestor/Admin. **Gerir obra/EAP/clientes:** Planejador/Gestor/Admin.
  **Criar usuário:** Gestor/Admin. Encarregado/Líder/Supervisor: RDO só das obras vinculadas.
- **Assinatura:** trial 14 dias → Vencido (tolerância 7 dias) → **Bloqueada = SOMENTE LEITURA**
  (vê/baixa o que já existe, não cria/edita). Avisos 7/3/1 dia. Limite de plano AVISA (não bloqueia).
- **Sessão única:** login novo derruba o dispositivo anterior (anti-compartilhamento).

## O que já está construído (na `main`)
Auth/RBAC, Empresas/Usuários (CRUD), Clientes, Obras (CRUD) + **EAP** (add manual + import CSV/XLSX +
editar itens/HH/valores), **RDO completo** (efetivo/HH, paralisações, recursos, serviços c/ avanço,
retrabalho, segurança DDS/APR/PT, clima GPS, fotos R2, assinaturas, PDF, autosave), Dashboards
(avanço, Curva S, farol, Pareto, produtividade), **Medição + faturamento**, aprovação por token +
revisão, Auditoria/LGPD, **Planos + billing** (motor de inadimplência somente-leitura, **AbacatePay**
checkout hospedado PIX/boleto/cartão, webhook auto-desbloqueio), onboarding (convite e-mail +
autocadastro público `/criar-conta`), cliente escolhe plano, reset de senha + e-mail, sessão única,
excluir RDO, editar/excluir obra, super-admin (editar/excluir empresa+CNPJ, planos, assinaturas,
estender trial +14d, **acessar como**), arquitetura de IA (outbox + resumo por regras — stub),
**offline-first** (RDO cria/edita offline + fila de sync em IndexedDB — validado), tela de **login com
visual** (mobile-first). **Já EM PRODUÇÃO no Railway (validado e2e: empresa→obra→EAP→RDO→foto R2→dashboard).**
**Pendências (config de produção, não código):** e-mail Resend (só logado sem chave), reset do super-admin
(hoje super@demo/super123), KYC do AbacatePay p/ cobrar de verdade.

## Deploy (Railway — em produção)
3 serviços num projeto Railway: **API** (.NET, root `industrialos/backend`, porta 8080),
**frontend** (nginx, root `industrialos/frontend`, porta 80, build-arg `VITE_API_URL` = URL da API),
**Postgres**. A API **migra + semeia** (tenant sistema + super-admin via `Seed:SuperAdmin*`) no boot,
em todos os ambientes. **Push na `main` = auto-redeploy.** `VITE_API_URL` é BUILD-TIME (mudou → redeploy).
Ver `DEPLOY.md` e `.env.example`. AbacatePay produção precisa de **KYC** (cartão idem); com chave Dev é
tudo simulado.

## FLUXO DE COLABORAÇÃO (importante)
- **Claude NUVEM:** escreve a feature numa **branch nova** (`feat/...` ou `fix/...`), **NÃO toca na
  `main`**, **NÃO roda/compila/migra** (sem SDK), **NÃO commita segredo**. No fim, faz push e lista os
  arquivos. Se mexer em entidade, avisa que precisa de migration (o local gera).
- **Claude LOCAL:** faz `git fetch`, confere a base com `merge-base` (cuidado com base antiga),
  mergeia `--no-ff`, resolve conflitos, gera migration se preciso, **compila backend + frontend**,
  roda `has-pending-model-changes`, **testa ao vivo** (API local + curl / navegador), e só então
  **empurra a `main`** (que dispara o redeploy no Railway). Depois apaga a branch.
- **Nunca supor regra de negócio** — perguntar ao dono. **Não escrever código antes da decisão.**
