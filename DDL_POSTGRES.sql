-- DDL essencial (PostgreSQL) — IndustrialOS. Todas as tabelas de negocio tem tenant_id.
create extension if not exists "pgcrypto";

create table tenants ( id uuid primary key default gen_random_uuid(), nome text not null,
  cnpj text, plano text, status text default 'ativo', criado_em timestamptz default now());

create table empresas ( id uuid primary key default gen_random_uuid(), tenant_id uuid not null references tenants(id),
  razao_social text not null, nome_fantasia text, cnpj text, logo_url text, matriz_id uuid references empresas(id),
  config jsonb default '{}', criado_em timestamptz default now());
create index on empresas(tenant_id);

create table usuarios ( id uuid primary key default gen_random_uuid(), tenant_id uuid not null,
  empresa_id uuid references empresas(id), nome text not null, email text, senha_hash text not null,
  funcao text not null check (funcao in ('encarregado','lider','supervisor','planejador','gestor','admin')),
  ativo boolean default true, ultimo_login timestamptz, criado_em timestamptz default now(),
  unique (tenant_id, email));
create index on usuarios(tenant_id);

create table clientes ( id uuid primary key default gen_random_uuid(), tenant_id uuid not null,
  nome text not null, cnpj text, contato text, endereco text);
create index on clientes(tenant_id);

create table obras ( id uuid primary key default gen_random_uuid(), tenant_id uuid not null,
  cliente_id uuid references clientes(id), empresa_id uuid references empresas(id), nome text not null,
  contrato text, ordem_servico text, local text, frente_servico text, responsavel_padrao text,
  data_inicio date, data_fim date, prazo_pagamento text, status text default 'andamento',
  latitude double precision, longitude double precision, criado_em timestamptz default now());
create index on obras(tenant_id);

create table usuario_obras ( usuario_id uuid references usuarios(id), obra_id uuid references obras(id),
  primary key (usuario_id, obra_id));

create table disciplinas ( id uuid primary key default gen_random_uuid(), tenant_id uuid not null,
  nome text not null);
create table etapas_credito ( id uuid primary key default gen_random_uuid(), tenant_id uuid not null,
  disciplina_id uuid not null references disciplinas(id) on delete cascade, nome text not null,
  peso_pct numeric not null, ordem int);   -- pesos que produzem o % por etapa (ex.: Tubulacao -> 85%)

create table obra_itens ( id uuid primary key default gen_random_uuid(), tenant_id uuid not null,
  obra_id uuid not null references obras(id), descricao text not null, unidade text, qtd_prevista numeric,
  hh_previsto numeric, valor numeric, disciplina_id uuid references disciplinas(id), data_inicio date,
  data_fim date, ordem int);
create index on obra_itens(obra_id);

create table rdos ( id uuid primary key default gen_random_uuid(), tenant_id uuid not null,
  obra_id uuid not null references obras(id), numero int, data date not null, dia_semana text, turno text,
  responsavel_usuario_id uuid references usuarios(id), clima jsonb, jornada jsonb, ocorrencias text,
  dificuldades jsonb, proximo_dia jsonb, planejamento jsonb, seguranca jsonb,
  revisao int not null default 0,
  status text default 'rascunho' check (status in ('rascunho','enviado','revisao_solicitada','em_revisao','aprovado')),
  token_aprovacao text, aprovado_por text, aprovado_em timestamptz, pdf_r2_key text,
  enviado_por uuid, enviado_em timestamptz, versao int default 1, criado_em timestamptz default now(),
  unique (obra_id, numero));
create index on rdos(tenant_id); create index on rdos(obra_id, data);

create table rdo_revisoes ( id uuid primary key default gen_random_uuid(), rdo_id uuid references rdos(id) on delete cascade,
  revisao int not null, status_no_momento text, pdf_r2_key text, snapshot jsonb,
  solicitado_por text, motivo_revisao text, criado_em timestamptz default now());
create index on rdo_revisoes(rdo_id);

create table rdo_efetivo ( id uuid primary key default gen_random_uuid(), rdo_id uuid references rdos(id) on delete cascade,
  funcao text, quantidade int, entrada text, saida text, hora_extra text, obs text);
create table rdo_paralisacoes ( id uuid primary key default gen_random_uuid(), rdo_id uuid references rdos(id) on delete cascade,
  inicio text, fim text, motivo text, descricao text);
create table rdo_recursos ( id uuid primary key default gen_random_uuid(), rdo_id uuid references rdos(id) on delete cascade,
  equipamento text, quantidade int, horas text, obs text);
