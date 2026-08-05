# Modelo de Dados — PostgreSQL (IndustrialOS)

## Estrategia multi-tenant
`tenant_id uuid` em TODAS as tabelas de negocio + Global Query Filter no EF Core
(`WHERE tenant_id = @currentTenant`). Chaves primarias `uuid` (gen_random_uuid()).
Timestamps `created_at/updated_at`; soft delete opcional (`deleted_at`).
Indices por `tenant_id` e por chaves de busca (obra_id, data).

## Entidades principais (resumo de colunas)

### tenants (empresas assinantes / SaaS)
id, nome, cnpj, plano, status, criado_em.

### empresas  (a empresa do cliente; 1 tenant pode ter 1..n empresas/filiais)
id, tenant_id, razao_social, nome_fantasia, cnpj, logo_url, matriz_id (self-fk p/ filial), config_json.

### usuarios
id, tenant_id, empresa_id, nome, email (unico por tenant), senha_hash, funcao
(enum: encarregado, lider, supervisor, planejador, gestor, admin), ativo, ultimo_login.

### permissoes / usuario_permissoes  (RBAC)
roles(id, tenant_id, nome), permissions(id, chave), role_permissions(role_id, permission_id),
usuario_roles(usuario_id, role_id). Alternativa simples: funcao + policy no backend.

### usuario_obras  (vinculo N:N)
usuario_id, obra_id.

### clientes
id, tenant_id, nome, cnpj, contato, endereco.

### obras
id, tenant_id, cliente_id, empresa_id, nome, contrato, ordem_servico, local,
frente_servico, responsavel_padrao, data_inicio, data_fim, prazo_pagamento,
status (planejada/andamento/paralisada/concluida), latitude, longitude.

### obra_itens  (EAP / escopo — item do cronograma)
id, tenant_id, obra_id, descricao, unidade, qtd_prevista, hh_previsto, valor,
disciplina (fk/enum), data_inicio, data_fim, ordem, peso (opcional).

### disciplinas / etapas_credito  (regras de credito por disciplina)
disciplinas(id, tenant_id, nome), etapas(id, disciplina_id, nome, peso_pct, ordem).

### equipes / funcionarios
funcionarios(id, tenant_id, nome, funcao, matricula, ativo).
funcoes(id, tenant_id, nome, custo_hh opcional).

### equipamentos
id, tenant_id, nome, tipo, proprio_locado, horimetro, custo_hora, status_manutencao.

### documentos
id, tenant_id, obra_id, tipo (projeto/desenho/procedimento/arquivo), nome, r2_key, versao.

### rdos  (o RELATORIO — cabecalho)
id, tenant_id, obra_id, numero (sequencial por obra), revisao (int, 0=original), data,
dia_semana, turno, responsavel_usuario_id, clima_json, jornada_json, ocorrencias,
dificuldades_json, proximo_dia_json, planejamento_json, seguranca_json,
status (rascunho/enviado/revisao_solicitada/em_revisao/aprovado), token_aprovacao,
aprovado_por, aprovado_em, pdf_r2_key, enviado_por, enviado_em.

### rdo_revisoes  (historico de revisoes — mantem revisao anterior)
id, rdo_id, revisao, status_no_momento, pdf_r2_key, snapshot_json (conteudo congelado),
solicitado_por, motivo_revisao (texto do fiscal/cliente ao pedir ajuste), criado_em.
> Fluxo: fiscal pode "Solicitar revisao" (grava motivo) -> RDO volta ao encarregado
> como `revisao_solicitada`; ao reeditar/reenviar, `revisao`+1 e a versao anterior fica
> registrada em rdo_revisoes. Aprovacao sempre age sobre a revisao corrente.

### rdo_efetivo        (id, rdo_id, funcao, quantidade, entrada, saida, hora_extra, obs)
### rdo_paralisacoes   (id, rdo_id, inicio, fim, motivo, descricao)
### rdo_recursos       (id, rdo_id, equipamento_id/nome, quantidade, horas, obs)
### rdo_servicos       (id, rdo_id, obra_item_id (nullable p/ extra), atividade, local,
                        qtd_exec, unidade, status, pct_informado, etapas_feitas_json,
                        motivo_hold, obs)
### rdo_retrabalho     (id, rdo_id, atividade, local, qtd, unidade, pessoas, horas,
                        causa, origem, descricao, acao_corretiva)
