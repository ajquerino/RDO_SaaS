import { useEffect, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { api, apiUpload, type Cliente, type Obra, type ObraDetalhe, type ObraLista, type RdoLista } from "../lib/api";
import { useAuth, podeGerirObras, podeVerValores, type Usuario } from "../store/auth";
import { useOnline, notificarSyncMudou } from "../lib/useOnline";
import { novoLocalId, salvarRdoLocal, listarRdosLocaisDaObra, type RdoLocal } from "../lib/db";
import Rdo from "./Rdo";
import Dashboard from "./Dashboard";
import Medicao from "./Medicao";
import Documentos from "./Documentos";
import HhSemanal from "./HhSemanal";
import EapEditor from "./EapEditor";
import SeloPlano from "./SeloPlano";
import GuiaUso from "./GuiaUso";
import { useAssinatura } from "../lib/useAssinatura";

export default function Obras() {
  const qc = useQueryClient();
  const gereObras = podeGerirObras(useAuth((s) => s.usuario)?.funcao);
  const bloqueada = useAssinatura().bloqueada; // assinatura vencida => só leitura
  const [aberta, setAberta] = useState<string | null>(null);
  const [nome, setNome] = useState("");
  const [contrato, setContrato] = useState("");
  const [clienteId, setClienteId] = useState("");
  const [prazo, setPrazo] = useState("");
  const [erro, setErro] = useState<string | null>(null);
  const [aviso, setAviso] = useState<string | null>(null);

  const { data: obras } = useQuery({ queryKey: ["obras"], queryFn: () => api<ObraLista[]>("/api/v1/obras") });
  const { data: clientes } = useQuery({ queryKey: ["clientes"], queryFn: () => api<Cliente[]>("/api/v1/clientes") });

  const criar = useMutation({
    mutationFn: () =>
      api<{ aviso?: string | null }>("/api/v1/obras", {
        method: "POST",
        body: JSON.stringify({ nome, contrato, clienteId: clienteId || null, prazoPagamento: prazo, status: "Andamento" }),
      }),
    onSuccess: (r) => {
      setNome(""); setContrato(""); setClienteId(""); setPrazo(""); setErro(null); setAviso(r.aviso ?? null);
      qc.invalidateQueries({ queryKey: ["obras"] });
      qc.invalidateQueries({ queryKey: ["uso-plano"] });
    },
    onError: (e) => setErro((e as Error).message),
  });

  return (
    <div className="space-y-6">
      {gereObras && <div className="flex justify-end"><SeloPlano /></div>}

      {aviso && (
        <div className="flex items-start justify-between gap-3 rounded-lg border border-amber-700 bg-amber-900/40 px-3 py-2 text-sm text-amber-200">
          <span>{aviso}</span>
          <button onClick={() => setAviso(null)} className="shrink-0 text-amber-300 hover:text-amber-100">✕</button>
        </div>
      )}

      {gereObras && !bloqueada && (
      <form onSubmit={(e) => { e.preventDefault(); criar.mutate(); }} className="rounded-xl bg-slate-800 p-4 space-y-3">
        <h2 className="font-semibold">Nova obra</h2>
        <div className="grid gap-3 sm:grid-cols-2">
          <input className="rounded-lg bg-slate-900 px-3 py-2" placeholder="Nome da obra *" value={nome} onChange={(e) => setNome(e.target.value)} required />
          <input className="rounded-lg bg-slate-900 px-3 py-2" placeholder="Contrato" value={contrato} onChange={(e) => setContrato(e.target.value)} />
          <select className="rounded-lg bg-slate-900 px-3 py-2" value={clienteId} onChange={(e) => setClienteId(e.target.value)}>
            <option value="">Cliente (opcional)</option>
            {clientes?.map((c) => <option key={c.id} value={c.id}>{c.nome}</option>)}
          </select>
          <input className="rounded-lg bg-slate-900 px-3 py-2" placeholder="Prazo pagto (ex.: 21/42)" value={prazo} onChange={(e) => setPrazo(e.target.value)} />
        </div>
        {erro && <p className="text-sm text-red-400">{erro}</p>}
        <button disabled={criar.isPending} className="rounded-lg bg-sky-600 px-4 py-2 font-semibold hover:bg-sky-500 disabled:opacity-50">
          {criar.isPending ? "Salvando..." : "Criar obra"}
        </button>
      </form>
      )}

      <ul className="space-y-2">
        {obras?.map((o) => (
          <li key={o.id} className="rounded-lg bg-slate-800">
            <button className="w-full px-4 py-3 flex items-center justify-between text-left" onClick={() => setAberta(aberta === o.id ? null : o.id)}>
              <span>
                <span className="font-medium">{o.nome}</span>
                <span className="block text-sm text-slate-400">{o.contrato ?? "sem contrato"} · {o.itens} itens</span>
              </span>
              <span className="text-xs rounded-full bg-slate-700 px-2 py-1">{o.status}</span>
            </button>
            {aberta === o.id && <ObraDetalhe obraId={o.id} />}
          </li>
        ))}
        {obras?.length === 0 && <p className="text-slate-400 text-sm">Nenhuma obra ainda.</p>}
      </ul>
    </div>
  );
}

function ObraDetalhe({ obraId }: { obraId: string }) {
  const qc = useQueryClient();
  const funcao = useAuth((s) => s.usuario)?.funcao;
  const gereObras = podeGerirObras(funcao);
  const verValores = podeVerValores(funcao);
  const bloqueada = useAssinatura().bloqueada; // assinatura vencida => só leitura
  const online = useOnline();
  const [msg, setMsg] = useState<string | null>(null);
  const [rdoEditando, setRdoEditando] = useState<string | null>(null);
  const [rascunhos, setRascunhos] = useState<RdoLocal[]>([]);
  const [editandoObra, setEditandoObra] = useState(false);
  const [sub, setSub] = useState<"detalhe" | "dashboard" | "hh" | "medicao" | "documentos">("detalhe");
  const [guiaAberto, setGuiaAberto] = useState(false);

  const { data } = useQuery({ queryKey: ["obra", obraId], queryFn: () => api<ObraDetalhe>(`/api/v1/obras/${obraId}`) });
  const { data: usuarios } = useQuery({ queryKey: ["usuarios"], queryFn: () => api<Usuario[]>("/api/v1/usuarios"), enabled: gereObras });
  const { data: rdos } = useQuery({ queryKey: ["rdos", obraId], queryFn: () => api<RdoLista[]>(`/api/v1/obras/${obraId}/rdos`) });

  const hoje = new Date().toISOString().slice(0, 10);
  const novoRdo = useMutation({
    mutationFn: () => api<{ id: string }>(`/api/v1/obras/${obraId}/rdos`, { method: "POST", body: JSON.stringify({ data: hoje }) }),
    onSuccess: (r) => { qc.invalidateQueries({ queryKey: ["rdos", obraId] }); setRdoEditando(r.id); },
  });

  // Rascunhos locais (offline) desta obra ainda não criados no servidor.
  const recarregarRascunhos = () => listarRdosLocaisDaObra(obraId).then((rs) => setRascunhos(rs.filter((r) => !r.serverId)));
  useEffect(() => {
    recarregarRascunhos();
    const h = () => recarregarRascunhos();
    window.addEventListener("rdo-sync-mudou", h);
    return () => window.removeEventListener("rdo-sync-mudou", h);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [obraId, rdoEditando]);

  // "Novo RDO": online cria no servidor; offline cria um rascunho local (sincroniza depois).
  async function criarRdo() {
    if (online) { novoRdo.mutate(); return; }
    const localId = novoLocalId();
    await salvarRdoLocal({ localId, serverId: null, obraId, data: { data: hoje }, atualizadoEm: Date.now(), sincronizado: false, finalizar: false, erroSync: null, fotosPendentes: [] });
    notificarSyncMudou();
    setRdoEditando(localId);
  }

  const excluirRdo = useMutation({
    mutationFn: (rdoId: string) => api(`/api/v1/rdos/${rdoId}`, { method: "DELETE" }),
    onSuccess: () => qc.invalidateQueries({ queryKey: ["rdos", obraId] }),
    onError: (e) => setMsg((e as Error).message),
  });

  const editarObra = useMutation({
    mutationFn: (r: Record<string, unknown>) => api(`/api/v1/obras/${obraId}`, { method: "PUT", body: JSON.stringify(r) }),
    onSuccess: () => { setEditandoObra(false); qc.invalidateQueries({ queryKey: ["obra", obraId] }); qc.invalidateQueries({ queryKey: ["obras"] }); },
    onError: (e) => setMsg((e as Error).message),
  });

  const excluirObra = useMutation({
    mutationFn: () => api(`/api/v1/obras/${obraId}`, { method: "DELETE" }),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ["obras"] }); },
    onError: (e) => setMsg((e as Error).message),
  });

  const vincular = useMutation({
    mutationFn: (usuarioId: string) => api(`/api/v1/obras/${obraId}/usuarios`, { method: "POST", body: JSON.stringify({ usuarioIds: [usuarioId] }) }),
    onSuccess: () => setMsg("Usuário vinculado."),
    onError: (e) => setMsg((e as Error).message),
  });

  const itens = data?.itens ?? [];

  // editor do RDO (apos todos os hooks, para nao violar as Regras de Hooks)
  if (rdoEditando)
    return (
      <div className="border-t border-slate-700 px-4 py-4">
        <Rdo obraId={obraId} rdoId={rdoEditando} onClose={() => setRdoEditando(null)} />
      </div>
    );

  return (
    <div className="border-t border-slate-700 px-4 py-4 space-y-4">
      <div className="flex flex-wrap gap-1">
        {(["detalhe", "dashboard", "hh", "documentos", ...(gereObras ? ["medicao"] as const : [])] as const).map((s) => (
          <button
            key={s}
            onClick={() => setSub(s)}
            className={`rounded-lg px-3 py-1.5 text-sm ${sub === s ? "bg-sky-600 text-white" : "bg-slate-700 text-slate-300 hover:bg-slate-600"}`}
          >
            {s === "detalhe" ? "EAP & RDOs" : s === "dashboard" ? "Dashboard" : s === "hh" ? "HH da semana" : s === "documentos" ? "Documentos" : "Medição"}
          </button>
        ))}
      </div>

      {sub === "dashboard" && <Dashboard obraId={obraId} />}
      {sub === "hh" && <HhSemanal obraId={obraId} />}
      {sub === "documentos" && <Documentos obraId={obraId} />}
      {sub === "medicao" && gereObras && <Medicao obraId={obraId} />}

      {sub === "detalhe" && <>
      {gereObras && data?.obra && (
        <div className="rounded-lg bg-slate-900/40 p-2">
          {editandoObra ? (
            <EditarObraForm obra={data.obra} salvando={editarObra.isPending} onSalvar={(r) => editarObra.mutate(r)} onCancelar={() => setEditandoObra(false)} />
          ) : (
            <div className="flex items-center justify-between gap-2">
              <span className="text-sm text-slate-400">Obra: <span className="text-slate-200">{data.obra.nome}</span></span>
              <div className="flex gap-1">
                <button onClick={() => setEditandoObra(true)} disabled={bloqueada} className="rounded-lg bg-slate-700 px-2 py-1 text-xs hover:bg-slate-600 disabled:opacity-50">Editar obra</button>
                <button
                  disabled={excluirObra.isPending || bloqueada}
                  onClick={() => { if (confirm(`Excluir a obra "${data.obra.nome}"?\n\nEla some das listas (exclusão reversível pelo banco). RDOs e itens ficam guardados.`)) excluirObra.mutate(); }}
                  className="rounded-lg bg-red-950 px-2 py-1 text-xs text-red-300 hover:bg-red-900 disabled:opacity-50"
                >Excluir obra</button>
              </div>
            </div>
          )}
        </div>
      )}
      {gereObras && (
        <div className="space-y-2">
          <ImportarEap
            obraId={obraId}
            onImportado={(n) => { setMsg(`${n} itens importados.`); qc.invalidateQueries({ queryKey: ["obra", obraId] }); qc.invalidateQueries({ queryKey: ["obras"] }); }}
          />
          {msg && <span className="text-sm text-emerald-400">{msg}</span>}
        </div>
      )}

      <EapEditor obraId={obraId} itens={itens} gereObras={gereObras} verValores={verValores} />

      {usuarios && usuarios.length > 0 && (
        <div className="text-sm">
          <span className="text-slate-400">Vincular usuário à obra: </span>
          <select
            defaultValue=""
            onChange={(e) => { if (e.target.value) vincular.mutate(e.target.value); e.target.value = ""; }}
            className="rounded-lg bg-slate-900 px-2 py-1"
          >
            <option value="">selecione…</option>
            {usuarios.map((u) => <option key={u.id} value={u.id}>{u.nome} ({u.funcao})</option>)}
          </select>
        </div>
      )}

      <button
        onClick={() => setGuiaAberto(true)}
        className="mb-3 flex w-full items-center gap-3 rounded-xl bg-slate-800/60 px-4 py-3 text-left ring-1 ring-sky-500/40 hover:bg-slate-800"
      >
        <span className="text-xl" aria-hidden>📖</span>
        <span>
          <span className="block text-sm font-semibold text-sky-300">Leia aqui — tudo que dá pra fazer no IndustrialOS</span>
          <span className="block text-xs text-slate-400">Guia rápido: RDO, EAP, dashboards, medição, offline e o que vem por aí.</span>
        </span>
      </button>
      {guiaAberto && <GuiaUso onFechar={() => setGuiaAberto(false)} />}

      <div className="border-t border-slate-700 pt-4">
        <div className="flex items-center justify-between mb-2">
          <span className="text-sm text-slate-400">RDOs — {(rdos?.length ?? 0) + rascunhos.length}</span>
          <button onClick={criarRdo} disabled={bloqueada || (online && novoRdo.isPending)} title={bloqueada ? "Assinatura vencida — acesso somente leitura" : undefined} className="rounded-lg bg-sky-600 px-3 py-1.5 text-sm font-semibold hover:bg-sky-500 disabled:opacity-50">
            {online && novoRdo.isPending ? "Criando…" : online ? "+ Novo RDO" : "+ Novo RDO (offline)"}
          </button>
        </div>
        <ul className="space-y-1">
          {rascunhos.map((r) => (
            <li key={r.localId}>
              <button onClick={() => setRdoEditando(r.localId)} className="w-full flex items-center justify-between rounded-lg border border-amber-700/60 bg-amber-950/30 px-3 py-2 text-sm text-left hover:bg-amber-950/50">
                <span>Rascunho · {(r.data as { data?: string })?.data ?? "—"}{r.finalizar ? " (aguardando envio)" : ""}</span>
                <span className="text-xs text-amber-300">📴 não sincronizado</span>
              </button>
            </li>
          ))}
          {rdos?.map((r) => (
            <li key={r.id} className="flex items-stretch gap-1">
              <button onClick={() => setRdoEditando(r.id)} className="flex-1 flex justify-between rounded-lg bg-slate-900 px-3 py-2 text-sm text-left hover:bg-slate-700">
                <span>RDO {r.numero}{r.revisao > 0 ? ` rev.${r.revisao}` : ""} · {r.data}</span>
                <span className="text-slate-400">{r.status}</span>
              </button>
              {gereObras && r.status !== "Aprovado" && (
                <button
                  disabled={excluirRdo.isPending || bloqueada}
                  title={bloqueada ? "Assinatura vencida — somente leitura" : "Excluir RDO"}
                  onClick={() => { if (confirm(`Excluir o RDO ${r.numero}${r.revisao > 0 ? ` rev.${r.revisao}` : ""}? (exclusão reversível)`)) excluirRdo.mutate(r.id); }}
                  className="rounded-lg bg-red-950 px-2 text-xs text-red-300 hover:bg-red-900 disabled:opacity-50"
                >Excluir</button>
              )}
            </li>
          ))}
          {(rdos?.length ?? 0) === 0 && rascunhos.length === 0 && <li className="text-slate-500 text-sm">Nenhum RDO ainda.</li>}
        </ul>
      </div>
      </>}
    </div>
  );
}

