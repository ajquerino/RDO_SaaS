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
| 2 | Vobi | Gestão de obras + IA (civil/arquitetura) | Marketing, IA, integração SINAPI | _a estudar_ |
| 3 | Mobuss Construção | Mobilidade em obra (civil) | Base instalada grande | _a estudar_ |
| 4 | Procore / Autodesk / PlanRadar | Global, construction management | Escala, ecossistema, capital | _a estudar_ |
| 5 | Produttivo / Kartado / Diário de Obra | RDO/checklist genérico | Simplicidade, já no ar | _a estudar_ |

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

### → Item de roadmap: Épico "Ponte Financeira" (ver `ROADMAP_COMPETITIVO.md`)

---

## 2. Vobi — vetor: IA + integração SINAPI
_A estudar. Rodar o prompt de estudo (abaixo) com CONCORRENTE=Vobi._

## 3. Mobuss Construção — vetor: base instalada
_A estudar._

## 4. Procore / Autodesk / PlanRadar — vetor: escala/ecossistema/capital
_A estudar._

## 5. Produttivo / Kartado / Diário de Obra — vetor: simplicidade
_A estudar._

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
