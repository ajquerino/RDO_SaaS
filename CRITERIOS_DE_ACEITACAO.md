# Criterios de Aceitacao (Gherkin) — validam o comportamento do MVP

## RDO
- Dado que seleciono uma obra cadastrada, Quando abro a Identificacao, Entao o numero
  do RDO e sugerido como (ultimo da obra + 1) e o responsavel vem do usuario logado.
- Dado efetivo lancado, Entao o total de colaboradores e a soma das quantidades.
- Dado inicio 07:00, almoco 12:00, retorno 13:00, termino 17:00, Entao horas
  trabalhadas = 9h00.
- Dado uma paralisacao 10:00-11:30, Entao tempo parado = 1h30 e soma no total.
- Dado um servico vinculado a um item com qtdPrev 40 e qtdExec 12, Entao o item fica
  em 30%. Se marco status Concluido, fica 100%. O maior valor prevalece.
- Dado um item que atinge 100%, Entao ele some do seletor de itens (com opcao mostrar).
- Dado status "Em espera", Entao abre o campo de motivo (obrigatorio para registrar).
- Dado retrabalho com 2 pessoas x 4 horas, Entao HH perdido = 8 e entra no % de
  retrabalho do dia.
- Dado que finalizo com campos obrigatorios vazios, Entao NAO envia e volta para a
  Identificacao indicando o que falta.
- Dado que finalizo valido, Entao gera PDF, salva na nuvem, cria token e oferece link
  de aprovacao.

## Aprovacao
- Dado um link com token valido, Quando o fiscal aprova informando nome/funcao, Entao
  o RDO fica "Aprovado" e nao pode mais ser editado (servidor recusa edicao).
- Dado token invalido, Entao a tela nao abre o RDO.
- Dado que o fiscal **solicita revisao** informando um motivo, Entao o RDO fica
  "revisao_solicitada", o motivo fica registrado e volta editavel para o encarregado.
- Dado um RDO em "revisao_solicitada", Quando o encarregado edita e reenvia, Entao gera
  Rev.N+1 e a revisao anterior fica preservada no historico (rdo_revisoes).

## Avanco / Dashboard
- Dado itens com hhPrev, Entao avanco da obra = soma(hh*%)/soma(hh).
- Dado itens sem hhPrev nem qtd, Entao avanco = media simples dos % dos itens.
- Dado prazo e avanco, Entao o farol segue: verde se avanco acompanha (ou 100%);
  amarelo se desvio 10-25%; vermelho se >25% ou prazo estourado; cinza sem datas.
- Dada uma data invalida no escopo, Entao o calculo NAO trava (curva ignora datas ruins).

## Boletim de Medicao / Faturamento
- Dado item com valor 10.000 e % passando de 20% para 50% no periodo, Entao medido no
  periodo = 3.000 e medido acumulado = 5.000.
- Dada a condicao de pagamento "21/42", Entao gera 2 parcelas (50% cada) com vencimento
  em data(ate)+21 e data(ate)+42.
- Dada a condicao "21/35/42/60", Entao gera 4 parcelas de 25% nos respectivos dias.
- Dada a condicao "100% a vista", Entao gera 1 parcela na data-base.
- Dado um plano de faturamento com evento "% mobilizacao de canteiro = 10%", Entao esse
  faturamento e independente das medicoes por avanco (marco proprio).
- Dado que sou Planejador, Entao consigo configurar o plano de faturamento e emitir/ver
  o BM; apagar BM exige senha de administracao.

## Permissoes
- Dado usuario Encarregado, Entao vejo apenas as obras vinculadas e nao vejo Cadastro/
  Usuarios/Administracao.
- Dado apagar RDO, Entao exige senha de administracao (separada do gestor).

## Offline
- Dado que preencho sem internet, Quando reconecta, Entao o RDO sincroniza sem
  duplicar (idempotente) e sem perder dados.
