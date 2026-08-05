# Matriz de Permissoes — IndustrialOS

Perfis: ENC (Encarregado), LID (Lider), SUP (Supervisor), PLAN (Planejador),
GES (Gestor), ADM (Administrador), FISC (Fiscal/Cliente - externo).
X = permitido | so obras vinculadas salvo GES/ADM (veem todas).

| Acao                                   | ENC | LID | SUP | PLAN | GES | ADM |
|----------------------------------------|-----|-----|-----|------|-----|-----|
| Login / ver suas obras                 |  X  |  X  |  X  |  X   |  X  |  X  |
| Criar/preencher/enviar RDO             |  X  |  X  |  X  |  X   |  X  |  X  |
| Editar RDO (nao aprovado)              |  X  |  X  |  X  |  X   |  X  |  X  |
| Ver Historico / abrir PDF              |  X  |  X  |  X  |  X   |  X  |  X  |
| Ver Dashboard                          |  X  |  X  |  X  |  X   |  X  |  X  |
| Enviar link de aprovacao               |  X  |  X  |  X  |  X   |  X  |  X  |
| Gerar nova revisao de RDO (pos-pedido) |  X  |  X  |  X  |  X   |  X  |  X  |
| Cadastrar/editar Obras, EAP e Contrato |     |     |     |  X   |  X  |  X  |
| Cadastrar/editar Clientes              |     |     |     |  X   |  X  |  X  |
| Importar cronograma / vincular usuário |     |     |     |  X   |  X  |  X  |
| Configurar plano de faturamento        |     |     |     |  X   |  X  |  X  |
| Cadastrar/editar Usuarios              |     |     |     |      |  X  |  X  |
| Emitir/ver Boletim de Medicao          |     |     |     |  X   |  X  |  X  |
| Apagar Boletim de Medicao              |     |     |     |      |     |  X  |
| Apagar RDO / obra / zerar tudo         |     |     |     |      |     |  X  |
| Ver valores (R$) / financeiro          |     |     |     |  X   |  X  |  X  |

FISC (externo): abrir o RDO/medicao pelo link com token e **aprovar** OU **solicitar
revisao** (informando motivo). Nao tem login nem acesso a outras telas.

Notas:
- ADM tem senha SEPARADA do GES (acoes destrutivas e financeiras criticas).
- PLAN cria BM mas nao apaga; para apagar RDO, solicita ao ADM.
- Isolamento por obra: ENC/LID/SUP/PLAN so veem obras vinculadas; GES/ADM veem todas.
