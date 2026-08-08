const API = import.meta.env.VITE_API_URL ?? "http://localhost:8080";

export function getToken() {
  return localStorage.getItem("accessToken");
}

/** fetch com Bearer + tratamento basico de erro (JSON).
 *  Tem timeout (padrao 20s): sem isso, se a API nao responder o spinner fica eterno. */
export async function api<T>(path: string, init: RequestInit = {}, timeoutMs = 20000): Promise<T> {
  const token = getToken();
  const ctrl = new AbortController();
  const timer = setTimeout(() => ctrl.abort(), timeoutMs);
  try {
    const res = await fetch(`${API}${path}`, {
      ...init,
      signal: init.signal ?? ctrl.signal,
      headers: {
        "Content-Type": "application/json",
        ...(token ? { Authorization: `Bearer ${token}` } : {}),
        ...(init.headers ?? {}),
      },
    });
    if (res.status === 204) return undefined as T;
    if (!res.ok) {
      const body = await res.json().catch(() => ({}));
      // Sessão derrubada por login em outro dispositivo: sai na hora (diferente do 401 de expiração).
      if (res.status === 401 && (res.headers.get("X-Sessao") === "encerrada" || body.code === "sessao_encerrada")) {
        localStorage.clear();
        localStorage.setItem("authMsg", "Sua conta foi acessada em outro dispositivo.");
        if (location.pathname !== "/") location.assign("/"); else location.reload();
        throw new Error("Sua conta foi acessada em outro dispositivo.");
      }
      throw new Error(body.erro ?? body.title ?? `Erro ${res.status}`);
    }
    return res.json() as Promise<T>;
  } catch (e) {
    if (e instanceof DOMException && e.name === "AbortError")
      throw new Error("Tempo de resposta esgotado — verifique se a API está no ar.");
    throw e;
  } finally {
    clearTimeout(timer);
  }
}
// Nota: uma resposta 402 (assinatura vencida) cai no `if (!res.ok)` acima e lança um Error
// com a mensagem amigável do corpo ({ erro }). O modo somente-leitura no front é só melhoria de UX.

// ---- Assinatura / inadimplência ----
export type AssinaturaStatus = {
  estado: "SemAssinatura" | "Trial" | "EmDia" | "PrestesAVencer" | "Vencido" | "Bloqueada";
  bloqueada: boolean;
  diasParaVencer?: number | null;
  diasAtraso?: number | null;
  avisoNivel?: number | null;
  vencimentoEm?: string | null;
  planoNome?: string | null;
};

/** upload multipart (sem Content-Type manual — o browser define o boundary). */
export async function apiUpload<T>(path: string, form: FormData): Promise<T> {
  const token = getToken();
  const res = await fetch(`${API}${path}`, {
    method: "POST",
    headers: token ? { Authorization: `Bearer ${token}` } : {},
    body: form,
  });
  if (!res.ok) {
    const body = await res.json().catch(() => ({}));
    throw new Error(body.erro ?? `Erro ${res.status}`);
  }
  return res.json() as Promise<T>;
}

/** baixa um recurso binario (ex.: PDF) com Bearer e devolve o Blob. */
export async function apiBlob(path: string): Promise<Blob> {
  const token = getToken();
  const res = await fetch(`${API}${path}`, { headers: token ? { Authorization: `Bearer ${token}` } : {} });
  if (!res.ok) throw new Error(`Erro ${res.status}`);
  return res.blob();
}

/** fetch para as rotas publicas (aprovacao por token) — nunca envia Bearer. */
export async function apiPublico<T>(path: string, init: RequestInit = {}): Promise<T> {
  const res = await fetch(`${API}${path}`, {
    ...init,
    headers: { "Content-Type": "application/json", ...(init.headers ?? {}) },
  });
  if (res.status === 204) return undefined as T;
  if (!res.ok) {
    const body = await res.json().catch(() => ({}));
    throw new Error(body.erro ?? `Erro ${res.status}`);
  }
  return res.json() as Promise<T>;
}

