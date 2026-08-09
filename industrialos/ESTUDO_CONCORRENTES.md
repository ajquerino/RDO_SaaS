# Estudo Competitivo — IndustrialOS

> Objetivo: **roadmap de produto**. Estudamos **um concorrente por vez**, atacando
> **um vetor por vez** (o ponto onde ele nos ganha), e saímos de cada estudo com uma
> **DECISÃO** (Igualar / Flanquear / Ignorar de propósito) e um **item de roadmap concreto**.
>
> Regra de ouro: nós somos o **melhor dado de campo industrial/EPC**. Não viramos ERP,
> não viramos construção civil. Onde o concorrente é forte no que não é nosso nicho,
> **flanqueamos** (integramos / reposicionamos), não copiamos de frente.

## Nosso produto (contexto fixo)
SaaS multi-tenant de gestão operacional de obras **industriais** (montagem, caldeiraria,
tubulação, pintura industrial, EPC). Núcleo = **RDO**. Já implementado: multi-tenant real,
RDO industrial completo (efetivo/HH com hora-extra BR, paralisações, retrabalho, segurança
DDS/APR/PT, fotos/vídeo, assinatura, PDF), avanço por HH/qtd/etapas, Curva S, faróis, Pareto,
dashboards por perfil, **boletim de medição + faturamento configurável por eventos**,
aprovação por token, auditoria/LGPD, planos/PIX, **outbox de eventos (plumbing de IA/integração)**.
Falta: **offline-first**. Time enxuto, preço acessível, pt-BR.

## Placar (vetores por concorrente)
| # | Concorrente | Foco real | Vetor onde ele ganha | Decisão |
|---|---|---|---|---|
| 1 | **Sienge** (Softplan) | ERP construção civil / incorporação | Financeiro/ERP robusto + marca | **Flanquear** ✅ |
| 2 | **Vobi** | Gestão de obras + IA (civil/arquitetura) | Marketing, IA, integração SINAPI | **Flanquear (IA) + Ignorar (SINAPI)** ✅ |
| 3 | **Mobuss Construção** | Mobilidade em obra (civil) | Base instalada grande | **Flanquear por vertical** ✅ |
| 4 | **Procore / Autodesk / PlanRadar** | Global, construction management | Escala, ecossistema, capital | **Ignorar de propósito** ✅ |
| 5 | **Produttivo / Kartado / Diário de Obra** | RDO/checklist genérico | Simplicidade, já no ar | **Igualar (adoção)** ✅ |

---

## 1. Sienge (Softplan) — vetor: Financeiro/ERP robusto + marca ✅

### Raio-X do vetor
- ERP completo de construção civil: engenharia, **financeiro**, suprimentos, **fiscal**,
  contábil, comercial, RH, qualidade — tudo integrado.
- Profundidade **fiscal/financeira brasileira**: contas a pagar/receber, fluxo de caixa,
  orçamento de engenharia, obrigações fiscais do setor, integração contábil (anos de regra embutida).
- **Marca + base**: desde 1990, +2.300 clientes, reconhecido como "o ERP da construção".
- **Ecossistema aberto**: 30–40+ APIs, 25+ softwares integrados; a plataforma se posiciona como *hub*.

### Por que ele ganha
Vantagem de **MARCA/DISTRIBUIÇÃO + profundidade FINANCEIRO-FISCAL** — **não** de RDO de campo.
Foco explícito em **incorporação imobiliária** ("do pré-obra ao pós-venda"). **Nada de montagem
industrial/EPC.** Ele é forte onde nós não queremos competir e ausente onde somos nativos.

### Custo de igualar (paridade)
Virar ERP financeiro-fiscal = esforço **G-G-G** e erro estratégico: reconstruir contas a
pagar/receber, fluxo de caixa, contábil e fiscal contra quem faz isso há 35 anos, com time
enxuto, nos tira do nicho. **Não vale. Paridade total é armadilha.**

### Jogada de flanco (nosso roadmap)
Não ser o ERP — ser a **fonte de verdade do campo que alimenta o ERP**. O dado financeiro
nasce no campo (avanço, medição, HH, custo realizado), e é aí que somos melhores.
1. **Ponte Medição → Financeiro** (P/M): boletim vira título/valor a faturar e **exporta** pro
   financeiro do cliente (CSV → depois API).
