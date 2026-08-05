# Wireframes (descricao tela a tela) — baseados no MVP

Convencao: [Botao] {campo} <lista> (secao). Mobile-first (coluna unica).

## Login
Logo / "RDO". {Seu nome: seletor de usuarios | Outro/Gestor} {PIN pessoal}
[Entrar]. Hint: "Gestor: nome + PIN do gestor".

## Home
Logo + nome empresa. "Ola, {nome} ({funcao})". [+ Novo RDO] [Continuar RDO?].
Grade de cards: [Historico][Dashboard] e (se gestor/planejador) [Boletim]
[Cadastro de Obras][Usuarios][Administracao]. (Farois das obras) <bolinha cor,
obra, dias restantes, avanco %>. [Ver dashboard]. [Sair].

## Topo (dentro do RDO)
[Menu ☰][Inicio ⌂] Logo/empresa  "Salvo auto". Barra de indicadores (colaboradores,
HH, paralisacao, retrabalho, fotos, pendencias). Rodape: [Anterior][Novo][Proximo/
Finalizar].

## Identificacao
(Obras cadastradas: seletor) [Atualizar da nuvem]. {N RDO}{Cliente*}{Obra*}{Contrato*}
{OS*}{Local*}{Frente*}{Data*}{Dia}{Responsavel*}.

## Clima
[Buscar clima automatico (GPS)]. (Chips condicoes) {Temperatura} {Observacoes}.

## Efetivo
{Funcao}{Qtd}{Entrada}{Saida}{HoraExtra}{Obs}[+ Adicionar]. <lista + total>.

## Jornada
{Inicio}{Almoco}{Retorno}{Termino} -> (Horas trabalhadas).

## Paralisacoes
{Inicio}{Fim}{Motivo}(tempo){Descricao}[+ Adicionar]. <lista + total>.

## Recursos
{Equipamento}{Qtd}{Horas}{Obs}[+ Adicionar]. <lista>.

## Servicos
(Item do escopo: seletor com % e ocultar concluidos) {Atividade}{Local}
{Qtd executada | % concluido | Etapas checkbox}{Unidade}{Status}. Se "Em espera":
{Motivo}. {Obs}[+ Adicionar]. <lista com status/escopo>.

## Retrabalho
{Atividade}{Local}{Qtd}{Un}{Pessoas}{Horas}(HH){Causa}{Origem}{Descricao}{Acao}
[+ Adicionar]. <lista + HH/% total>.

## Ocorrencias / Dificuldades / Pendencias / Proximo Dia / Planejamento
Campos livres / chips / lista conforme catalogo de campos.

## Fotos
[Tirar foto][Galeria]. <grade: imagem, {Categoria}{Descricao}, data/hora/GPS, [X]>.

## Seguranca
<checklist DDS/APR/PT/Area/EPIs/Ferramentas/Equip> {Observacoes}.

## Assinaturas
(canvas Encarregado){Nome} (canvas Supervisor){Nome} (canvas Fiscal){Nome}.

## Exportacao
(Revisao: linhas de resumo com alertas). [Gerar PDF][Compartilhar][WhatsApp][E-mail]
[Imprimir]. (Salvar na nuvem: URL) [Enviar RDO]. Rodape: [Finalizar e enviar].

## Historico
[Atualizar]{Filtrar por obra}. <grupos Cliente-Obra: RDO N, data, selo Aprovado/
Pendente, [Abrir PDF][Editar][Enviar p/ aprovar]>.

## Dashboard
{Obra}. (Farois). (KPIs). (Avanco HH/qtd + itens). (Curva S previsto x realizado).
(HH/dia)(Servicos por status)(Pareto paralisacao)(Pareto retrabalho).

## Cadastro de Obras (gestor)
{PIN} -> {dados da obra + prazo pagamento}. [Importar cronograma]. (Item: {Desc}
{Un}{Qtd}{HH}{Valor}{Disciplina}{DataIni}{DataFim}[+]). <lista de itens>. [Salvar].
<obras cadastradas: [Editar][Apagar]>.

## Usuarios (gestor)
{PIN} -> {Nome}{Funcao}{PIN pessoal}<obras checkbox><ativo>[Salvar]. <lista usuarios>.

## Boletim (planejamento/gestor)
{Obra}{De}{Ate}[Gerar]. (por item %ini->%fim, valor, no periodo). (Totais + faturamento/
parcelas). [Gerar PDF][Salvar]. <boletins emitidos: [Abrir PDF][Apagar(admin)]>.

## Administracao (admin)
{Senha admin} -> [Zerar tudo] {Obra}[Apagar obra] <RDOs: [Apagar]>.

## Aprovacao (tela isolada, link)
"Aprovacao de RDO". (obra, cliente, data, responsavel, revisao, selo). [Abrir PDF do RDO].
{Nome}{Funcao} [Aprovar] [Solicitar revisao -> {Motivo}]. (Mesma tela serve p/ medicao.)

## Faturamento (planejamento/gestor)
{Obra}{ValorContrato}{CondicaoPagamento: seletor/nova}. <eventos: {Tipo}{%|Valor}
{Gatilho}{DataPrevista}{Recorrencia}[+]>. [Salvar plano].
