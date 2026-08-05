# Roadmap por Sprints — IndustrialOS
Regra: cada modulo entregue completo (banco + backend + frontend + testes + docs)
antes do proximo. Sprints ~2 semanas (ajustavel).

## Sprint 0 — Fundacao
Setup .NET 10 (Clean Architecture, EF Core, JWT, Swagger, Docker), Postgres, React+TS+
Vite+Tailwind, CI/CD, multi-tenant base (tenant_id + query filter), health checks.

## Sprint 1 — Auth & Empresas & Usuarios (RBAC)
Login/refresh, hash de senha, roles/permissoes, cadastro de empresas/filiais e
usuarios, vinculo usuario-obra. Frontend: login, home, gestao de usuarios.

## Sprint 2 — Clientes & Obras & EAP
Cadastro de clientes e obras (contrato, datas, prazo pagamento, localizacao); EAP
item a item; importar cronograma (Excel/CSV); documentos (upload R2).

## Sprint 3 — RDO (nucleo) parte 1
Cabecalho + secoes: identificacao, clima (auto), efetivo, jornada, paralisacoes,
recursos, servicos (vinculo EAP + avanco flexivel), salvamento automatico. PDF
(QuestPDF). Mobile-first.

## Sprint 4 — RDO parte 2 + Offline
Retrabalho, ocorrencias, dificuldades, pendencias, proximo dia, planejamento, fotos
(R2+thumb), seguranca, assinaturas. Finalizar/enviar. **Offline-first** (IndexedDB +
fila de sync + conflito). Edicao com bloqueio pos-aprovacao.

## Sprint 5 — Aprovacao, Revisao & Historico
Link com token; tela publica de aprovacao com **Aprovar / Solicitar revisao (motivo)**;
ciclo de revisoes (Rev.N, historico em rdo_revisoes); historico agrupado; selos; reenvio.

## Sprint 6 — Avanco, Curva S & Dashboard
Snapshots de avanco (evento ao finalizar); avanco por HH/qtd/etapas; curva S
previsto x realizado; farois; dashboards por perfil; Pareto; rankings.

## Sprint 7 — Faturamento, Boletim de Medicao & Custos
Plano de faturamento configuravel por contrato (eventos: assinatura/entrada/mobilizacao/
canteiro mensal/medicao/comissionamento/entrega) + condicoes de pagamento (21/42,
21/35/42/60, a vista); calculo (% x valor por periodo), parcelas/vencimentos, PDF,
numeracao por obra, aprovacao por token; custos previsto x realizado (base), histograma HH.

## Sprint 8 — Equipamentos, Produtividade & Documentos
Equipamentos (horas/custos/manutencao); produtividade (HH prev x real, kg/t/m2);
documentos por obra; refinamento de dashboards.

## Sprint 9 — Hardening & Go-live
Auditoria/logs, LGPD, performance (indices/cache Redis), testes E2E, observabilidade,
billing/planos (SaaS), onboarding de tenant.

## Fase futura — IA (arquitetura ja preparada)
Resumo diario, previsao de atraso, analise de produtividade/risco, NLP.