2. **Custo realizado exportável** (M): avanço × HH × equipamento → custo por obra/item.
3. **Camada de integração aberta** (M/G): webhook/API pública sobre o **outbox de eventos** que
   já existe — `medicao_emitida` e `rdo_finalizado` viram lançamento em qualquer ERP, inclusive
   o próprio Sienge via API dele. **Plugamos no ecossistema dele em vez de brigar.**
4. **NÃO construir agora**: contabilidade, obrigações fiscais, contas a pagar → deixar pro ERP do cliente.

### Prova pro cliente (pós-flanco)
- "O dado do campo (RDO, HH, avanço) vira boletim de medição e já sai pronto pro seu financeiro — sem redigitar."
- "Medição por HH e por Kg/tonelada, curva S física — coisa que ERP de construção civil não calcula."
- "Mantém seu ERP; a gente é a camada de obra industrial que ele não tem, e conversa com ele."

### DECISÃO → **FLANQUEAR** (não igualar)
Construir a **Ponte Financeira** aproveitando o outbox de eventos já existente. Vira o "melhor
dado de campo industrial" que *alimenta* o financeiro, inclusive o do próprio Sienge.

### → Item de roadmap: Épico "Ponte Financeira" (ver a seção **Roadmap Competitivo** abaixo)

---

## 2. Vobi — vetor: IA (agentes) + SINAPI + narrativa de marketing ✅

### Raio-X do vetor
- Público real: **arquitetos, designers, reforma, construtoras/empreiteiras civis/residenciais**.
  +70 mil profissionais. Preço a partir de **R$ 180/mês** — SMB, ticket baixo, self-service.
- **SINAPI**: orçamento por composições da tabela SINAPI (custos de referência de obra civil/
  residencial por estado/mês/desoneração) — coração do produto.
- **IA**: posicionamento "único com Agentes de IA do Brasil" — forte como **narrativa de marketing**;
  capacidade concreta a confirmar.
- Ciclo civil completo: orçamento → cronograma → compras → diário → medição → financeiro + IA.

### Por que ele ganha
**MARKETING/NARRATIVA + produto no nicho civil/residencial.** Ponto-chave: **SINAPI é irrelevante
pro nosso cliente** — montagem industrial orça por HH, Kg, tonelada, diâmetro de solda, m². A IA é
o que importa e hoje é mais bandeira do que fosso técnico.

### Custo de igualar
- SINAPI → esforço **P**, mas **não vale** (cliente errado, distração).
- IA → **plumbing já pronto** (`EventoDominio` outbox + `IResumoIa` stub). IA real de nicho = **M**,
  e larga na frente com IA *industrial*, não genérica.

### Jogada de flanco (divide o vetor)
- **SINAPI → IGNORAR** e virar argumento: "SINAPI é preço de obra residencial; indústria mede HH/tonelada".
- **IA → FLANQUEAR igualando com profundidade de nicho** sobre o outbox existente:
  - Resumo diário do RDO (trocar `ResumoIaRegras` → `ResumoIaLlm`).
  - Previsão de atraso (curva S + Pareto de paralisações + retrabalho).
  - Alerta de produtividade/risco (HH real vs. previsto por frente).
  - Copiar a **lição de marketing**: comunicar como "IA para obra industrial".

### Prova pro cliente
- "IA que resume o RDO e aponta risco de atraso lendo sua curva S e paralisações — não chatbot genérico."
- "A gente não te faz orçar montagem com tabela residencial (SINAPI): HH, tonelada, solda, m²."
- "Mesmo ciclo da Vobi (orçamento→medição→financeiro), mas nativo pra caldeiraria/tubulação/EPC."

### DECISÃO → **FLANQUEAR (IA) + IGNORAR (SINAPI)**
Não perseguir SINAPI (mercado errado). Ligar **IA real de nicho** sobre o plumbing já existente —
maior ROI (infra pronta + diferencial único). Vobi não é concorrente direto (mira arquiteto/reforma);
a ameaça real é **narrativa** — neutralizada com IA que resolve a dor industrial.

### → Item de roadmap: Épico "IA Industrial" (ver a seção **Roadmap Competitivo** abaixo)

## 3. Mobuss Construção (Teclógica) — vetor: base instalada grande ✅

### Raio-X do vetor
- Software **modular** (11 módulos, nuvem, do canteiro ao pós-obra) para **construção civil/
  edificações**. Presente em **+100 construtoras brasileiras**.
