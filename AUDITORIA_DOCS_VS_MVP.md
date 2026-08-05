# AUDITORIA — Documentos da Fase 2 × MVP

> **Objetivo:** verificar se os 13 documentos da fase 2 preservam a lógica validada no MVP (`github.com/ajquerino/RDO`) e responder aos 7 pontos abertos da seção 11 de [ANALISE_DO_MVP.md](ANALISE_DO_MVP.md).
> **Data:** 2026-08-03.
> **Veredito geral:** documentos **fortes, coerentes entre si e bem ancorados no MVP**. Foram escritos por quem leu o bundle a fundo (retrabalho, etapas, boletim, faturamento, EAP, disciplinas, Pareto, "em espera" e temperatura existem de fato no MVP — confirmado por busca no código). Restam **poucos gaps reais**, quase todos de modelagem fina ou premissas a confirmar — nenhum invalida a base.

---

## 1. Verificação de ancoragem no MVP (o que eu conferi no código)

Busquei cada recurso "suspeito de ser novo" direto no `index.html` do MVP. Resultado:

| Recurso nos docs | Ocorrências no MVP | Conclusão |
|---|---|---|
| retrabalho | 46 | ✅ é do MVP |
| etapas (crédito por etapa) | 25 | ✅ é do MVP |
| boletim | 17 | ✅ é do MVP |
| faturamento | 8 | ✅ é do MVP |
| medição | 7 | ✅ é do MVP |
| EAP / escopo | 7 / 14 | ✅ é do MVP |
| disciplina | 5 | ✅ é do MVP |
| "em espera" (hold) | 4 | ✅ é do MVP |
| temperatura | 10 | ✅ é do MVP |
| token (aprovação) | 50 | ✅ é do MVP |
| **parcela** | **0** | ⚠️ base quase nula |
| **vencimento** | **1** | ⚠️ base quase nula |
| **prazo de pagamento** | **1** | ⚠️ base quase nula |

**Correção de rota:** minha suspeita inicial de que "Medição/Boletim seria adição nova" estava **errada** — é do MVP. O único ponto sem lastro real é a **regra de parcelas/vencimentos** (ver Gap G1).

---

## 2. Situação dos 7 pontos abertos (seção 11 da análise)

| # | Pergunta | Status nos docs | Onde |
|---|---|---|---|
| 1 | Cálculo de HH (almoço/hora extra) | 🟡 **Parcial** | Almoço é descontado (07-12/13-17 = 9h, T02). Mas **onde a hora extra entra no HH realizado** não está definido; `rdo_efetivo.hora_extra` é `text`. |
| 2 | Faixas do farol | 🟢 **Resolvido (a confirmar)** | verde: acompanha/100%; amarelo: desvio 10–25%; vermelho: >25% ou prazo estourado; cinza: sem datas. Confirmar que 10/25 são os números do MVP. |
| 3 | Fonte oficial do % de avanço | 🟢 **Resolvido** | `avanco_obra_hh = Σ(hh·pct)/Σhh`; fallback qtd; fallback média simples. |
| 4 | "Alteração do cliente" reabre o RDO? | 🔴 **Não resolvido** | Docs só modelam **bloqueio duro pós-aprovação (409)**. O MVP tinha "Alteração do cliente" e "Revisão do RDO" — falta o caminho de revisão solicitada. **Gap G4.** |
| 5 | Numeração do RDO | 🟢 **Resolvido** | Sequencial por obra (`unique(obra_id,numero)`, último+1). |
| 6 | Serviço extra em produtividade/custos | 🟡 **Parcial** | Modelado (`rdo_servicos.obra_item_id` nulo = extra), mas **o efeito nos indicadores** (entra/não entra na produtividade) não está definido. |
| 7 | Papel do "Fiscal do Cliente" | 🟢 **Resolvido** | Externo, sem login; aprova só por link com token. |

---

## 3. Gaps e inconsistências encontrados (priorizados)

### 🔴 Alta — resolver antes de codar o módulo afetado

**G1 — Regra de parcelas/vencimentos sem lastro no MVP.**
DDL/Modelo/Critérios definem `medicao_parcelas` e regras "21/42" e "30/45/60/180". No MVP: `parcela`=0, `vencimento`=1. É provável **premissa**, não lógica validada. Ação: você confirmar a regra real de faturamento antes do Sprint 7.

**G2 — Crédito por etapa/disciplina não tem onde morar na DDL.**
O teste T07 exige "Tubulação, marcar até Soldagem → 85%", e o Modelo cita `disciplinas/etapas_credito(peso_pct)`. Mas a **DDL não tem essas tabelas** — `disciplina` é `text` em `obra_itens` e `etapas_feitas` é `jsonb` em `rdo_servicos`. Sem `etapas_credito.peso_pct`, o "85%" não tem fonte. Ação: adicionar tabelas de disciplinas/etapas com pesos à DDL (Sprint 3).

