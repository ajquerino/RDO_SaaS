import { useEffect, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  api, apiBlob, type CondicaoPagamento, type Faturamento, type FaturamentoEvento,
  type MedicaoCalc, type MedicaoLista,
} from "../lib/api";
import { useAuth } from "../store/auth";

const brl = (v?: number) => (v == null ? "—" : v.toLocaleString("pt-BR", { style: "currency", currency: "BRL" }));
const pct = (f?: number) => (f == null ? "—" : `${(f * 100).toLocaleString("pt-BR", { maximumFractionDigits: 1 })}%`);
const inp = "w-full rounded-lg bg-slate-900 px-3 py-2 text-sm";

const TIPOS = [
  "assinatura_contrato", "entrada", "mobilizacao", "canteiro_mensal",
  "medicao_periodica", "comissionamento", "entrega_tecnica", "entrega_final", "outro",
];

/** Aba Medição da obra (Sprint 7): plano de faturamento + emissão de boletins. */
export default function Medicao({ obraId }: { obraId: string }) {
  return (
    <div className="space-y-6">
      <PlanoFaturamento obraId={obraId} />
      <EmitirBoletim obraId={obraId} />
      <BoletinsEmitidos obraId={obraId} />
    </div>
  );
}

// ---------- Plano de faturamento ----------
function PlanoFaturamento({ obraId }: { obraId: string }) {
  const qc = useQueryClient();
  const { data } = useQuery({ queryKey: ["faturamento", obraId], queryFn: () => api<Faturamento>(`/api/v1/obras/${obraId}/faturamento`) });
  const { data: condicoes } = useQuery({ queryKey: ["condicoes"], queryFn: () => api<CondicaoPagamento[]>("/api/v1/condicoes-pagamento") });

  const [nome, setNome] = useState("");
  const [valor, setValor] = useState("");
  const [condId, setCondId] = useState("");
  const [eventos, setEventos] = useState<FaturamentoEvento[] | null>(null);
  const [msg, setMsg] = useState<string | null>(null);

  // hidrata o formulário a partir do que veio do backend (uma vez por carregamento)
  useEffect(() => {
    if (data === undefined || eventos !== null) return;
    setNome(data.plano?.nome ?? "Plano de faturamento");
    setValor(data.plano ? String(data.plano.valorContrato) : "");
    setCondId(data.plano?.condicaoPagamentoId ?? "");
    setEventos(data.eventos ?? []);
  }, [data, eventos]);
  const evs = eventos ?? [];

  const salvar = useMutation({
    mutationFn: () => api(`/api/v1/obras/${obraId}/faturamento`, {
      method: "POST",
      body: JSON.stringify({ nome, valorContrato: Number(valor) || 0, condicaoPagamentoId: condId || null, eventos: evs }),
    }),
    onSuccess: () => { setMsg("Plano salvo."); qc.invalidateQueries({ queryKey: ["faturamento", obraId] }); },
    onError: (e) => setMsg((e as Error).message),
  });

  const addEvento = () => setEventos([...evs, { tipo: "medicao_periodica", base: "percentual", ordem: evs.length + 1 }]);
  const setEv = (i: number, patch: Partial<FaturamentoEvento>) => setEventos(evs.map((e, k) => (k === i ? { ...e, ...patch } : e)));
  const delEv = (i: number) => setEventos(evs.filter((_, k) => k !== i));

  return (
    <section className="rounded-xl bg-slate-800 p-4 space-y-3">
      <h3 className="font-semibold">Plano de faturamento</h3>
      <div className="grid gap-3 sm:grid-cols-3">
        <label className="text-sm">Nome<input className={inp} value={nome} onChange={(e) => setNome(e.target.value)} /></label>
        <label className="text-sm">Valor de contrato (R$)<input className={inp} type="number" step="0.01" value={valor} onChange={(e) => setValor(e.target.value)} /></label>
        <label className="text-sm">Condição de pagamento (default)
          <select className={inp} value={condId} onChange={(e) => setCondId(e.target.value)}>
            <option value="">—</option>
            {condicoes?.map((c) => <option key={c.id} value={c.id}>{c.nome}</option>)}
          </select>
        </label>
      </div>

      <CondicoesInline />

      <div className="space-y-2">
        <div className="flex items-center justify-between">
          <span className="text-sm text-slate-400">Eventos / marcos</span>
          <button onClick={addEvento} className="rounded-lg bg-slate-700 px-3 py-1 text-sm hover:bg-slate-600">+ Evento</button>
        </div>
        {evs.length === 0 && <p className="text-sm text-slate-500">Nenhum evento. Marcos como mobilização, entrada, comissionamento são faturados independentes da medição por avanço.</p>}
        {evs.map((e, i) => (
          <div key={i} className="grid gap-2 sm:grid-cols-12 items-center rounded-lg bg-slate-900/60 p-2">
            <select className={`${inp} sm:col-span-3`} value={e.tipo} onChange={(ev) => setEv(i, { tipo: ev.target.value })}>
              {TIPOS.map((t) => <option key={t} value={t}>{t}</option>)}
            </select>
            <select className={`${inp} sm:col-span-2`} value={e.base} onChange={(ev) => setEv(i, { base: ev.target.value })}>
              <option value="percentual">percentual</option>
              <option value="valor_fixo">valor_fixo</option>
            </select>
            {e.base === "percentual" ? (
              <input className={`${inp} sm:col-span-2`} type="number" step="0.01" placeholder="%" value={e.percentual ?? ""} onChange={(ev) => setEv(i, { percentual: ev.target.value === "" ? null : Number(ev.target.value) })} />
            ) : (
              <input className={`${inp} sm:col-span-2`} type="number" step="0.01" placeholder="R$" value={e.valor ?? ""} onChange={(ev) => setEv(i, { valor: ev.target.value === "" ? null : Number(ev.target.value) })} />
            )}
            <input className={`${inp} sm:col-span-4`} placeholder="Descrição" value={e.descricao ?? ""} onChange={(ev) => setEv(i, { descricao: ev.target.value })} />
            <button onClick={() => delEv(i)} className="sm:col-span-1 rounded-lg bg-red-900/60 px-2 py-2 text-sm text-red-200 hover:bg-red-900">✕</button>
          </div>
        ))}
      </div>

      <div className="flex items-center gap-3">
        <button onClick={() => salvar.mutate()} disabled={salvar.isPending} className="rounded-lg bg-sky-600 px-4 py-2 text-sm font-semibold hover:bg-sky-500 disabled:opacity-50">
          {salvar.isPending ? "Salvando…" : "Salvar plano"}
        </button>
        {msg && <span className="text-sm text-emerald-400">{msg}</span>}
      </div>
    </section>
  );
}

