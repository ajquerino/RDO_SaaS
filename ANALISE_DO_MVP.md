# ANÁLISE DO MVP — RDO (Relatório Diário de Obra)

> **Fonte analisada:** repositório `github.com/ajquerino/RDO` — arquivo único `index.html` (~1,37 MB).
> **Data da análise:** 2026-08-03.
> **Método:** o bundle JavaScript está **minificado** (nomes de função de 1–2 letras). Portanto esta análise reconstrói o comportamento a partir de: (a) todos os textos de interface em português; (b) bibliotecas identificadas; (c) o endpoint de nuvem embutido; (d) padrões de armazenamento e PWA. Onde há **inferência**, está marcado como tal. Nenhuma regra de negócio foi presumida sem evidência textual.
> **Prioridade de verdade:** conforme definido no prompt mestre, a lógica validada no MVP tem prioridade. Pontos ambíguos estão listados na seção "Pontos a confirmar com o negócio".

---

## 1. Resumo completo do sistema atual

O MVP é um **aplicativo web de página única (SPA) em React**, empacotado e minificado dentro de um único `index.html`, hospedado no **GitHub Pages**. Ele funciona como um **PWA offline-first**: usa `serviceWorker` para cache e `localStorage` como banco local. A sincronização com a "nuvem" é feita contra um **endpoint do Google Apps Script** (`script.google.com/macros/s/.../exec`), que por sua vez persiste em **Google Sheets**.

O produto resolve um fluxo operacional único: **o encarregado preenche, no celular, o Relatório Diário de Obra**, anexa fotos e assinaturas, gera um **PDF profissional** e o envia para aprovação (fiscal/cliente) via **WhatsApp/compartilhamento nativo** ou para a nuvem. Camadas de gestão consomem **dashboards e indicadores** (avanço físico, HH, produtividade, faróis de prazo, Curva S, Pareto de paralisações).

**Stack técnica identificada:**

| Camada | Tecnologia no MVP |
|---|---|
| UI | React (minificado, inline) |
| Empacotamento | Bundle único `index.html` (sem build tooling exposto) |
| Persistência local | `localStorage` |
| Offline/PWA | `serviceWorker` |
| Backend / sincronização | Google Apps Script (`/exec`) → Google Sheets |
| Geração de PDF | jsPDF + html2canvas + jsPDF-AutoTable |
| Exportação de planilha | SheetJS (XLSX) |
| Clima | Geolocalização (GPS) + Open-Meteo |
| Compartilhamento | `navigator.share` (Web Share API) + fluxo manual WhatsApp/Drive |

---

## 2. Funcionalidades existentes

### 2.1 Autenticação e papéis
- Login por **seleção de nome + PIN pessoal** ("Escolha seu nome e digite o PIN").
- **PIN de gestor** separado ("PIN do gestor", "Gerenciar / apagar (PIN)").
- **Senha de administração** para operações destrutivas ("Senha de administração", "Apagar (admin)").
- **Cadastro de equipe por PIN** ("Cadastrar equipe (PIN)").
- Papéis identificados: **Encarregado, Engenheiro, Gestor, Administrativo, Fiscal do Cliente, Técnico de Segurança** — além de funções operacionais (Soldador, Caldeireiro, Montador, Mecânico, Isolador, Pintor Industrial).
- Vínculo usuário↔obras: "Obras que este usuário responde".

### 2.2 Cadastro de Obras
- Identificação da obra, contrato, cliente, escopo, prazo.
- Lista de obras salvas localmente e sincronizadas ("Obras cadastradas", "Nenhuma obra salva ainda").
- **Importar cronograma (Excel/CSV)** e **Buscar escopo da nuvem**.
- Itens de escopo com **quantidade prevista** e **HH previsto** (base para avanço).
- Exclusão controlada: apagar obra, apagar RDO específico, "Limpar obras não enviadas".

