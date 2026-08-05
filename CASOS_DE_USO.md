# Casos de Uso — IndustrialOS

Formato: Ator | Objetivo | Pre-condicao | Fluxo principal | Regras.

## Autenticacao e acesso
UC01 Login — Usuario | acessar | cadastrado/ativo | informa email/nome+senha ->
JWT (access+refresh) -> carrega tenant e obras vinculadas | senha hash; RBAC.
UC02 Trocar de empresa/filial (multi) — Usuario com acesso a varias.
UC03 Recuperar senha — via e-mail (token).

## Empresas / Configuracao (Admin da empresa)
UC10 Cadastrar empresa/filial, logo, config.
UC11 Gerenciar usuarios e permissoes (RBAC).
UC12 Cadastrar clientes.

## Obras / EAP
UC20 Cadastrar obra (contrato, datas, prazo de pagamento, localizacao).
UC21 Montar EAP item a item (desc, unidade, qtd, HH, valor, disciplina, datas).
UC22 Importar cronograma (Excel/CSV) -> cria itens em lote.
UC23 Vincular usuarios a obras.
UC24 Anexar documentos (projetos/desenhos/procedimentos) na obra.

## RDO (nucleo)
UC30 Iniciar RDO — Encarregado | numero sugerido por obra; responsavel = usuario.
UC31 Preencher secoes (identificacao, clima [auto GPS], efetivo, jornada,
paralisacoes, recursos, servicos, retrabalho, ocorrencias, dificuldades, pendencias,
proximo dia, planejamento, fotos, seguranca, assinaturas) — salvamento automatico.
UC32 Lancar servico vinculado ao item da EAP e informar avanco (qtd / % / etapas /
concluido) — avanco do item = maior valor; oculta itens 100%.
UC33 Finalizar e enviar — valida obrigatorios, gera PDF (QuestPDF), sobe midia (R2),
grava, gera token de aprovacao, atualiza snapshot de avanco.
UC34 Editar RDO enviado — bloqueado se aprovado (servidor); se "revisao_solicitada",
editar gera nova revisao (Rev.N+1) e arquiva a anterior no historico.
UC35 Trabalhar offline e sincronizar ao reconectar (fila + conflito).

## Aprovacao
UC40 Fiscal/Cliente aprova RDO por link com token — ve so aquele RDO + PDF, assina/
aprova; RDO fica bloqueado. UC41 Historico e selo de aprovacao.
UC42 Fiscal/Cliente **solicita revisao** por link (informa motivo) — RDO volta ao
encarregado como "revisao_solicitada" para gerar nova revisao.
UC43 Fiscal/Cliente **aprova medicao** por link com token (mesmo padrao do RDO).

## Medicao / Faturamento
UC49 Configurar plano de faturamento da obra (Planejador/Gestor) — eventos (% assinatura,
entrada, mobilizacao, canteiro mensal, medicao periodica, comissionamento, entrega) +
condicao de pagamento (ex.: 21/42, 30/45/60/180, a vista). Varia por contrato/cliente.
UC50 Emitir boletim de medicao (Planejador/Gestor) — obra + periodo -> calcula por
item (%ini/%fim, medido no periodo = variacao x valor) + parcelas/vencimentos pela
condicao de pagamento aplicavel -> PDF -> salva numerado. UC51 Apagar boletim — so Admin.

## Dashboards / Indicadores
UC60 Dashboard por perfil (diretoria/engenharia/fiscal/cliente/encarregado):
avanco fisico/financeiro, produtividade, equipes, equipamentos, paralisacoes,
Pareto, curva S, farois, rankings (obras/equipes/produtividade).

## Planejamento / Custos
UC70 Necessidades do proximo dia (mao de obra, equipamentos, materiais, restricoes).
UC71 Custos previsto x realizado, histograma HH, curva S financeira (preparado).

## Administracao
UC80 Apagar RDO/obra/tudo (Admin, senha propria; auditado). UC81 Auditoria/logs.