// criação rápida de condições de pagamento (presets + custom)
function CondicoesInline() {
  const qc = useQueryClient();
  const [aberto, setAberto] = useState(false);
  const [nome, setNome] = useState("");
  const [dias, setDias] = useState(""); // ex.: "21/42" ou "21/35/42/60"

  const criar = useMutation({
    mutationFn: (body: { nome: string; parcelas: { dias: number; pct?: number }[] }) =>
      api("/api/v1/condicoes-pagamento", { method: "POST", body: JSON.stringify(body) }),
    onSuccess: () => { setNome(""); setDias(""); qc.invalidateQueries({ queryKey: ["condicoes"] }); },
  });

  const preset = (label: string, ds: number[]) => criar.mutate({ nome: label, parcelas: ds.map((d) => ({ dias: d })) });
  const criarCustom = () => {
    const ds = dias.split("/").map((s) => parseInt(s.trim(), 10)).filter((n) => !isNaN(n));
    if (ds.length === 0) return;
    criar.mutate({ nome: nome.trim() || dias.trim(), parcelas: ds.map((d) => ({ dias: d })) });
  };

  return (
    <div className="rounded-lg bg-slate-900/40 p-3 text-sm">
      <button onClick={() => setAberto(!aberto)} className="text-sky-400">{aberto ? "▾" : "▸"} Condições de pagamento</button>
      {aberto && (
        <div className="mt-2 space-y-2">
          <div className="flex flex-wrap gap-2">
            <button onClick={() => preset("21/42", [21, 42])} className="rounded-lg bg-slate-700 px-2 py-1 hover:bg-slate-600">21/42</button>
            <button onClick={() => preset("21/35/42/60", [21, 35, 42, 60])} className="rounded-lg bg-slate-700 px-2 py-1 hover:bg-slate-600">21/35/42/60</button>
            <button onClick={() => preset("30/45/60/180", [30, 45, 60, 180])} className="rounded-lg bg-slate-700 px-2 py-1 hover:bg-slate-600">30/45/60/180</button>
            <button onClick={() => preset("100% à vista", [0])} className="rounded-lg bg-slate-700 px-2 py-1 hover:bg-slate-600">100% à vista</button>
          </div>
          <div className="flex flex-wrap gap-2 items-center">
            <input className="rounded-lg bg-slate-900 px-2 py-1" placeholder="Nome (opcional)" value={nome} onChange={(e) => setNome(e.target.value)} />
            <input className="rounded-lg bg-slate-900 px-2 py-1" placeholder="Dias ex.: 21/42" value={dias} onChange={(e) => setDias(e.target.value)} />
            <button onClick={criarCustom} className="rounded-lg bg-sky-600 px-3 py-1 font-semibold hover:bg-sky-500">Criar</button>
            <span className="text-slate-500">parcelas divididas igualmente</span>
          </div>
        </div>
      )}
    </div>
  );
}