- Ponto revelador: o **RDO é o módulo MAIS NOVO deles** — a força está em mão de obra/qualidade/
  documentos/vistoria de edificação, não num RDO industrial maduro.

### Por que ele ganha
Vantagem de **DISTRIBUIÇÃO** (base instalada + relacionamento + reputação em construtoras), **não
de produto de RDO**. E o foco é **edificação/construção civil** — de novo, fora do nosso nicho industrial.

### Custo de igualar
Base instalada **não se iguala com código** — é ativo de tempo, canal e relacionamento. Tentar roubar
as 100 construtoras civis deles = guerra de canal que um time enxuto perde. **Esforço não-técnico, e não vale.**

### Jogada de flanco
**Flanquear por vertical**, não por feature: ir onde a base deles NÃO vale (montagem industrial/
caldeiraria/EPC) e ser **mais profundo no RDO** — que neles é recém-lançado e genérico. Construir
**distribuição PRÓPRIA no nicho** (associações/sindicatos de montagem industrial, EPCistas, redes de
soldagem) onde a Mobuss não tem presença. A defesa é escolher o campo, não copiar o adversário.

### Prova pro cliente
- "RDO de montagem industrial de ponta a ponta (HH, solda, paralisação, retrabalho) — não um módulo de RDO recém-lançado em cima de um software de edificação."
- "Feito para caldeiraria/tubulação/EPC, não para prédio residencial."
- "Você não é a 101ª construtora de uma lista; é a obra industrial que a gente entende nativamente."

### DECISÃO → **FLANQUEAR POR VERTICAL** (batalha de posicionamento/canal, não de produto)
Pouco código: a resposta é **go-to-market vertical** + provar profundidade de RDO industrial. Único item
de roadmap com código é reduzir atrito de **migração/entrada** (importar dados de quem vem de outra ferramenta).

### → Item de roadmap: Épico "Entrada sem atrito" (migração/import) — ver a seção **Roadmap Competitivo** abaixo

---

## 4. Procore / Autodesk / PlanRadar — vetor: escala + ecossistema + capital ✅

### Raio-X do vetor
- **Globais**, gerenciam bilhões em obra, ecossistema/integrações enorme, muito capital.
- **Procore**: a partir de **~US$ 10 mil/ano**, suporte em PT. **PlanRadar**: planos Basic/Pro/
  Enterprise, licenças ilimitadas, discurso de ROI.
- Produto **genérico global**: sem hora-extra CLT, sem boletim de medição BR, sem faturamento por
  eventos BR, sem PIX/LGPD nativos.

### Por que ele ganha
Vantagem de **CAPITAL + ESCALA + ECOSSISTEMA** — imbatível em conta gigante. Mas para o cliente médio
brasileiro de montagem, é **caro demais** (dólar, US$10k+/ano) e **genérico demais** (não fala a
realidade fiscal/trabalhista/de medição brasileira).

### Custo de igualar
Igualar capital/escala/ecossistema global é **impossível e sem sentido** para um time enxuto.

### Jogada de flanco
Competir em **preço BR + aderência local**, não em escala. Sua vantagem já pronta: pt-BR nativo, R$,
**hora-extra CLT**, **boletim de medição/faturamento por eventos BR**, LGPD, PIX — a **~1/10 do preço**.
Usá-los como **âncora de posicionamento**: *"o poder de um Procore para montagem industrial brasileira,
em português e a um décimo do preço"*.

### Prova pro cliente
- "Preço em real, não em dólar — cabe no orçamento de uma montadora, não só de uma multinacional."
- "Medição, hora-extra e faturamento na regra brasileira, prontos — não um template global genérico."
- "Implantação em dias, em português, sem consultoria internacional."

### DECISÃO → **IGNORAR DE PROPÓSITO** (não é concorrente no seu segmento de preço/porte)
Eles só aparecem em contas enormes (grandes EPCistas). **Nenhum código a construir** por causa deles —
a resposta é posicionamento e a aderência local que você já tem. Reencontrar quando você subir para
enterprise; hoje, não. (Cautela honesta: se um dia mirar grande EPCista, aí sim vira concorrente real.)

### → Item de roadmap: NENHUM (só material de posicionamento/comparativo comercial)

---

## 5. Produttivo / Kartado / Diário de Obra — vetor: simplicidade + já no ar ✅

