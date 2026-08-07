import { useRef, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { api, apiUpload, type Cliente, type ObraDetalhe, type ObraLista, type RdoLista } from "../lib/api";
import { useAuth, podeGerirObras, podeVerValores, type Usuario } from "../store/auth";
import Rdo from "./Rdo";
import Dashboard from "./Dashboard";
import Medicao from "./Medicao";
import Documentos from "./Documentos";
import HhSemanal from "./HhSemanal";
import SeloPlano from "./SeloPlano";

const brl = (v?: number) => (v == null ? "—" : v.toLocaleString("pt-BR", { style: "currency", currency: "BRL" }));

export default function Obras() {
  const qc = useQueryClient();
  const gereObras = podeGerirObras(useAuth((s) => s.usuario)?.funcao);
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

      {gereObras && (
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
  const fileRef = useRef<HTMLInputElement>(null);
  const [msg, setMsg] = useState<string | null>(null);
  const [rdoEditando, setRdoEditando] = useState<string | null>(null);
  const [sub, setSub] = useState<"detalhe" | "dashboard" | "hh" | "medicao" | "documentos">("detalhe");

  const { data } = useQuery({ queryKey: ["obra", obraId], queryFn: () => api<ObraDetalhe>(`/api/v1/obras/${obraId}`) });
  const { data: usuarios } = useQuery({ queryKey: ["usuarios"], queryFn: () => api<Usuario[]>("/api/v1/usuarios"), enabled: gereObras });
  const { data: rdos } = useQuery({ queryKey: ["rdos", obraId], queryFn: () => api<RdoLista[]>(`/api/v1/obras/${obraId}/rdos`) });

  const hoje = new Date().toISOString().slice(0, 10);
  const novoRdo = useMutation({
    mutationFn: () => api<{ id: string }>(`/api/v1/obras/${obraId}/rdos`, { method: "POST", body: JSON.stringify({ data: hoje }) }),
    onSuccess: (r) => { qc.invalidateQueries({ queryKey: ["rdos", obraId] }); setRdoEditando(r.id); },
  });

  const importar = useMutation({
    mutationFn: (file: File) => { const f = new FormData(); f.append("file", file); return apiUpload<{ importados: number }>(`/api/v1/obras/${obraId}/itens/importar`, f); },
    onSuccess: (r) => { setMsg(`${r.importados} itens importados.`); qc.invalidateQueries({ queryKey: ["obra", obraId] }); qc.invalidateQueries({ queryKey: ["obras"] }); },
    onError: (e) => setMsg((e as Error).message),
  });

  const vincular = useMutation({
    mutationFn: (usuarioId: string) => api(`/api/v1/obras/${obraId}/usuarios`, { method: "POST", body: JSON.stringify({ usuarioIds: [usuarioId] }) }),
    onSuccess: () => setMsg("Usuário vinculado."),
    onError: (e) => setMsg((e as Error).message),
  });

  const itens = data?.itens ?? [];
  const totalHh = itens.reduce((s, i) => s + (i.hhPrevisto ?? 0), 0);
  const totalValor = itens.reduce((s, i) => s + (i.valor ?? 0), 0);

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
      {gereObras && (
        <div className="flex flex-wrap items-center gap-2">
          <input ref={fileRef} type="file" accept=".csv,.xlsx,.xlsm" className="text-sm text-slate-300 file:mr-3 file:rounded-lg file:border-0 file:bg-sky-600 file:px-3 file:py-1.5 file:text-white" />
          <button
            onClick={() => { const f = fileRef.current?.files?.[0]; if (f) importar.mutate(f); }}
            disabled={importar.isPending}
            className="rounded-lg bg-slate-700 px-3 py-1.5 text-sm hover:bg-slate-600 disabled:opacity-50"
          >
            {importar.isPending ? "Importando..." : "Importar cronograma"}
          </button>
          {msg && <span className="text-sm text-emerald-400">{msg}</span>}
        </div>
      )}

      <div>
        <div className="flex justify-between text-sm text-slate-400 mb-2">
          <span>EAP — {itens.length} itens</span>
          <span>HH previsto: {totalHh.toLocaleString("pt-BR")}{verValores ? ` · ${brl(totalValor)}` : ""}</span>
        </div>
        <div className="overflow-x-auto">
          <table className="w-full text-sm">
            <thead className="text-slate-400 text-left">
              <tr><th className="py-1 pr-3">Descrição</th><th className="pr-3">Un</th><th className="pr-3">Qtd</th><th className="pr-3">HH</th>{verValores && <th className="pr-3">Valor</th>}<th>Disciplina</th></tr>
            </thead>
            <tbody>
              {itens.map((i) => (
                <tr key={i.id} className="border-t border-slate-700/50">
                  <td className="py-1 pr-3">{i.descricao}</td>
                  <td className="pr-3">{i.unidade ?? "—"}</td>
                  <td className="pr-3">{i.qtdPrevista ?? "—"}</td>
                  <td className="pr-3">{i.hhPrevisto ?? "—"}</td>
                  {verValores && <td className="pr-3">{brl(i.valor)}</td>}
                  <td>{i.disciplina ?? "—"}</td>
                </tr>
              ))}
              {itens.length === 0 && <tr><td colSpan={verValores ? 6 : 5} className="py-2 text-slate-400">Sem itens{gereObras ? ". Importe um cronograma acima." : "."}</td></tr>}
            </tbody>
          </table>
        </div>
      </div>

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

      <div className="border-t border-slate-700 pt-4">
        <div className="flex items-center justify-between mb-2">
          <span className="text-sm text-slate-400">RDOs — {rdos?.length ?? 0}</span>
          <button onClick={() => novoRdo.mutate()} disabled={novoRdo.isPending} className="rounded-lg bg-sky-600 px-3 py-1.5 text-sm font-semibold hover:bg-sky-500 disabled:opacity-50">
            {novoRdo.isPending ? "Criando…" : "+ Novo RDO"}
          </button>
        </div>
        <ul className="space-y-1">
          {rdos?.map((r) => (
            <li key={r.id}>
              <button onClick={() => setRdoEditando(r.id)} className="w-full flex justify-between rounded-lg bg-slate-900 px-3 py-2 text-sm text-left hover:bg-slate-700">
                <span>RDO {r.numero}{r.revisao > 0 ? ` rev.${r.revisao}` : ""} · {r.data}</span>
                <span className="text-slate-400">{r.status}</span>
              </button>
            </li>
          ))}
          {rdos?.length === 0 && <li className="text-slate-500 text-sm">Nenhum RDO ainda.</li>}
        </ul>
      </div>
      </>}
    </div>
  );
}