### 2.3 RDO — módulo principal
Campos e blocos confirmados nos textos de UI:
- **Cabeçalho:** Número do RDO, Data, Dia da Semana, Obra, Encarregado.
- **Turno / Jornada:** Hora início, Hora fim, Hora almoço, Retorno almoço, Hora Extra, Hora término.
- **Clima:** Condições climáticas (Ensolarado, Parcialmente Nublado, Chuva Fraca/Forte, Vento), observações climáticas, **busca automática por GPS**.
- **Frente de Serviço** e **Atividades** (com "Serviço extra fora do escopo").
- **Efetivo / Mão de Obra:** efetivo por função, efetivo lançado, efetivo médio, HH.
- **Equipamentos:** equipamentos utilizados, horas utilizadas, equipamento indisponível; itens específicos (Máquina de Solda, etc.).
- **Paralisações:** por motivo (Falta de Material, Falta de mão de obra, Chuva, Equipamento indisponível, Área não liberada/isolada), com horário e motivo.
- **Segurança:** DDS realizado, APR/PT emitida, itens de segurança marcados, equipamentos inspecionados, Não Conformidade, observações de segurança.
- **Documentação:** PT, APR, projetos, liberações.
- **Anexos:** Fotos (múltiplas), Assinaturas (canvas, com "Limpar assinatura").
- **Observações / Dificuldades encontradas.**
- **Planejamento do Próximo Dia:** Recursos necessários, Mão de obra necessária, Pendências, materiais pendentes.

### 2.4 Aprovação
- Fluxo de status: **Aguardando Liberação → Enviar p/ aprovar → Aprovado**.
- **Enviar ao fiscal p/ aprovar (WhatsApp)**.
- **Aprovação de RDO** por gestor/fiscal ("Aprovar este RDO", "Aprovados").
- "Revisão do RDO" e "Alteração do cliente".

### 2.5 Geração de documentos
- **PDF profissional** do RDO (jsPDF + html2canvas + AutoTable): "Gerar PDF profissional", "Abrir PDF", "Abrir PDF no Drive", "Compartilhar PDF (nativo)".
- **Exportação Excel** (SheetJS).

### 2.6 Sincronização / nuvem
- **Enviar RDO para a nuvem**, **Atualizar da nuvem**, **Buscar escopo da nuvem**, "Sincronizando obras da nuvem…".
- Configuração via **URL da nuvem** (endpoint Apps Script colável pelo usuário).
- Offline-first: opera sem internet e sincroniza ao reconectar.

### 2.7 Dashboard e indicadores
- **Dashboard** com "Indicadores do Dia" e "Ver dashboard completo".
- **Avanço da obra:** físico, por HH, por quantidade.
- **Curva S (previsto x realizado)** e curva de avanço realizado por HH.
- **Faróis das obras (prazo x avanço)**.
- **Paralisações por motivo (Pareto)**.
- HH previsto x realizado, efetivo médio, produtividade, meta.

---

## 3. Fluxos operacionais

**Fluxo 1 — Preenchimento diário (encarregado):**
Login (nome + PIN) → seleciona obra → Novo RDO (ou "Continuar RDO em andamento") → preenche turno/clima → frente/atividades → efetivo por função → equipamentos/horas → paralisações → segurança → fotos/assinaturas → observações/dificuldades → planejamento do próximo dia → Gerar PDF → Enviar (nuvem/WhatsApp).

**Fluxo 2 — Aprovação (fiscal/cliente):**
Recebe RDO (WhatsApp/nuvem) → revisa → Aprovar / solicitar alteração → status atualizado.

**Fluxo 3 — Gestão (engenheiro/gestor/diretoria):**
Atualiza da nuvem → Dashboard → analisa avanço, Curva S, faróis, Pareto de paralisações, produtividade → planeja recursos.

**Fluxo 4 — Cadastro/administração (gestor/admin):**
Cadastra obra → importa cronograma/escopo → cadastra equipe (PIN) → gerencia/apaga com PIN/senha admin.

---

## 4. Regras de negócio identificadas

1. **Acesso por PIN**, com níveis distintos (pessoal / gestor / senha de administração para ações destrutivas).
2. **Usuário responde por um subconjunto de obras** — não vê todas.
3. **Avanço calculado de duas formas:** por **HH** (previsto x realizado) e por **quantidade** (qtd prevista x realizada por item de escopo).
4. **HH derivado do efetivo x horas trabalhadas líquidas** (descontando almoço; horas extras somam).
5. **Serviço extra** é marcado como "fora do escopo" — separado do avanço contratual.
6. **Paralisações** possuem motivo categorizado e alimentam o **Pareto** e o cálculo de horas improdutivas.
7. **Fluxo de aprovação com estados** (Aguardando → Enviado → Aprovado), com trilha de alteração do cliente.
8. **Farol de obra** combina prazo x avanço (verde/amarelo/vermelho — inferido).
9. **Offline-first:** o dado nasce local e é promovido à nuvem; "obras não enviadas" existem como estado intermediário.
10. **Curva S** compara avanço previsto (cronograma importado) x realizado acumulado.