### Raio-X do vetor
- Apps **simples** de RDO/checklist, **self-service**, **trial grátis** (ex.: 30 dias), preço baixo
  (ex.: a partir de ~R$ 850/ano), base de milhares de profissionais.
- **Template pronto**, cabeçalho automático, técnico preenche no celular "conforme trabalha", relatório
  sai pronto com foto/local/assinatura e chega ao cliente no mesmo dia. **Kartado**: forte em
  infraestrutura/concessões, **offline**, interface intuitiva.

### Por que ele ganha
Vantagem **DE PRODUTO (UX) + go-to-market self-service**: baixíssima fricção de adoção, já no ar,
barato. **É o concorrente mais parecido com o nosso núcleo (RDO)** — e onde eles ganham é no **atrito**.

### Custo de igualar
**Médio, e VALE muito** — é a lição mais acionável do estudo todo. Nosso produto é profundo, mas
profundidade vira **peso**: o PRD manda "RDO completo em < 5 min no celular" e "100% offline". Muito da
base já existe (signup, trial, mobile-first, plumbing), mas o **RDO industrial completo tem fricção** e o
**offline-first ainda falta**.

### Jogada de flanco
**Igualar a simplicidade de ENTRADA** mantendo a profundidade opcional: *"fácil como um app de diário,
profundo como um ERP de montagem"*.
1. **RDO rápido por padrão** — seções avançadas colapsadas; caminho mínimo do encarregado em poucos toques.
2. **Valores-padrão/template por tenant** (efetivo, equipamentos, funções) — reduz digitação em campo.
3. **Onboarding self-service** com obra-exemplo já populada (o trial já existe; falta o "primeiro RDO em minutos").
4. **Offline-first** (pendência de go-live) — os simples têm offline; sem isso você perde no campo sem sinal.

### Prova pro cliente
- "Preenche o RDO em minutos no celular, offline — e ainda ganha medição, curva S e custo que um app de diário não tem."
- "Simples de começar hoje (trial + obra-exemplo), sem implantação."
- "Cresce com você: começa como diário, vira gestão de medição/faturamento sem trocar de sistema."

### DECISÃO → **IGUALAR (adoção/simplicidade de entrada)**
A lição mais importante: **profundidade não pode virar atrito**. Épico "RDO em 5 minutos" + onboarding
self-service + fechar o **offline-first**. É o maior risco competitivo real do núcleo — priorizar alto.

### → Item de roadmap: Épico "RDO em 5 minutos" + Offline-first — ver a seção **Roadmap Competitivo** abaixo

---

---

# Roadmap Competitivo — épicos derivados das decisões acima

> Cada épico nasce de uma **DECISÃO** das seções 1–5. Vira tarefa técnica concreta, ancorada no
> código que já existe. Ordem = prioridade competitiva × ROI × reaproveitamento.

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

## Épico 3 — "Entrada sem atrito" (origem: estudo Mobuss → FLANQUEAR por vertical)

**Tese:** a vantagem da Mobuss é base instalada (canal), não RDO. Não se combate com feature —
combate-se escolhendo o nicho industrial e **removendo o atrito de quem migra de outra ferramenta**.
Código mínimo; o grosso é go-to-market vertical (fora do escopo deste arquivo). Esforço **P**.

### Fase 1 — Import de EAP/cronograma robusto (P)
- [ ] Reforçar `CronogramaImport` (ClosedXML) com **mapeamento de colunas assistido** (o usuário
      aponta qual coluna é descrição/qtd/HH/valor) em vez de layout fixo — reduz o atrito de trazer o
      escopo de uma planilha existente. (Alinha com a sugestão nº 6 da ANALISE_DO_MVP.)
- **Aceite:** empresa vinda de planilha/outro app sobe a EAP sem reformatar o arquivo.

### Fora de escopo (é go-to-market, não código)
Distribuição vertical (associações/sindicatos de montagem, EPCistas), materiais de migração e
comparativos. Registrar no plano comercial, não aqui.

---

## Épico 4 — (origem: estudo Procore/PlanRadar → IGNORAR de propósito)

