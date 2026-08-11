# Montaris — Divulgação (LinkedIn) e registro da sessão

## Marca
- **Nome comercial (UI, e-mails, PWA):** **Montaris**
- **Código / projetos / namespaces C#:** seguem `IndustrialOS.*` (arquitetura — NÃO renomear)
- **Descritor:** Gestão de obras industriais
- **Tagline:** Do canteiro ao faturamento.
- **Motivo do rebrand:** "IndustrialOS" já existia como app no Google. Nomes descartados por já estarem
  usados/arriscados: Avantto (aviação BR), Obrix (concorrente direto getobrix.com), Montta (~Monta EV),
  Avanzo/Praxo/Bitola/Faina (tomados/poluídos). **Canteira** ficou livre mas puxava pra obra civil.
- **A verificar antes de registrar:** domínio `montaris.com.br` (registro.br) e marca no **INPI** (classe de software).

---

## Post do LinkedIn (versão final — anexar `montaris_linkedin.png`)

Outro projeto paralelo que venho construindo — e que hoje quero mostrar: o **Montaris**.

É um SaaS para a gestão operacional de **obras industriais** (montagem, caldeiraria, tubulação, pintura, EPC), com o **RDO (Relatório Diário de Obra)** no centro.

No campo, o RDO ainda vive em papel, PDF ou planilha solta — e o dado que a equipe registra todo dia raramente vira decisão, medição ou faturamento. Foi essa dor que quis atacar.

O que já está de pé:

• RDO digital que funciona **offline** no canteiro e sincroniza depois
• Efetivo e **HH com hora-extra**, paralisações, retrabalho, segurança (DDS/APR/PT), clima, fotos e assinatura
• **EAP** importada de qualquer planilha, com avanço físico por item
• Dashboards de avanço, **Curva S**, farol e produtividade
• **Medição e faturamento** — do boletim ao recebimento
• Aprovação do RDO por link, sem o fiscal precisar de conta
• Multi-empresa, com permissões por função

Stack: **.NET 10, React + PWA**, multi-tenant — **em desenvolvimento, rodando em fase piloto**.

O mais bonito pra mim é ver o ciclo fechar: o dado que o encarregado preenche no celular, no fim do dia, **alimenta sozinho** o avanço, a medição e o faturamento da obra.

Ainda há muito a evoluir. Mas é assim que deixo de só *usar* as ferramentas do meu trabalho e passo a *construir* as que eu queria ter.

Seguimos. 🚀

\#Engenharia #GestãoDeObras #OrçamentaçãoIndustrial #RDO #EPC #DotNet #React #SaaS #Tecnologia

> **Imagem:** `marketing/montaris_linkedin.png` (1200×1200 — mockup de celular + mini-dashboard: RDO,
> avanço, Curva S, farol; selo "em desenvolvimento · piloto").

---

## O que foi entregue nesta sessão (todas validadas e em produção)
- **Página "Conheça"** pública (/conheca) + link no login.
- **Obra de exemplo** `[EXEMPLO]` semeada em toda conta (nova e existentes via backfill).
- **Guia "Leia aqui"** dentro do app (com seção "O que vem por aí").
- **Import de EAP mapeável** — sobe qualquer planilha e aponta as colunas na tela.
- **Duplicar RDO** — novo Rascunho pré-preenchido; avanço anterior fixo + incremento do dia.
- **Fotos no PDF** do RDO + vídeo como link + lightbox na tela + rota `/rdo/{id}`.
- **Layout responsivo** — nav vira drawer (hambúrguer) no mobile; fim do overflow horizontal.
- **Home (🏠) + Voltar (‹)** na navegação.
- **Rebrand → Montaris** (só user-facing; namespaces intactos).
- **Fix de segurança:** empresa **suspensa** agora é somente-leitura (loga e vê, mas não cria/edita).
- **Fix de deploy:** `index.html` com `no-cache` (PWA atualiza no deploy sem limpar cache).

## Suspensão (regra confirmada pelo dono)
Empresa **suspensa** = **somente leitura**: loga normalmente e vê/baixa tudo que já existe, mas não
cria/edita (qualquer escrita retorna 402). Reativar volta ao normal.

## Roadmap ("O que vem por aí")
- RDO em minutos (modo rápido + presets de efetivo/equipamento) — médio, próximo.
- IA industrial (resumo do RDO + risco de atraso/produtividade) — grande, planejar.
- Integração com ERP/financeiro (API/webhook sobre o outbox) — grande, planejar.