---

## 5. Pontos fortes

- **Fluxo de campo maduro e validado** — cobre RDO de ponta a ponta com riqueza de domínio industrial (DDS, APR/PT, paralisações categorizadas, não conformidade).
- **Offline-first real** — funciona em obra sem sinal, essencial para o público.
- **PDF profissional gerado no cliente** — entregável concreto que o mercado reconhece.
- **Indicadores certos** — HH previsto x realizado, Curva S, Pareto de paralisações e faróis são exatamente os KPIs que engenharia de obras usa.
- **Duas dimensões de avanço** (HH e quantidade) — sofisticação rara em MVPs.
- **Compartilhamento nativo/WhatsApp** — adoção sem fricção no comportamento real do usuário.
- **Domínio bem modelado**: funções, equipamentos e motivos de paralisação já refletem caldeiraria/montagem/tubulação/pintura.

---

## 6. Limitações da arquitetura atual

| # | Limitação | Impacto |
|---|---|---|
| 1 | **Bundle único de 1,37 MB minificado inline** | Impossível manter/evoluir; sem modularidade; carga inicial pesada. |
| 2 | **Backend em Google Apps Script + Sheets** | Não escala para múltiplas empresas; sem transações; limites de cota; latência; sem consultas relacionais. |
| 3 | **`localStorage` como banco** | Capacidade limitada (~5–10 MB), síncrono, sem índices, some ao limpar navegador; fotos estouram o limite. |
| 4 | **URL do Apps Script exposta em texto claro** no HTML público | ⚠️ **Risco de segurança** — endpoint gravável exposto; qualquer pessoa pode escrever/ler dados. Deve ser revogado. |
| 5 | **Sem multi-tenant real** | Isolamento por empresa inexistente; tudo compartilha o mesmo backend/planilha. |
| 6 | **Autenticação por PIN sem hashing/servidor** | Sem gestão de sessão robusta, sem tokens, sem expiração; frágil para B2B. |
| 7 | **Sem controle de versão do dado / auditoria** | Sheets não registra quem alterou o quê e quando de forma confiável. |
| 8 | **Fotos/assinaturas provavelmente em base64** no dado | Infla o payload, estoura localStorage e Sheets; sem storage de objetos (R2). |
| 9 | **Sem testes automatizados** | Regressões silenciosas a cada mudança. |
| 10 | **Sincronização sem resolução de conflito robusta** | Dois dispositivos editando a mesma obra offline podem sobrescrever. |
| 11 | **Sem separação frontend/backend/domínio** | Regras de negócio acopladas à UI. |

---

## 7. Funcionalidades que devem permanecer (preservar a lógica validada)

- Todo o **conjunto de campos e blocos do RDO** (seção 2.3).
- **Dois modos de avanço** (HH e quantidade) e o cálculo de HH líquido.
- **Motivos de paralisação categorizados** e o **Pareto**.
- **Curva S** e **faróis de prazo x avanço**.
- **Fluxo de aprovação em estados** (Aguardando → Enviado → Aprovado → Alteração do cliente).
- **DDS / APR / PT / Não Conformidade** e demais itens de segurança.
- **Planejamento do próximo dia** (recursos, mão de obra, pendências, materiais).
- **Geração de PDF profissional** e exportação Excel.
- **Offline-first** com sincronização.
- **Clima automático por GPS.**
- **Compartilhamento nativo / WhatsApp** como opção de envio.
- **Vínculo usuário ↔ obras** e níveis de acesso.

---

## 8. Funcionalidades que podem ser melhoradas

1. **Autenticação:** PIN → identidade real (e-mail/telefone + senha com hash + JWT), mantendo **PIN como atalho de campo** por cima do usuário autenticado.
2. **Anexos:** base64 → upload direto para **Cloudflare R2** com URLs assinadas; compressão de imagem no cliente.
3. **PDF:** gerar no **backend (QuestPDF)** para consistência, tamanho e cabeçalho/rodapé corporativo — mantendo preview rápido no cliente.
4. **Sincronização:** modelo explícito de fila offline + resolução de conflito (versionamento por registro / last-write com detecção).
5. **Dashboard:** de imagens estáticas para **gráficos interativos** com filtros por obra/período/equipe e drill-down.
6. **Importação de cronograma:** validação e mapeamento de colunas assistido em vez de importação crua de Excel/CSV.
7. **Aprovação:** portal do fiscal/cliente com link seguro em vez de depender de PDF por WhatsApp.
8. **Multiempresa:** isolamento por tenant em todas as entidades.

