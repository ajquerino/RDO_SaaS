# PRD — IndustrialOS
## Plataforma SaaS de Gestao Operacional de Obras Industriais

### 1. Visao do produto
Plataforma SaaS multi-tenant para empresas de montagem industrial, caldeiraria,
estruturas metalicas, tubulacao, pintura, mecanica, manutencao, EPC e construcao
industrial. Une planejamento, execucao (RDO), produtividade, indicadores,
documentacao, medicao/faturamento e inteligencia de dados num unico sistema,
mobile-first e offline-first. O RDO e o modulo operacional principal; a plataforma
o cerca com cadastros, dashboards por perfil, custos e medicao.

### 2. Problema e proposta de valor
- Obras industriais controlam efetivo, avanco, paralisacoes e retrabalho em papel/
  Excel, sem consolidacao nem visao gerencial em tempo real.
- Proposta: preencher o RDO no celular em minutos, e a partir do dado de campo gerar
  automaticamente avanco fisico/financeiro, curva S, farois de prazo, boletim de
  medicao e indicadores por perfil — com aprovacao do fiscal/cliente.

### 3. Personas
- **Encarregado/Lider** (campo): preenche o RDO no celular, offline, poucos cliques.
- **Supervisor**: acompanha frentes, valida, complementa.
- **Planejador**: cadastra EAP/cronograma, emite boletim de medicao, acompanha avanco.
- **Gestor/Engenharia**: cadastra obras/usuarios, ve dashboards e rankings.
- **Fiscal do Cliente / Cliente**: aprova RDO/medicao por link, ve avanco.
- **Administrador (empresa)**: configura empresa, permissoes, exclusoes.
- **Super-admin (SaaS)**: gestao de tenants, planos, billing.

### 4. Objetivos e metricas de sucesso
- Preencher um RDO completo em < 5 min no celular.
- Funcionar 100% offline e sincronizar sem perda.
- Reduzir tempo de fechamento de medicao; avanco/curva S automaticos.
- Metricas: tempo medio de preenchimento, % RDOs aprovados no prazo, adocao por
  usuario, retencao por tenant, NPS.

### 5. Escopo (modulos)
Empresas/Filiais; Usuarios & Permissoes (RBAC); Clientes; Obras (contratos,
cronograma/EAP, documentos, localizacao); Equipes (efetivo/HH/histograma);
Equipamentos (proprios/locados, horas, custos, manutencao); Documentos; **RDO**;
Produtividade; Dashboard (paineis por perfil); Planejamento; Custos (previsto x
realizado, curva S); **Boletim de Medicao**; Aprovacoes.

### 6. Requisitos funcionais chave (preservar do MVP)
- RDO com todas as secoes validadas (ver ANALISE_DO_MVP.md secao 2.3 e AUDITORIA_DOCS_VS_MVP.md).
- Numeracao de RDO sequencial por obra; RDO com REVISOES (Rev.0, Rev.1...): fiscal pode
  solicitar revisao (com motivo) antes de aprovar; revisao anterior mantida no historico.
- Avanco por item flexivel: status Concluido=100%, % direto, qtd executada/prevista,
  ou etapas (regras de credito por disciplina). Nunca > 100%.
- Avanco da obra ponderado por HH (fallback quantidade, depois media).
- Curva S prevista (HH distribuido pelas datas dos itens) x realizada.
- Farol prazo x avanco (verde/amarelo/vermelho/cinza) com regras do MVP.
- Boletim de medicao: medido = (%fim - %inicio) x valor do item.
- Faturamento CONFIGURAVEL por contrato/cliente (nao ha regra unica): plano com eventos
  (% assinatura, % entrada, % mobilizacao de canteiro, canteiro diluido mensal, medicoes
  quinzenais/mensais, % comissionamento, % entrega tecnica/final, ou 100% na entrega) +
  condicoes de pagamento reutilizaveis (ex.: 21/42, 21/35/42/60, 30/45/60/180, a vista).
- Aprovacao de RDO E de medicao por link com token; bloqueio de edicao pos-aprovacao.
- Vinculo usuario-obra; cada usuario ve so suas obras; gestor ve todas.
- Salvamento automatico + sincronizacao; clima automatico por GPS.

### 7. Requisitos nao-funcionais
- Multi-tenant com isolamento total (tenant_id em todas as entidades + filtros).
- Performance: respostas < 300 ms em consultas comuns; agregacoes materializadas.
- Offline-first (IndexedDB + fila de sync + resolucao de conflito).
- Seguranca: JWT, hash de senha (bcrypt/argon2), RBAC, auditoria/logs, LGPD.
- Escalabilidade horizontal; storage de midia em Cloudflare R2; Redis p/ cache.
- Observabilidade (logs estruturados, metricas, tracing).

### 8. Fora de escopo (agora)
- IA (resumo, previsao de atraso, NLP): apenas preparar arquitetura/eventos.
- Integracao com ERP/orcamento: preparar contratos, nao implementar.

### 9. Restricoes e premissas
- Stack fixo: .NET 10 (LTS) + EF Core + PostgreSQL; React + TS + Vite + Tailwind; R2; Docker.
- Mobile-first (desktop e consequencia). Idioma pt-BR.

### 10. Riscos
- Complexidade do offline/sync; concorrencia multi-usuario na mesma obra.
- Modelagem de avanco/medicao (fonte de verdade unica).
- Volume de midia (fotos/videos) — usar storage dedicado e thumbnails.
