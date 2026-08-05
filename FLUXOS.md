# Fluxos Operacionais — IndustrialOS (validados no MVP)

## F1. Onboarding do tenant
Super-admin cria tenant/plano -> Admin da empresa configura empresa/logo -> cadastra
usuarios (funcoes, PIN/senha) -> cadastra clientes e obras -> monta EAP -> vincula
usuarios as obras.

## F2. Login e contexto
Usuario abre o app -> login (nome/email + senha) -> recebe token + obras vinculadas ->
Home (atalhos + farois das obras dele).

## F3. Ciclo diario do RDO (campo)
Home -> "Novo RDO" -> Identificacao (seleciona obra cadastrada, numero automatico,
responsavel = usuario) -> preenche secoes (clima auto, efetivo, jornada, paralisacoes,
recursos, servicos vinculados a EAP com avanco, retrabalho, ocorrencias, dificuldades,
pendencias, proximo dia, planejamento, fotos, seguranca, assinaturas) -> Revisao ->
Finalizar e enviar (gera PDF, sobe midia, salva, gera token) -> envia link ao fiscal.
Salvamento automatico e offline em todo o fluxo.

## F4. Aprovacao (fiscal/cliente)
Recebe link -> abre tela isolada (so aquele RDO + PDF) -> informa nome/funcao ->
  (a) **Aprovar** -> RDO bloqueado para edicao -> selo "Aprovado" no Historico; OU
  (b) **Solicitar revisao** (informa motivo) -> RDO volta ao encarregado como
      "revisao_solicitada". O encarregado edita, gera **Rev.N+1** (a revisao anterior
      fica no historico/rdo_revisoes) e reenvia -> novo ciclo de aprovacao.
Mesma logica vale para aprovacao de **medicao** por link com token.

## F5. Avanco e indicadores
Ao finalizar RDO, atualiza snapshot de avanco da obra -> Dashboard mostra avanco
(HH/qtd), curva S (previsto x realizado), farois, Pareto, rankings, por perfil.

## F6. Medicao / faturamento (planejamento)
Boletim -> escolhe obra + periodo -> calcula (% x valor por item + variacao no
periodo) -> vencimentos por prazo de pagamento -> gera PDF -> salva numerado ->
(apagar so admin).

## F7. Edicao e correcao
Historico -> Editar RDO pendente -> salva por cima. Se aprovado, edicao bloqueada; se
"revisao_solicitada", editar gera nova revisao (mantendo a anterior no historico).

## F8. Administracao
Admin (senha propria) -> apagar RDO/obra/tudo (auditado); planejamento nao apaga RDO
(solicita ao admin).
