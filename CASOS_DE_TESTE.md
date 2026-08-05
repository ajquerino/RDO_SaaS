# Casos de Teste (QA) — validam paridade com o MVP

Formato: ID | Cenario | Passos | Resultado esperado.

T01 Numeracao por obra | Enviar 2 RDOs na obra A | 1o = RDO 1, 2o = RDO 2; obra B recomeca em 1.
T02 Horas jornada | 07-12 / 13-17 | 9h00.
T03 Total efetivo | 4 soldadores + 3 montadores | 7 colaboradores.
T04 Paralisacao | 10:00-11:30 | 1h30; total soma.
T05 Avanco por qtd | item 40, exec 12 | 30%.
T06 Avanco por status | marcar Concluido | 100% (ignora qtd).
T07 Avanco por etapas | disciplina Tubulacao, marcar ate Soldagem | 85%.
T08 Item 100% oculto | concluir item | some do seletor; "mostrar concluidos" reexibe.
T09 Hold | status Em espera | exige/mostra motivo.
T10 Retrabalho | 2 pessoas x 4 h | HH 8; % sobre HH do dia.
T11 Obrigatorios | finalizar sem contrato | bloqueia e indica campo.
T12 Envio | finalizar valido | PDF gerado, registro salvo, token criado.
T13 Aprovacao | abrir link token, aprovar | status Aprovado; edicao bloqueada (409).
T14 Token invalido | abrir link errado | nao exibe RDO.
T14a Solicitar revisao | fiscal pede revisao com motivo | status revisao_solicitada; motivo salvo.
T14b Nova revisao | encarregado edita e reenvia | Rev.N+1; revisao anterior no historico.
T15 Avanco obra HH | itens com HH | soma(hh*%)/soma(hh).
T16 Avanco sem peso | itens sem HH/qtd | media dos %.
T17 Farol | avanco < tempo (>25%) | vermelho; sem datas | cinza.
T18 Data invalida | item com data ruim | calculo nao trava.
T19 Boletim | item 10.000, 20%->50% | periodo 3.000; acum 5.000.
T20 Parcelas | condicao 21/42 | 2 parcelas 50%, venc. ate+21 e ate+42.
T20a Parcelas 4x | condicao 21/35/42/60 | 4 parcelas de 25% nos dias indicados.
T20b Faturamento marco | evento mobilizacao 10% | fatura independente da medicao por avanco.
T21 Permissao BM | Planejador emite; apagar exige admin.
T22 Permissao apagar RDO | so admin (senha separada).
T23 Isolamento obra | encarregado ve so obras vinculadas.
T24 Multi-tenant | usuario do tenant A nao ve dados do tenant B.
T25 Offline | preencher offline, reconectar | sincroniza sem duplicar.