---

## 9. Funcionalidades que estão faltando (para virar SaaS comercial)

- **Multi-tenant** com isolamento por empresa (usuários, obras, clientes, equipes, equipamentos, documentos, indicadores).
- **Gestão de empresas/filiais/logo/configurações** (Módulo Empresas).
- **Cadastro robusto de clientes** (Módulo Clientes).
- **Histograma de efetivo** e gestão de equipes/funções (Módulo Equipes).
- **Módulo Equipamentos completo:** próprios x locados, horas, custos, manutenção.
- **Repositório de documentos** (projetos, desenhos, procedimentos) com versionamento.
- **Módulo Produtividade** consolidado (HH, Kg, toneladas, m², comparativos).
- **Dashboards por papel** (Diretoria/Engenharia/Fiscal/Cliente/Encarregado) com rankings.
- **Módulo Planejamento** estruturado (restrições, materiais, equipe/equipamento do dia seguinte).
- **Módulo Custos** (previsto x realizado, curva S financeira) — preparado para integração futura com orçamento.
- **Trilha de auditoria** e histórico de alterações.
- **Notificações** (aprovação pendente, RDO não enviado, atraso).
- **Gestão de assinatura/planos/billing** (é um SaaS a ser vendido).
- **Preparação de arquitetura para IA** (sem implementar): geração automática de RDO, resumo diário, previsão de atraso, perguntas em linguagem natural.

---

## 10. Sugestões de evolução (para sua aprovação — não implementar ainda)

Cada item abaixo carrega justificativa técnica/operacional. **Aguardo sua decisão** antes de incorporar a qualquer documento.

1. **Modelo de dados de RDO orientado a "linhas de apontamento"** (efetivo, equipamento e atividade como registros filhos) em vez de campos planos — habilita histograma, produtividade e custos sem redesenho.
2. **Catálogo corporativo reutilizável** (funções, equipamentos, motivos de paralisação, itens de segurança) por tenant — reduz digitação em campo e padroniza indicadores.
3. **Assinatura eletrônica com carimbo de data/hora e geolocalização** para valor jurídico do RDO.
4. **Portal do cliente/fiscal** com aprovação por link seguro e trilha — elimina dependência de WhatsApp para o aceite formal.
5. **Motor de indicadores no backend** (materialized views/Redis) para dashboards rápidos com muitos RDOs.
6. **Sincronização por fila com idempotência** (cada RDO com UUID gerado no cliente) — evita duplicidade e resolve conflito offline.
7. **Camada de eventos/append-only** desde já — barato agora, e é o alicerce para a IA (resumo diário, previsão de atraso) sem reprocessar o banco transacional.

---

## 11. Pontos a confirmar com o negócio (bloqueiam decisões de modelagem)

1. **Cálculo exato de HH:** hora-almoço sempre descontada? Hora extra entra no HH realizado ou é indicador à parte?
2. **Farol de obra:** quais faixas percentuais definem verde/amarelo/vermelho?
3. **Avanço físico:** quando há HH e quantidade, qual é a fonte oficial do % de avanço da obra?
4. **Estados de aprovação:** o "Alteração do cliente" reabre o RDO para edição do encarregado ou gera revisão nova?
5. **Numeração do RDO:** sequencial por obra, por empresa ou global? Reinicia por ano?
6. **Serviço extra:** entra em produtividade/custos ou fica segregado do contrato?
7. **Papel do "Fiscal do Cliente":** é usuário do tenant da construtora ou do tenant do cliente?

---

## 12. Alerta de segurança imediato

> O `index.html` público contém uma **URL de deployment do Google Apps Script** (`.../exec`) em texto claro. Como esse endpoint grava e lê dados, **recomendo revogar/rotacionar esse deployment** antes de qualquer divulgação do repositório. Não é necessário para a nova arquitetura e representa exposição de escrita não autenticada.

---

### Observação final
Esta análise cumpre a **PRIMEIRA TAREFA** do prompt mestre e **não contém código**. Como os documentos da fase 2 (PRD, Arquitetura, Modelo de Dados, etc.) já existem na pasta do projeto, o próximo passo lógico é **cruzar esta análise com esses documentos** para validar se eles preservam integralmente a lógica do MVP — em especial os "Pontos a confirmar" da seção 11.