// ---------- Emitir boletim ----------
function EmitirBoletim({ obraId }: { obraId: string }) {
  const qc = useQueryClient();
  const hoje = new Date().toISOString().slice(0, 10);
  const [de, setDe] = useState(hoje);
  const [ate, setAte] = useState(hoje);
  const [condId, setCondId] = useState("");
  const [calc, setCalc] = useState<MedicaoCalc | null>(null);
  const [erro, setErro] = useState<string | null>(null);

  const { data: condicoes } = useQuery({ queryKey: ["condicoes"], queryFn: () => api<CondicaoPagamento[]>("/api/v1/condicoes-pagamento") });

  const calcular = useMutation({
    mutationFn: () => api<MedicaoCalc>(`/api/v1/obras/${obraId}/medicoes/calcular`, {
      method: "POST", body: JSON.stringify({ de, ate, condicaoPagamentoId: condId || null }),
    }),
    onSuccess: (r) => { setCalc(r); setErro(null); },
    onError: (e) => setErro((e as Error).message),
  });

  const salvar = useMutation({
    mutationFn: () => api<{ id: string }>(`/api/v1/obras/${obraId}/medicoes`, {
      method: "POST", body: JSON.stringify({ de, ate, condicaoPagamentoId: condId || null }),
    }),
    onSuccess: () => { setCalc(null); qc.invalidateQueries({ queryKey: ["medicoes", obraId] }); },
    onError: (e) => setErro((e as Error).message),
  });

  return (
    <section className="rounded-xl bg-slate-800 p-4 space-y-3">
      <h3 className="font-semibold">Emitir boletim de medição</h3>
      <div className="grid gap-3 sm:grid-cols-4">
        <label className="text-sm">De<input className={inp} type="date" value={de} onChange={(e) => setDe(e.target.value)} /></label>
        <label className="text-sm">Até<input className={inp} type="date" value={ate} onChange={(e) => setAte(e.target.value)} /></label>
        <label className="text-sm">Condição
          <select className={inp} value={condId} onChange={(e) => setCondId(e.target.value)}>
            <option value="">Do plano</option>
            {condicoes?.map((c) => <option key={c.id} value={c.id}>{c.nome}</option>)}
          </select>
        </label>
        <div className="flex items-end">
          <button onClick={() => calcular.mutate()} disabled={calcular.isPending} className="w-full rounded-lg bg-slate-700 px-4 py-2 text-sm font-semibold hover:bg-slate-600 disabled:opacity-50">
            {calcular.isPending ? "Calculando…" : "Calcular"}
          </button>
        </div>
      </div>
      {erro && <p className="text-sm text-red-400">{erro}</p>}

      {calc && (
        <div className="space-y-4">
          <div className="grid grid-cols-2 gap-2 sm:grid-cols-5">
            <Card rotulo="Valor contrato" valor={brl(calc.valorContrato)} />
            <Card rotulo="Medido no período" valor={brl(calc.valorPeriodo)} />
            <Card rotulo="Medido acumulado" valor={brl(calc.medidoAcumulado)} />
            <Card rotulo="Avanço físico" valor={pct(calc.pctFisico)} sub={`HH ${pct(calc.avancoHh)}`} />
            <Card rotulo="Avanço financeiro" valor={pct(calc.pctFinanceiro)} />
          </div>

          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead className="text-slate-400 text-left">
                <tr><th className="py-1 pr-3">Item</th><th className="pr-3">% ini</th><th className="pr-3">% fim</th><th className="pr-3">Valor</th><th className="pr-3">Medido período</th><th>Medido acum.</th></tr>
              </thead>
              <tbody>
                {calc.itens.map((i) => (
                  <tr key={i.obraItemId} className="border-t border-slate-700/50">
                    <td className="py-1 pr-3">{i.descricao}</td>
                    <td className="pr-3">{pct(i.pctIni)}</td>
                    <td className="pr-3">{pct(i.pctFim)}</td>
                    <td className="pr-3">{brl(i.valor)}</td>
                    <td className="pr-3">{brl(i.medidoPeriodo)}</td>
                    <td>{brl(i.medidoAcum)}</td>
                  </tr>
                ))}
                {calc.itens.length === 0 && <tr><td colSpan={6} className="py-2 text-slate-400">Nenhum item com valor no contrato.</td></tr>}
              </tbody>
            </table>
          </div>

          <div>
            <p className="text-sm text-slate-400 mb-1">Faturamento — parcelas</p>
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead className="text-slate-400 text-left"><tr><th className="py-1 pr-3">Dias</th><th className="pr-3">Vencimento</th><th className="pr-3">%</th><th>Valor</th></tr></thead>
                <tbody>
                  {calc.parcelas.map((p, k) => (
                    <tr key={k} className="border-t border-slate-700/50">
                      <td className="py-1 pr-3">{p.dias}</td>
                      <td className="pr-3">{new Date(p.vencimento).toLocaleDateString("pt-BR")}</td>
                      <td className="pr-3">{p.pct.toLocaleString("pt-BR", { maximumFractionDigits: 2 })}%</td>
                      <td>{brl(p.valor)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>

          <button onClick={() => salvar.mutate()} disabled={salvar.isPending} className="rounded-lg bg-emerald-600 px-4 py-2 font-semibold hover:bg-emerald-500 disabled:opacity-50">
            {salvar.isPending ? "Salvando…" : "Salvar boletim"}
          </button>
        </div>
      )}
    </section>
  );
}

// ---------- Boletins emitidos ----------
function BoletinsEmitidos({ obraId }: { obraId: string }) {
  const qc = useQueryClient();
  const ehAdmin = useAuth((s) => s.usuario)?.funcao === "Admin";
  const { data } = useQuery({ queryKey: ["medicoes", obraId], queryFn: () => api<MedicaoLista[]>(`/api/v1/obras/${obraId}/medicoes`) });

  const apagar = useMutation({
    mutationFn: (id: string) => api(`/api/v1/medicoes/${id}`, { method: "DELETE" }),
    onSuccess: () => qc.invalidateQueries({ queryKey: ["medicoes", obraId] }),
  });

  async function abrirPdf(id: string) {
    const blob = await apiBlob(`/api/v1/medicoes/${id}/pdf`);
    window.open(URL.createObjectURL(blob), "_blank");
  }

  return (
    <section className="rounded-xl bg-slate-800 p-4">
      <h3 className="font-semibold mb-2">Boletins emitidos</h3>
      <ul className="space-y-1">
        {data?.map((m) => (
          <li key={m.id} className="flex items-center justify-between rounded-lg bg-slate-900 px-3 py-2 text-sm">
            <span>
              <span className="font-medium">BM {m.numero}</span>
              <span className="text-slate-400"> · {new Date(m.de).toLocaleDateString("pt-BR")}–{new Date(m.ate).toLocaleDateString("pt-BR")} · {brl(m.valorPeriodo)} · fís. {pct(m.pctFisico)} · {m.status}</span>
            </span>
            <span className="flex gap-2">
              <button onClick={() => abrirPdf(m.id)} className="rounded-lg bg-slate-700 px-3 py-1 text-xs hover:bg-slate-600">PDF</button>
              {ehAdmin && <button onClick={() => { if (confirm(`Apagar BM ${m.numero}?`)) apagar.mutate(m.id); }} className="rounded-lg bg-red-900/60 px-3 py-1 text-xs text-red-200 hover:bg-red-900">Apagar</button>}
            </span>
          </li>
        ))}
        {data?.length === 0 && <li className="text-slate-500 text-sm">Nenhum boletim emitido.</li>}
      </ul>
    </section>
  );
}

function Card({ rotulo, valor, sub }: { rotulo: string; valor: string; sub?: string }) {
  return (
    <div className="rounded-xl bg-slate-900 p-3">
      <p className="text-xs text-slate-400">{rotulo}</p>
      <p className="text-lg font-bold leading-tight">{valor}</p>
      {sub && <p className="text-xs text-slate-500">{sub}</p>}
    </div>
  );
}