### rdo_pendencias     (id, rdo_id, descricao, responsavel, prazo, status)
### rdo_midia          (id, rdo_id, tipo (foto/video), r2_key, thumb_key, categoria,
                        descricao, data, hora, lat, lon, duracao_seg (video), tamanho_bytes)
                        -- substitui/generaliza rdo_fotos; suporta VIDEO (validado no MVP).
### rdo_assinaturas    (id, rdo_id, papel (encarregado/supervisor/fiscal), nome, img_r2_key)

### faturamento_planos  (plano de faturamento POR CONTRATO/OBRA — configuravel)
id, tenant_id, obra_id, nome, valor_contrato, base_reajuste (opcional), obs.
> O faturamento varia por cliente/contrato. O plano e uma LISTA de eventos (abaixo);
> nao existe regra unica. Cobre: % assinatura de contrato, % entrada, % mobilizacao de
> canteiro, canteiro diluido mensalmente, medicoes quinzenais/mensais, % comissionamento,
> % entrega tecnica/final, ou 100% na entrega (manutencao).

### faturamento_eventos  (marcos/parcelas do plano)
id, tenant_id, faturamento_plano_id, tipo (assinatura_contrato | entrada | mobilizacao |
canteiro_mensal | medicao_periodica | comissionamento | entrega_tecnica | entrega_final |
outro), base (percentual | valor_fixo), percentual, valor, gatilho (data | evento |
por_medicao), data_prevista, recorrencia (nulo | quinzenal | mensal), ordem, descricao.
> `condicao_pagamento` (dias/parcelas) vem do evento OU da obra (default) — ver abaixo.

### condicoes_pagamento  (regra de vencimento reutilizavel)
id, tenant_id, nome (ex.: "21/35/42/60", "30/45/60/180", "100% a vista"),
parcelas_json (lista de {dias, pct}; se pct omitido, divide igualmente).
> Ex.: "21/42" = [{21,50},{42,50}]; "21/35/42/60" = 4x25%; "100% na entrega" = [{X,100}].
> Vinculavel na obra (default) e sobrescrevivel por evento de faturamento.

### medicoes (boletins de medicao — para tipo medicao_periodica)
id, tenant_id, obra_id, faturamento_evento_id (nullable), numero (sequencial por obra),
de, ate, valor_contrato, medido_acumulado, valor_periodo, pct_fisico, pct_financeiro,
pdf_r2_key, status (emitido/aprovado), token_aprovacao, criado_por, aprovado_por,
aprovado_em. UNIQUE(obra_id, numero).
### medicao_itens (id, medicao_id, obra_item_id, pct_ini, pct_fim, valor, medido_periodo, medido_acum)
### medicao_parcelas (id, medicao_id, dias, vencimento, valor, pct)
> Geradas a partir da condicao_pagamento aplicavel (evento > obra).

### avanco_snapshots  (materializacao p/ dashboard rapido)
id, tenant_id, obra_id, data, pct_fisico_hh, pct_fisico_qtd, hh_acumulado. (atualizado
por evento ao enviar RDO — evita recalcular lendo tudo.)

## Relacionamentos (essencial)
tenant 1:N empresas 1:N obras; obra 1:N obra_itens; obra 1:N rdos; rdo 1:N (efetivo,
servicos, paralisacoes, recursos, retrabalho, pendencias, fotos, assinaturas);
obra 1:N medicoes 1:N medicao_itens/parcelas; usuario N:N obra.

## Formulas de dominio (implementar no backend, fonte unica)
- pct_item = max(concluido?1, pct_informado/100, qtd_exec/qtd_prev, soma_etapas/100).
- avanco_obra_hh = sum(hh_prev*pct)/sum(hh_prev) (fallback qtd, depois media).
- pct_financeiro = medido_acumulado / valor_contrato.
- medido_periodo_item = (pct_fim - pct_ini) * valor_item.
- parcela.valor = valor_total * (pct/100); parcela.vencimento = data_base + dias (data_base
  = fim do periodo da medicao, ou data do evento de faturamento).
- desvio_prazo = pct_tempo_decorrido - pct_avanco_fisico.
- farol: verde se pct_avanco>=100 ou desvio_prazo <= LIMIAR_AMARELO; amarelo se
  LIMIAR_AMARELO < desvio_prazo <= LIMIAR_VERMELHO; vermelho se desvio_prazo >
  LIMIAR_VERMELHO ou prazo estourado; cinza sem datas.
  Defaults: LIMIAR_AMARELO=10, LIMIAR_VERMELHO=25 (configuraveis por tenant — a confirmar).
- curva_s_previsto(dia) = sum(hh_prev * clamp((dia-ini)/(fim-ini),0,1)) / sum(hh_prev).