/** URL absoluta para abrir um recurso publico em nova aba (ex.: PDF da aprovacao). */
export function urlPublica(path: string) {
  return `${API}${path}`;
}

// ---- tipos de dominio ----
export type Cliente = { id: string; nome: string; cnpj?: string; contato?: string; endereco?: string };
export type ObraLista = { id: string; nome: string; contrato?: string; status: string; dataInicio?: string; dataFim?: string; itens: number };
export type ObraItem = {
  id: string; descricao: string; unidade?: string; qtdPrevista?: number; hhPrevisto?: number;
  valor?: number; disciplina?: string; dataInicio?: string; dataFim?: string; ordem: number;
};
export type Obra = {
  id: string; nome: string; contrato?: string; ordemServico?: string; local?: string;
  clienteId?: string; prazoPagamento?: string; status: string; dataInicio?: string; dataFim?: string;
};
export type ObraDetalhe = { obra: Obra; itens: ObraItem[] };

export type RdoLista = { id: string; numero: number; revisao: number; data: string; turno?: string; status: string };
export type Midia = { id: string; tipo: string; categoria?: string; descricao?: string; tamanhoBytes: number; url: string };
export type Efetivo = { funcao?: string; quantidade: number; entrada?: string; saida?: string; horaExtra?: string; obs?: string };
export type Paralisacao = { inicio?: string; fim?: string; motivo?: string; descricao?: string };
export type Recurso = { equipamento?: string; quantidade: number; horas?: string; obs?: string };
export type Servico = { obraItemId?: string | null; atividade?: string; local?: string; qtdExec?: number | null; unidade?: string; status?: string; pctInformado?: number | null; motivoHold?: string; obs?: string; pctItem?: number };
export type Retrabalho = { atividade?: string; local?: string; quantidade?: number | null; unidade?: string; pessoas: number; horas?: number | null; causa?: string; origem?: string; descricao?: string; acaoCorretiva?: string };
export type Pendencia = { descricao?: string; responsavel?: string; prazo?: string; status?: string };
export type RdoDetalhe = {
  id: string; obraId: string; numero: number; revisao: number; data: string; diaSemana?: string; turno?: string;
  status: string; ocorrencias?: string; clima: any; jornada: any; dificuldades: any; proximoDia: any; planejamento: any; seguranca: any; assinaturas: any;
  efetivo: Efetivo[]; paralisacoes: Paralisacao[]; recursos: Recurso[]; servicos: Servico[]; retrabalho: Retrabalho[];
  tokenAprovacao?: string | null; motivoRevisao?: string | null; revisadoPor?: string | null; revisadoEm?: string | null;
  aprovadoPor?: string | null; aprovadoEm?: string | null;
};

// ---- Dashboard (Sprint 6) ----
export type DashItem = { descricao: string; hhPrevisto?: number | null; qtdPrevista?: number | null; pct: number };
export type CurvaPonto = { data: string; previstoPct: number; realizadoPct: number };
export type Dashboard = {
  obra: { nome: string; contrato?: string; dataInicio?: string; dataFim?: string; status: string };
  avanco: { pct: number; baseAvanco: string; itens: DashItem[] };
  hh: { previsto: number; realizado: number; efetivoMedio: number };
  farol: { cor: string; desvio?: number | null };
  paralisacoes: { motivo: string; ocorrencias: number; minutos: number }[];
  retrabalho: { causa: string; hh: number }[];
  curvaS: CurvaPonto[];
  produtividade: Produtividade;
  rdos: number;
};

// ---- Produtividade (Sprint 8) ----
export type Produtividade = {
  hhDireto: number; hhIndireto: number; hhNaoClassificado: number;
  pctDireto: number; pctIndireto: number;
  porFuncao: { funcao: string; hh: number; pessoas: number }[];
};