**Sem itens de roadmap.** Não é concorrente no segmento de preço/porte atual. A resposta é
**posicionamento** ("o poder de um Procore para montagem industrial brasileira, em português e a ~1/10
do preço") e a **aderência local que já existe** (R$, hora-extra CLT, medição/faturamento BR, LGPD, PIX).
Reavaliar só se o produto mirar grande EPCista (enterprise) no futuro.

---

## Épico 5 — "RDO em 5 minutos" + Offline-first (origem: estudo Produttivo/Kartado → IGUALAR adoção) 🔥 PRIORIDADE ALTA

**Tese:** o concorrente mais parecido com o núcleo ganha no **atrito de adoção**. Profundidade não pode
virar peso. Meta do próprio PRD: "RDO completo em < 5 min" e "100% offline". É o maior risco competitivo
real do núcleo. Esforço **M**.

### Fase 1 — RDO rápido por padrão (P/M) 🎯 primeiro
- [ ] No `Rdo.tsx`, **colapsar seções avançadas** (retrabalho, segurança detalhada, planejamento do
      próximo dia) por padrão; caminho mínimo do encarregado (efetivo + serviços + fotos) em destaque.
- [ ] Preservar autosave full-replace (enviar TODAS as seções — ver convenção do HANDOFF).
- **Aceite:** encarregado fecha um RDO básico em poucos toques, sem rolar por seções que não usa.

### Fase 2 — Valores-padrão/template por tenant (M)
- [ ] Catálogos já existem (61 funções, equipamentos por tenant). Adicionar **preset de efetivo/
      equipamento por obra** para pré-preencher o RDO do dia (reduz digitação repetida em campo).
- **Aceite:** o RDO do dia nasce pré-preenchido com o efetivo padrão da frente.

### Fase 3 — Onboarding self-service com obra-exemplo (M)
- [ ] No signup/trial (já existem), semear uma **obra-exemplo** com EAP e 1 RDO de amostra por tenant
      novo, para o "primeiro RDO em minutos" sem configurar nada.
- **Aceite:** empresa nova entra e vê valor (um RDO/dashboard) antes de cadastrar qualquer dado.

### Fase 4 — Offline-first (M/G) — pendência de go-live
- [ ] IndexedDB + fila de sync + resolução de conflito (UUID gerado no cliente, idempotência) —
      ver sugestão nº 6 da ANALISE_DO_MVP e a nota transversal abaixo.
- **Aceite:** RDO preenchido sem sinal sincroniza sem perda nem duplicidade ao reconectar.

> Este épico é o de **maior ROI de adoção** — priorizar acima dos demais no núcleo de campo.

---

## Épicos futuros
- _Novos concorrentes/vetores entram aqui conforme o placar de `ESTUDO_CONCORRENTES.md` evolui._

> Nota transversal: **offline-first** (IndexedDB + fila de sync) segue como a maior lacuna de
> go-live e diferencial de campo — agora capturada no **Épico 5, Fase 4** (prioridade alta).

---

## Prompt reutilizável (estudo de 1 concorrente / 1 vetor)
```
Você é um estrategista de produto B2B SaaS de construção/indústria no Brasil.
Estudar UM concorrente por vez para eu bater ou igualar o ponto exato onde ele me ganha.

MEU PRODUTO: IndustrialOS — SaaS multi-tenant de obras INDUSTRIAIS (montagem, caldeiraria,
tubulação, pintura, EPC). Núcleo RDO. [ver contexto fixo neste arquivo]. Nicho industrial/EPC,
não construção civil genérica. Time enxuto, pt-BR, preço acessível.

CONCORRENTE: {{NOME}} | Foco: {{FOCO}} | VETOR A ATACAR (só este): {{VETOR}}

PRODUZA, nesta ordem:
1. RAIO-X DO VETOR (features/capacidades concretas; real vs. percebido/marca)
2. POR QUE ELE GANHA (produto / marca-distribuição / capital / integração-ecossistema?)
3. CUSTO DE IGUALAR (esforço P/M/G até paridade mínima; vale a pena dado meu nicho?)
4. JOGADA DE FLANCO (neutralizar sem copiar de frente: foco industrial/EPC, integrar em vez
   de reconstruir, reposicionar)
5. PROVA PRO CLIENTE (3 argumentos na língua de quem contrata montagem industrial)
6. DECISÃO: [Igualar] / [Flanquear] / [Ignorar de propósito] + item de roadmap concreto

Regras: concreto e específico do mercado BR. Sem generalidade. Não inventar preço/feature —
marcar "a confirmar". Se faltar dado real, dizer o que verificar antes de decidir.
```
