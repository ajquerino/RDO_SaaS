# Catalogo de Campos — por tela (valida o MVP)

Legenda: [obrig] obrigatorio | [auto] preenchido automatico | [calc] calculado.

## Login
nome/email; PIN/senha [obrig].

## Identificacao
numeroRdo [auto/edit]; cliente [obrig]; contrato [obrig]; ordemServico [obrig];
obra [obrig]; local [obrig]; frenteServico [obrig]; data [obrig]; diaSemana [auto];
responsavel [auto do usuario/edit]. Seletor de obra cadastrada (preenche tudo).

## Clima
condicoes (multi: Ensolarado, Parc. Nublado, Chuva Fraca, Chuva Forte, Neblina,
Vento); temperatura; observacoes. Botao clima automatico (GPS).

## Efetivo (lista)
funcao [obrig]; quantidade; entrada; saida; horaExtra; obs. -> total colaboradores [calc].

## Jornada
inicio; almoco; retorno; termino -> horasTrabalhadas [calc].

## Paralisacoes (lista)
inicio; fim; motivo; descricao -> tempoParado por item e total [calc].

## Recursos (lista)
equipamento; quantidade; horas; obs.

## Servicos (lista, vinculado a EAP)
itemEscopo (opcional); atividade [obrig]; local; qtdExec OU pctInformado OU etapas
(checkbox por disciplina) OU status Concluido; unidade; status (Concluido/Em andamento/
Em espera/Nao iniciado); motivoHold (se Em espera); obs. -> % do item [calc].

## Retrabalho (lista)
atividade [obrig]; local; quantidade; unidade; pessoas; horas -> HH perdido [calc];
causa [obrig]; origem; descricao; acaoCorretiva.

## Ocorrencias
texto livre.

## Dificuldades
tipos (chips); descricao.

## Pendencias (lista)
pendencia; responsavel; prazo; status (Aberta/Em andamento/Concluida).

## Proximo Dia
maoObra; equipamentos; materiais; ferramentas; consumiveis; documentacao.

## Planejamento
servicos; prioridades; sequencia; areas; observacoes.

## Fotos (lista)
imagem [obrig]; categoria; descricao; data [auto]; hora [auto]; gps [auto].

## Seguranca
dds; apr; pt; areaIsolada; epis; ferramentasInsp; equipInsp (checkbox); observacoes.

## Assinaturas
encarregado {nome, img}; supervisor {nome, img}; fiscal {nome, img}.

## Cadastro de Obras
cliente; obra; contrato; ordemServico; local; frenteServico; responsavelPadrao;
dataInicio; dataFim; prazoPagamento (ex.: 30 ou 21/42); observacoes.
Itens EAP: descricao; unidade; qtdPrev; hhPrev; valor; disciplina; dataInicio; dataFim.

## Usuarios
nome; funcao (Encarregado/Lider/Supervisor/Planejador/Gestor); pinPessoal; obras[];
ativo.

## Plano de Faturamento (por obra/contrato)
valorContrato; condicaoPagamento (ex.: 21/42, 21/35/42/60, 30/45/60/180, a vista);
eventos[]: tipo (assinatura/entrada/mobilizacao/canteiroMensal/medicaoPeriodica/
comissionamento/entregaTecnica/entregaFinal/outro); base (%|valor); percentual|valor;
gatilho (data|evento|porMedicao); dataPrevista; recorrencia (quinzenal|mensal).

## Boletim de Medicao
obra; de; ate -> por item: %ini/%fim, valor, medidoPeriodo, medidoAcum [calc];
totais: contrato, acumulado, periodo, %fisico, %financeiro; parcelas/vencimentos [calc]
(pela condicao de pagamento aplicavel). Aprovacao por link/token.

## Media do RDO (fotos e videos)
tipo (foto|video); arquivo [obrig]; categoria; descricao; data/hora/gps [auto];
duracao [auto, video].
