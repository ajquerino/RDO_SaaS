# Roadmap Competitivo — Épicos derivados do Estudo Competitivo

> Cada épico nasce de uma **DECISÃO** em `ESTUDO_CONCORRENTES.md`. Aqui viram tarefas técnicas
> concretas, ancoradas no código que já existe. Ordem = prioridade competitiva × ROI × reaproveitamento.

---

## Épico 1 — "Ponte Financeira" (origem: estudo Sienge → FLANQUEAR)

**Tese:** não virar ERP. Ser a fonte de verdade do campo que **alimenta** o financeiro do cliente.
Aproveita infra já pronta: `Medicao` (boletim), `AvancoCalculo`, `JornadaCalculo` e o outbox
`EventoDominio`. Esforço total **P → M**. Sem depender de offline-first.

### Fase 1 — Export de Medição como título a faturar (P) 🎯 primeiro
Transformar o boletim de medição num artefato que o financeiro do cliente consome sem redigitar.
- [ ] `GET /api/v1/obras/{id}/medicoes/{medicaoId}/export?formato=csv|xlsx`
      → linhas de **parcela** (`MedicaoParcela`: vencimento, valor, pct) + cabeçalho (obra, BM nº,
      período, valor contrato, medido acumulado). RBAC: PLAN/GES/ADM (mesmo de `MedicoesController`).
- [ ] Layout de export estável e documentado (colunas fixas) — vira "contrato" de integração.
      Reaproveitar ClosedXML (já usado no import de cronograma) para o `.xlsx`.
- [ ] Botão "Exportar p/ financeiro" na aba **Medição** (`Medicao.tsx`), ao lado do PDF do BM.
- **Aceite:** dado o BM emitido, o usuário baixa CSV/XLSX com parcelas e vencimentos prontos p/
  lançar em contas a receber de qualquer sistema.

### Fase 2 — Custo realizado exportável (M)
O que o financeiro quer e o RDO genérico não dá: custo que nasce do campo.
- [ ] Serviço `Domain/Services/CustoRealizadoCalculo.cs`: por obra/item, HH realizado
      (`JornadaCalculo` + efetivo dos RDOs) × custo-hora + horas de equipamento × custo-hora.
      **Regra de custo-hora depende do usuário → NÃO inventar; expor parâmetro/config por função e
      por equipamento.** (Alinha com o TODO de índices kg/t/m² já registrado no HANDOFF.)
- [ ] `GET /api/v1/obras/{id}/custo-realizado?de=&ate=` → previsto (HH×valor da EAP) x realizado x desvio.
- [ ] Export CSV/XLSX no mesmo padrão da Fase 1.
- **Bloqueio de negócio:** confirmar com usuário a fonte do custo-hora (tabela por função? por
  contrato? por tenant?). Registrar a resposta aqui antes de codar.

### Fase 3 — Camada de integração aberta / webhooks (M/G)
Fazer o outbox `EventoDominio` (hoje só persiste) virar integração real — inclusive plugar no Sienge.
- [ ] Worker que consome `EventoDominio` não-processado (`Processado=false`) e entrega via HTTP
      (BackgroundService no `.Api` ou serviço dedicado). Marca `Processado/ProcessadoEm`. Idempotente
      (o evento já é append-only por transação).
- [ ] Entidade `WebhookAssinatura` por tenant (url, segredo HMAC, eventos inscritos: `medicao_emitida`,
      `rdo_finalizado`, `rdo_aprovado`). CRUD Admin/GES.
- [ ] Entrega assinada (HMAC-SHA256 no header), retry com backoff, log de entrega.
- [ ] Doc pública "IndustrialOS → seu ERP" com payloads de `medicao_emitida` e `rdo_finalizado`.
- [ ] (Opcional/depois) Conector direto Sienge: mapear `medicao_emitida` → API de títulos do Sienge.
- **Aceite:** cliente cadastra uma URL e passa a receber medição/RDO como evento assinado, sem polling.

### Fora de escopo deste épico (deixar pro ERP do cliente — decisão do estudo)
Contabilidade, obrigações fiscais (SPED/NF), contas a pagar, folha. **Não construir.**

### Dependências / reaproveitamento
- `EventoDominio` (outbox) — **já existe**, já grava `medicao_emitida`/`rdo_finalizado`.
- `Medicao` + `MedicaoParcela` — **já existem** (parcelas com vencimento/valor/pct).
- ClosedXML — já no projeto (import de cronograma) → reusar no export.
- `AvancoCalculo` / `JornadaCalculo` — base do custo realizado.

---

## Épico 2 — "IA Industrial" (origem: estudo Vobi → FLANQUEAR a IA / IGNORAR SINAPI)

**Tese:** neutralizar a narrativa "agentes de IA" da Vobi entregando IA que resolve dor
*industrial* — não chatbot genérico. Aproveita infra pronta: `IResumoIa` (abstração) +
`ResumoIaRegras` (stub) + `RdoContextoBuilder` + outbox `EventoDominio`. Esforço **M**.
NÃO integrar SINAPI (mercado residencial, cliente errado).

### Fase 1 — Resumo diário do RDO com LLM real (P/M) 🎯 primeiro
- [ ] `Infrastructure/Ia/ResumoIaLlm : IResumoIa` chamando LLM; trocar o registro no DI
      (`AddScoped<IResumoIa, ResumoIaLlm>`). Contrato e `RdoContexto` já existem.
- [ ] Config de provedor/chave por env (sem hardcode); fallback pro stub `ResumoIaRegras` se sem chave.
- [ ] Já existe o botão "Resumo do dia" no `Rdo.tsx` e o endpoint `GET /rdos/{id}/resumo` — só liga a IA real.
- **Aceite:** resumo em linguagem natural do RDO (efetivo, HH, avanço, paralisações, retrabalho) via LLM.

### Fase 2 — Previsão de atraso (M)
- [ ] Serviço que cruza **curva S** (previsto x realizado), **Pareto de paralisações** e **retrabalho**
      → sinal de risco de prazo por obra/frente. Começar por **regras** (determinístico, explicável),
      LLM só para redigir a explicação.
- [ ] Expor no Dashboard como farol/《alerta》 com o "porquê" (quais paralisações/desvios puxaram o risco).
- **Aceite:** obra com curva realizada abaixo da prevista + paralisações recorrentes acende alerta com causa.

### Fase 3 — Alerta de produtividade/risco por frente (M)
- [ ] HH realizado vs. previsto por frente/função (dados já em `DashboardController.produtividade`).
- [ ] Consumir o outbox `EventoDominio` (`rdo_finalizado`) via worker para gerar alertas assíncronos.
- **Aceite:** desvio de produtividade por frente notifica gestor sem ele abrir o dashboard.

### Fora de escopo (decisão do estudo)
Integração SINAPI. Orçamento residencial. Chatbot genérico sem contexto de obra.

### Lição de posicionamento (não é código)
Comunicar publicamente como **"IA para obra industrial"** — a Vobi vence hoje na narrativa, não
na dor do nosso cliente. Marketing importa tanto quanto a feature.

---

## Épicos futuros (a preencher conforme estudamos os concorrentes 3–5)
- **Épico 3** — origem: estudo Mobuss (base instalada). _a definir._
- **Épico 4** — origem: estudo Procore/PlanRadar (escala/ecossistema). _a definir._
- **Épico 5** — origem: estudo Produttivo/Kartado (simplicidade/onboarding). _a definir._

> Nota transversal: **offline-first** (IndexedDB + fila de sync) segue como a maior lacuna de
> go-live e diferencial de campo — priorizar em paralelo à Fase 1 da Ponte Financeira.