**G4 — Fluxo de revisão pós-aprovação ausente** (ponto aberto #4).
Falta definir o que "Alteração do cliente" faz: reabre para o encarregado? gera revisão nova (v2) mantendo a v1 aprovada? Hoje o modelo só bloqueia. Ação: decisão de negócio + estado de "revisão solicitada".

### 🟡 Média — corrigir para consistência interna

**G3 — Medição: aprovação e numeração incompletas na DDL.**
Permissões e fluxo F4/UC40 preveem **aprovar medição por link**, mas: (a) `medicoes` na DDL **não tem** `aprovado_por/aprovado_em` (o Modelo tinha); (b) **sem** `unique(obra_id,numero)`; (c) a API `/aprovacao/{token}` só trata RDO, não medição. Ação: alinhar DDL + endpoint de aprovação de medição.

**G5 — Login: PIN × senha não reconciliados.**
Wireframe/Catálogo mantêm **PIN** (nome + PIN pessoal / PIN do gestor); PRD/Casos de Uso/API usam **e-mail + senha + JWT**. São modelos diferentes de autenticação. Recomendação (já na análise §8.1): **usuário autenticado por senha/JWT no cadastro do dispositivo + PIN como atalho rápido de campo** por cima. Precisa ficar explícito num documento.

**G6 — Vídeos não modelados.**
Prompt mestre e Arquitetura citam **vídeos**; a camada de dados só tem `rdo_fotos` (imagens). Ação: `rdo_midia` genérica (foto/vídeo) ou tabela de vídeos + thumb.

### 🟢 Baixa — anotar, não bloqueia

- **G7 — DDL é subconjunto:** faltam `equipamentos`, `funcionarios/funcoes`, `documentos`, RBAC (`roles/permissions`). É **intencional** (Roadmap adia p/ Sprints 1/2/8), mas precisa entrar antes das respectivas sprints.
- **G8 — Referência cruzada quebrada:** o PRD cita "ANALISE_DO_MVP.md seções 3.1–3.17", estrutura que **não existe** no [ANALISE_DO_MVP.md](ANALISE_DO_MVP.md) atual (que vai até a seção 12 com outra numeração). Ajustar a referência.
- **G9 — `turno`** existe em `rdos` mas não aparece no Catálogo/Wireframe de Identificação. Alinhar.

---

## 4. Pontos fortes dos documentos (o que está muito bom)

- **Fórmulas de domínio centralizadas** (Modelo §"Fórmulas de domínio") como fonte única — evita a lógica espalhada que era limitação do MVP.
- **`avanco_snapshots`** materializado por evento — resolve a limitação de dashboard lento.
- **Idempotência de sync** (`/rdos/sincronizar`, UUID no cliente, `versao`) — endereça o risco de conflito offline.
- **Critérios de aceitação e casos de teste em Gherkin numérico** (T01–T25) — dão paridade verificável com o MVP.
- **Camada de eventos/outbox** já prevista para IA sem reescrever o núcleo.
- **Roadmap com regra "módulo completo antes do próximo"** coerente com seu prompt.

---

## 5. Correções aplicadas (2026-08-03, após respostas do negócio)

| Gap | Status | O que mudou nos documentos |
|---|---|---|
| G1 Faturamento | ✅ **Resolvido** | Modelado como **plano configurável por contrato** (`faturamento_planos` + `faturamento_eventos` + `condicoes_pagamento` com parcelas `[{dias,pct}]`). Cobre % assinatura/entrada/mobilização/canteiro mensal/medição/comissionamento/entrega e "100% a vista". Atualizados: Modelo, DDL, API, PRD, Casos de Uso, Catálogo, Wireframes, Critérios, Casos de Teste, Roadmap. |
| G4 Revisão | ✅ **Resolvido** | Fiscal pode **"Solicitar revisão" (com motivo)** antes de aprovar; RDO gera **Rev.N+1** e mantém a anterior em `rdo_revisoes`. Atualizados: Modelo, DDL, API, PRD, Fluxos, Casos de Uso, Permissões, Critérios, Casos de Teste, Roadmap. |
| G2 Etapas/disciplina | ✅ **Resolvido** | DDL ganhou `disciplinas` e `etapas_credito(peso_pct)`; `obra_itens.disciplina_id` FK. |
| G3 Medição aprovação | ✅ **Resolvido** | `medicoes` com `aprovado_por/em`, `token_aprovacao`, `unique(obra_id,numero)`; endpoint de aprovação por token na API. |
| G5 Login PIN×senha | ✅ **Resolvido** | Arquitetura define: senha/JWT como credencial primária + **PIN como atalho de campo**. |
| G6 Vídeos | ✅ **Resolvido** | `rdo_fotos` → `rdo_midia(tipo foto\|video, duracao_seg…)`. |
| G8 Ref. cruzada | ✅ **Resolvido** | PRD passou a citar a estrutura correta da análise + esta auditoria. |
| G7 DDL subconjunto | 🟡 Pendente (planejado) | `equipamentos/funcionarios/documentos/RBAC` seguem adiados p/ Sprints 1/2/8 (intencional). |
| G9 `turno` | 🟢 Menor | Alinhar Catálogo/Wireframe na tela de Identificação (pendência cosmética). |

## 6. Ainda depende de você (1 item)

- **Faixas do farol:** adotei defaults **amarelo 10–25 / vermelho >25** (agora configuráveis por tenant no Modelo). Confirmar se são esses números ou outros (ex.: 15/30).

## 7. Recomendação de próximos passos

1. Você confirmar as **faixas do farol** (único ponto aberto).
2. Fechar G7/G9 quando chegar a sprint correspondente.
3. Liberar o **Sprint 0/1** para implementação — a base documental está consistente e alinhada ao MVP.

> Nenhuma correção alterou a lógica operacional do MVP — todas **preencheram lacuna de modelagem** ou **implementaram uma regra que você confirmou**. Base pronta para construir.