create table rdo_servicos ( id uuid primary key default gen_random_uuid(), rdo_id uuid references rdos(id) on delete cascade,
  obra_item_id uuid references obra_itens(id), atividade text, local text, qtd_exec numeric, unidade text,
  status text, pct_informado numeric, etapas_feitas jsonb, motivo_hold text, obs text);
create table rdo_retrabalho ( id uuid primary key default gen_random_uuid(), rdo_id uuid references rdos(id) on delete cascade,
  atividade text, local text, quantidade numeric, unidade text, pessoas int, horas numeric,
  causa text, origem text, descricao text, acao_corretiva text);
create table rdo_pendencias ( id uuid primary key default gen_random_uuid(), rdo_id uuid references rdos(id) on delete cascade,
  descricao text, responsavel text, prazo date, status text);
create table rdo_midia ( id uuid primary key default gen_random_uuid(), rdo_id uuid references rdos(id) on delete cascade,
  tipo text not null default 'foto' check (tipo in ('foto','video')), r2_key text, thumb_key text, categoria text,
  descricao text, data text, hora text, lat double precision, lon double precision, duracao_seg int, tamanho_bytes bigint);
create index on rdo_midia(rdo_id);
create table rdo_assinaturas ( id uuid primary key default gen_random_uuid(), rdo_id uuid references rdos(id) on delete cascade,
  papel text, nome text, img_r2_key text);

-- Faturamento configuravel por contrato/obra (nao ha regra unica; varia por cliente)
create table condicoes_pagamento ( id uuid primary key default gen_random_uuid(), tenant_id uuid not null,
  nome text not null, parcelas jsonb not null);  -- [{dias, pct}]; pct omitido => divide igual
create table faturamento_planos ( id uuid primary key default gen_random_uuid(), tenant_id uuid not null,
  obra_id uuid not null references obras(id) on delete cascade, nome text, valor_contrato numeric,
  condicao_pagamento_id uuid references condicoes_pagamento(id), obs text);
create table faturamento_eventos ( id uuid primary key default gen_random_uuid(), tenant_id uuid not null,
  faturamento_plano_id uuid not null references faturamento_planos(id) on delete cascade,
  tipo text not null check (tipo in ('assinatura_contrato','entrada','mobilizacao','canteiro_mensal',
    'medicao_periodica','comissionamento','entrega_tecnica','entrega_final','outro')),
  base text not null default 'percentual' check (base in ('percentual','valor_fixo')),
  percentual numeric, valor numeric, gatilho text check (gatilho in ('data','evento','por_medicao')),
  data_prevista date, recorrencia text check (recorrencia in ('quinzenal','mensal')),
  condicao_pagamento_id uuid references condicoes_pagamento(id), ordem int, descricao text);
create index on faturamento_eventos(faturamento_plano_id);

create table medicoes ( id uuid primary key default gen_random_uuid(), tenant_id uuid not null,
  obra_id uuid not null references obras(id), faturamento_evento_id uuid references faturamento_eventos(id),
  numero int, de date, ate date, valor_contrato numeric,
  medido_acumulado numeric, valor_periodo numeric, pct_fisico numeric, pct_financeiro numeric,
  pdf_r2_key text, status text default 'emitido' check (status in ('emitido','aprovado')),
  token_aprovacao text, criado_por uuid, aprovado_por text, aprovado_em timestamptz,
  criado_em timestamptz default now(), unique (obra_id, numero));
create index on medicoes(obra_id);
create table medicao_itens ( id uuid primary key default gen_random_uuid(), medicao_id uuid references medicoes(id) on delete cascade,
  obra_item_id uuid references obra_itens(id), pct_ini numeric, pct_fim numeric, valor numeric,
  medido_periodo numeric, medido_acum numeric);
create table medicao_parcelas ( id uuid primary key default gen_random_uuid(), medicao_id uuid references medicoes(id) on delete cascade,
  dias int, vencimento date, valor numeric, pct numeric);

create table avanco_snapshots ( id uuid primary key default gen_random_uuid(), tenant_id uuid not null,
  obra_id uuid references obras(id), data date, pct_fisico_hh numeric, pct_fisico_qtd numeric, hh_acumulado numeric);
create index on avanco_snapshots(obra_id, data);

create table auditoria ( id bigserial primary key, tenant_id uuid, usuario_id uuid, acao text, entidade text,
  entidade_id uuid, detalhe jsonb, criado_em timestamptz default now());