// Import de EAP em 2 passos: (1) sobe qualquer planilha → (2) aponta as colunas (com sugestão) → aplica.
type CampoEap = "descricao" | "unidade" | "qtd" | "hh" | "valor" | "disciplina" | "inicio" | "fim";
type AnaliseEap = {
  colunas: string[];
  amostra: string[][];
  totalLinhas: number;
  linhas: string[][];
  sugestao: Record<CampoEap, number | null>;
};
const CAMPOS_EAP: { k: CampoEap; label: string; obrig?: boolean }[] = [
  { k: "descricao", label: "Descrição", obrig: true },
  { k: "unidade", label: "Unidade" },
  { k: "qtd", label: "Qtd" },
  { k: "hh", label: "HH" },
  { k: "valor", label: "Valor" },
  { k: "disciplina", label: "Disciplina" },
  { k: "inicio", label: "Início" },
  { k: "fim", label: "Fim" },
];
const MAPA_VAZIO: Record<CampoEap, number | null> = {
  descricao: null, unidade: null, qtd: null, hh: null, valor: null, disciplina: null, inicio: null, fim: null,
};

function ImportarEap({ obraId, onImportado }: { obraId: string; onImportado: (n: number) => void }) {
  const [analise, setAnalise] = useState<AnaliseEap | null>(null);
  const [mapa, setMapa] = useState<Record<CampoEap, number | null>>(MAPA_VAZIO);
  const [erro, setErro] = useState<string | null>(null);

  const analisar = useMutation({
    mutationFn: (file: File) => { const f = new FormData(); f.append("file", file); return apiUpload<AnaliseEap>(`/api/v1/obras/${obraId}/itens/importar/analisar`, f); },
    onSuccess: (a) => { setAnalise(a); setMapa({ ...MAPA_VAZIO, ...a.sugestao }); setErro(null); },
    onError: (e) => setErro((e as Error).message),
  });
  const aplicar = useMutation({
    mutationFn: () => api<{ importados: number }>(`/api/v1/obras/${obraId}/itens/importar/aplicar`, { method: "POST", body: JSON.stringify({ linhas: analise!.linhas, mapeamento: mapa }) }),
    onSuccess: (r) => { setAnalise(null); setMapa(MAPA_VAZIO); onImportado(r.importados); },
    onError: (e) => setErro((e as Error).message),
  });

  function escolher(e: React.ChangeEvent<HTMLInputElement>) {
    setErro(null);
    const f = e.target.files?.[0];
    if (f) analisar.mutate(f);
    e.target.value = ""; // permite reescolher o mesmo arquivo depois
  }

  // Passo 1 — subir arquivo
  if (!analise) {
    return (
      <div className="space-y-1">
        <label className="block text-sm text-slate-400">Importar EAP de planilha (.csv, .xlsx, .xlsm)</label>
        <input type="file" accept=".csv,.xlsx,.xlsm" onChange={escolher} disabled={analisar.isPending}
          className="text-sm text-slate-300 file:mr-3 file:rounded-lg file:border-0 file:bg-sky-600 file:px-3 file:py-1.5 file:text-white disabled:opacity-50" />
        {analisar.isPending && <p className="text-xs text-slate-400">Lendo o arquivo…</p>}
        {erro && <p className="text-sm text-red-400">{erro}</p>}
      </div>
    );
  }

  // Passo 2 — apontar colunas + conferir amostra
  const valSel = (v: number | null) => (v === null ? "" : String(v));
  return (
    <div className="space-y-3 rounded-xl bg-slate-900/40 p-3">
      <div className="flex items-center justify-between">
        <p className="text-sm font-medium text-slate-200">Aponte as colunas <span className="text-slate-500">({analise.totalLinhas} linhas)</span></p>
        <button onClick={() => { setAnalise(null); setErro(null); }} className="text-xs text-slate-400 hover:text-slate-200">Cancelar</button>
      </div>

      <div className="grid gap-2 sm:grid-cols-2">
        {CAMPOS_EAP.map(({ k, label, obrig }) => (
          <label key={k} className="flex items-center justify-between gap-2 text-sm">
            <span className="whitespace-nowrap text-slate-400">{label}{obrig && <span className="text-sky-400"> *</span>}</span>
            <select value={valSel(mapa[k])} onChange={(e) => setMapa({ ...mapa, [k]: e.target.value === "" ? null : Number(e.target.value) })}
              className="min-w-0 flex-1 rounded-lg bg-slate-900 px-2 py-1.5">
              <option value="">— (nenhuma)</option>
              {analise.colunas.map((c, i) => <option key={i} value={i}>{c || `Coluna ${i + 1}`}</option>)}
            </select>
          </label>
        ))}
      </div>

      <div className="overflow-x-auto rounded-lg border border-slate-800">
        <table className="w-full text-xs">
          <thead className="text-left text-slate-500">
            <tr>{analise.colunas.map((c, i) => <th key={i} className="whitespace-nowrap px-2 py-1">{c || `Coluna ${i + 1}`}</th>)}</tr>
          </thead>
          <tbody>
            {analise.amostra.map((row, ri) => (
              <tr key={ri} className="border-t border-slate-800">
                {analise.colunas.map((_, ci) => <td key={ci} className="whitespace-nowrap px-2 py-1 text-slate-300">{row[ci] ?? ""}</td>)}
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {erro && <p className="text-sm text-red-400">{erro}</p>}
      <div className="flex flex-wrap items-center gap-2">
        <button onClick={() => aplicar.mutate()} disabled={mapa.descricao === null || aplicar.isPending}
          className="rounded-lg bg-sky-600 px-4 py-2 text-sm font-semibold hover:bg-sky-500 disabled:opacity-50">
          {aplicar.isPending ? "Importando…" : `Importar ${analise.totalLinhas} itens`}
        </button>
        {mapa.descricao === null && <span className="text-xs text-amber-400">Escolha a coluna de Descrição.</span>}
      </div>
    </div>
  );
}

// Edição dos campos principais da obra. Preserva os campos não editados aqui
// (frenteServico, responsavelPadrao, GPS, cliente) reenviando os valores atuais.
const STATUS_OBRA = ["Planejada", "Andamento", "Paralisada", "Concluida"] as const;
function EditarObraForm({ obra, salvando, onSalvar, onCancelar }:
  { obra: Obra; salvando: boolean; onSalvar: (r: Record<string, unknown>) => void; onCancelar: () => void }) {
  const [nome, setNome] = useState(obra.nome);
  const [contrato, setContrato] = useState(obra.contrato ?? "");
  const [ordemServico, setOrdemServico] = useState(obra.ordemServico ?? "");
  const [local, setLocal] = useState(obra.local ?? "");
  const [prazo, setPrazo] = useState(obra.prazoPagamento ?? "");
  const [status, setStatus] = useState(obra.status);
  const [dataInicio, setDataInicio] = useState(obra.dataInicio ?? "");
  const [dataFim, setDataFim] = useState(obra.dataFim ?? "");

  const salvar = () => onSalvar({
    clienteId: obra.clienteId ?? null, empresaId: obra.empresaId ?? null,
    nome: nome.trim(), contrato: contrato || null, ordemServico: ordemServico || null,
    local: local || null, frenteServico: obra.frenteServico ?? null,
    responsavelPadrao: obra.responsavelPadrao ?? null,
    dataInicio: dataInicio || null, dataFim: dataFim || null,
    prazoPagamento: prazo || null, status,
    latitude: obra.latitude ?? null, longitude: obra.longitude ?? null,
  });

  const inp = "rounded-lg bg-slate-900 px-2 py-1 text-sm";
  return (
    <div className="space-y-2">
      <div className="grid gap-2 sm:grid-cols-2">
        <input className={inp} value={nome} onChange={(e) => setNome(e.target.value)} placeholder="Nome da obra *" />
        <input className={inp} value={contrato} onChange={(e) => setContrato(e.target.value)} placeholder="Contrato" />
        <input className={inp} value={ordemServico} onChange={(e) => setOrdemServico(e.target.value)} placeholder="Ordem de serviço" />
        <input className={inp} value={local} onChange={(e) => setLocal(e.target.value)} placeholder="Local" />
        <input className={inp} value={prazo} onChange={(e) => setPrazo(e.target.value)} placeholder="Prazo pagamento (ex.: 21/42)" />
        <select className={inp} value={status} onChange={(e) => setStatus(e.target.value)}>
          {STATUS_OBRA.map((s) => <option key={s} value={s}>{s}</option>)}
        </select>
        <label className="text-xs text-slate-400">Início<input type="date" className={`${inp} w-full`} value={dataInicio} onChange={(e) => setDataInicio(e.target.value)} /></label>
        <label className="text-xs text-slate-400">Fim<input type="date" className={`${inp} w-full`} value={dataFim} onChange={(e) => setDataFim(e.target.value)} /></label>
      </div>
      <div className="flex gap-1">
        <button disabled={salvando || !nome.trim()} onClick={salvar} className="rounded-lg bg-emerald-600 px-3 py-1 text-xs font-semibold hover:bg-emerald-500 disabled:opacity-50">{salvando ? "Salvando…" : "Salvar obra"}</button>
        <button disabled={salvando} onClick={onCancelar} className="rounded-lg bg-slate-700 px-3 py-1 text-xs hover:bg-slate-600">Cancelar</button>
      </div>
    </div>
  );
}
