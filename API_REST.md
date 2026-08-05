# Contratos de API REST — IndustrialOS (.NET 10)

Convencoes: JSON; JWT Bearer; multi-tenant via claim `tenant_id`; paginacao
`?page=&size=`; filtros por querystring; respostas `{ data, meta }`; erros RFC7807.
Versao: `/api/v1`. Todos os endpoints filtram pelo tenant do token.

## Auth
POST /auth/login            {email|nome, senha} -> {accessToken, refreshToken, usuario}
POST /auth/refresh          {refreshToken} -> {accessToken}
POST /auth/recuperar        {email}

## Empresas / Usuarios / Clientes
GET/POST/PUT/DELETE /empresas
GET/POST/PUT/DELETE /usuarios            (POST cria com senha_hash)
POST /usuarios/{id}/obras                {obraIds:[]}
GET/POST/PUT/DELETE /clientes

## Obras / EAP / Documentos
GET/POST/PUT/DELETE /obras
GET /obras/{id}                          (com resumo de avanco)
GET/POST/PUT/DELETE /obras/{id}/itens    (EAP)
POST /obras/{id}/itens/importar          (multipart Excel/CSV) -> cria em lote
GET/POST/DELETE /obras/{id}/documentos   (upload -> R2)

## RDO
GET /obras/{id}/rdos                      (lista/filtra por data, status)
POST /obras/{id}/rdos                      (cria rascunho; numero sugerido)
GET /rdos/{id}
PUT /rdos/{id}                             (edita; 409 se aprovado)
POST /rdos/{id}/finalizar                  -> gera PDF, snapshot, token
POST /rdos/{id}/revisar                     (abre nova revisao a partir de revisao_solicitada;
                                             revisao+1; arquiva anterior em rdo_revisoes)
GET /rdos/{id}/revisoes                     (historico de revisoes)
POST /rdos/{id}/midia                       (multipart foto|video -> R2, thumb)
POST /rdos/sincronizar                     (batch offline: cria/atualiza; idempotente)
GET /rdos/{id}/pdf                          -> url assinada R2

## Aprovacao de RDO (publico com token)
GET /aprovacao/{token}                      -> resumo + pdf (sem auth)
POST /aprovacao/{token}/aprovar             {nome, funcao} -> aprova (bloqueia edicao)
POST /aprovacao/{token}/solicitar-revisao   {nome, funcao, motivo} -> status revisao_solicitada

## Faturamento (plano por contrato) / Condicoes de pagamento
GET/POST/PUT/DELETE /condicoes-pagamento     (ex.: "21/35/42/60", "100% a vista")
GET/POST/PUT /obras/{id}/faturamento         (plano + eventos: entrada, mobilizacao,
                                              canteiro_mensal, medicao_periodica, comissionamento,
                                              entrega_tecnica/final, %/valor, gatilho, recorrencia)

## Medicao
POST /obras/{id}/medicoes/calcular          {de, ate} -> boletim calculado (nao salva)
POST /obras/{id}/medicoes                   (salva) -> {numero, pdfUrl, token}
GET /obras/{id}/medicoes
GET /medicao-aprovacao/{token}               -> resumo + pdf (publico, sem auth)
POST /medicao-aprovacao/{token}/aprovar      {nome, funcao} -> aprova medicao
DELETE /medicoes/{id}                        (role Admin)

## Dashboard / Indicadores
GET /obras/{id}/avanco                       (%hh, %qtd, itens, curva, curvaS)
GET /dashboard?obraId=&perfil=              (KPIs, pareto, rankings)
GET /farois                                  (lista de obras: prazo x avanco)

## Admin
DELETE /obras/{id}/rdos                       (modo=rdo|obra|tudo; role Admin)
GET /auditoria                                (logs)