// ---- Aprovacao publica por token (Sprint 5) ----
export type AprovacaoView = {
  numero: number; revisao: number; data: string; diaSemana?: string; turno?: string;
  status: string; ocorrencias?: string; responsavel?: string;
  obra: { nome?: string; contrato?: string; local?: string; cliente?: string };
  efetivo: { funcao?: string; quantidade: number; horaExtra?: string }[];
  paralisacoes: { inicio?: string; fim?: string; motivo?: string; descricao?: string }[];
  servicos: { atividade?: string; item?: string; status?: string; qtdExec?: number | null; unidade?: string; pct: number }[];
  retrabalho: { atividade?: string; pessoas: number; horas?: number | null; causa?: string }[];
  motivoRevisao?: string | null; revisadoPor?: string | null; aprovadoPor?: string | null; aprovadoEm?: string | null;
};

// ---- Medicao / Faturamento (Sprint 7) ----
export type CondicaoPagamento = { id: string; nome: string; parcelas: string }; // parcelas = jsonb string [{dias,pct?}]
export type FaturamentoEvento = {
  id?: string; tipo: string; base: string; percentual?: number | null; valor?: number | null;
  gatilho?: string | null; dataPrevista?: string | null; recorrencia?: string | null;
  condicaoPagamentoId?: string | null; ordem: number; descricao?: string | null;
};
export type FaturamentoPlano = { id: string; nome: string; valorContrato: number; condicaoPagamentoId?: string | null };
export type Faturamento = { plano: FaturamentoPlano | null; eventos: FaturamentoEvento[] };

export type MedicaoItemCalc = { obraItemId: string; descricao: string; pctIni: number; pctFim: number; valor: number; medidoPeriodo: number; medidoAcum: number };
export type MedicaoParcelaCalc = { dias: number; vencimento: string; valor: number; pct: number };
export type MedicaoCalc = {
  de: string; ate: string; valorContrato: number; medidoAcumulado: number; valorPeriodo: number;
  pctFisico: number; pctFinanceiro: number; avancoHh: number;
  itens: MedicaoItemCalc[]; parcelas: MedicaoParcelaCalc[];
};
export type MedicaoLista = { id: string; numero: number; de: string; ate: string; valorPeriodo: number; medidoAcumulado: number; pctFisico: number; pctFinanceiro: number; status: string };

// ---- Equipamentos / Documentos (Sprint 8) ----
export type Equipamento = {
  id: string; nome: string; tipo?: string | null; proprioLocado: string;
  horimetro?: number | null; custoHora?: number | null; statusManutencao?: string | null;
};
export type Documento = { id: string; tipo: string; nome: string; versao?: string | null; tamanhoBytes: number; criadoEm: string; url: string };

// ---- Hardening (Sprint 9) ----
export type AuditoriaItem = { id: string; acao: string; entidade: string; entidadeId?: string | null; usuarioId?: string | null; detalhe: string; criadoEm: string };
export type AuditoriaPagina = { total: number; page: number; size: number; itens: AuditoriaItem[] };
export type Plano = { id: string; nome: string; limiteObras?: number | null; limiteUsuarios?: number | null; precoMensal?: number | null };

// ---- Console de Plataforma (SuperAdmin) ----
export type TenantResumo = { id: string; nome: string; cnpj?: string | null; plano: string; planoId?: string | null; planoNome?: string | null; status: string; criadoEm: string; nObras: number; nUsuarios: number; limiteObras?: number | null; limiteUsuarios?: number | null };
export type MetricasPlataforma = { totalTenants: number; tenantsAtivos: number; totalObras: number; totalRdos: number; totalUsuarios: number };

// Uso do plano do tenant corrente (aviso, nunca bloqueia)
export type UsoPlano = { nObras: number; limiteObras?: number | null; nUsuarios: number; limiteUsuarios?: number | null; plano?: string | null };
