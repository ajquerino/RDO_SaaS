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

## Épicos futuros (a preencher conforme estudamos os concorrentes 2–5)
- **Épico 2** — origem: estudo Vobi (IA + SINAPI). _a definir._
- **Épico 3** — origem: estudo Mobuss (base instalada). _a definir._
- **Épico 4** — origem: estudo Procore/PlanRadar (escala/ecossistema). _a definir._
- **Épico 5** — origem: estudo Produttivo/Kartado (simplicidade/onboarding). _a definir._

> Nota transversal: **offline-first** (IndexedDB + fila de sync) segue como a maior lacuna de
> go-live e diferencial de campo — priorizar em paralelo à Fase 1 da Ponte Financeira.
