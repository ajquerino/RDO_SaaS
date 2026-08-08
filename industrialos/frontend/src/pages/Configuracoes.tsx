import { useEffect, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { api } from "../lib/api";

type RegraHE = {
  limiteSemanalHoras: number; percentUtilFaixa1: number; percentUtilFaixa2: number;
  sabadoPercent: number; sabadoLimiteHoras: number; sabadoPercentAcima: number;
  sabadoUsaCorteHorario: boolean; sabadoHoraCorte: string | null; sabadoPercentAposCorte: number | null;
  domingoPercent: number; domingoLimiteHoras: number; domingoPercentAcima: number;
  feriadoPercent: number; feriadoLimiteHoras: number; feriadoPercentAcima: number;
};

const inputCls = "w-full rounded-lg bg-slate-900 px-3 py-2 text-sm";

function Num({ label, valor, onChange }: { label: string; valor: number; onChange: (v: number) => void }) {
  return (
    <label className="block text-sm">
      <span className="mb-1 block text-slate-400">{label}</span>
      <input type="number" min={0} step="0.01" className={inputCls} value={valor}
        onChange={(e) => onChange(e.target.value === "" ? 0 : Number(e.target.value))} />
    </label>
  );
}

export default function Configuracoes() {
  const qc = useQueryClient();
  const { data } = useQuery({ queryKey: ["config-he"], queryFn: () => api<RegraHE>("/api/v1/configuracoes/hora-extra") });
  const [form, setForm] = useState<RegraHE | null>(null);
  const [msg, setMsg] = useState<string | null>(null);
  const [erro, setErro] = useState<string | null>(null);

  useEffect(() => { if (data) setForm({ ...data, sabadoHoraCorte: data.sabadoHoraCorte?.slice(0, 5) ?? null }); }, [data]);

  const salvar = useMutation({
    mutationFn: (f: RegraHE) => api<RegraHE>("/api/v1/configuracoes/hora-extra", {
      method: "PUT",
      body: JSON.stringify({
        ...f,
        // se o corte estiver desligado, não envia hora/percent do corte
        sabadoHoraCorte: f.sabadoUsaCorteHorario ? (f.sabadoHoraCorte || null) : null,
        sabadoPercentAposCorte: f.sabadoUsaCorteHorario ? f.sabadoPercentAposCorte : null,
      }),
    }),
    onSuccess: () => { setMsg("Salvo. Vale a partir do próximo fechamento de HH."); setErro(null); qc.invalidateQueries({ queryKey: ["config-he"] }); },
    onError: (e) => { setErro((e as Error).message); setMsg(null); },
  });

  if (!form) return <p className="text-slate-400 text-sm">Carregando…</p>;
  const set = (patch: Partial<RegraHE>) => setForm({ ...form, ...patch });

  return (
    <form onSubmit={(e) => { e.preventDefault(); salvar.mutate(form); }} className="space-y-5">
      <div>
        <h2 className="text-lg font-semibold">Horas extras</h2>
        <p className="text-sm text-slate-400">Taxas aplicadas no fechamento semanal de HH. Vale a partir do próximo fechamento.</p>
      </div>

      <fieldset className="rounded-xl bg-slate-800 p-4 space-y-3">
        <legend className="px-1 text-sm font-medium text-sky-300">Dia útil</legend>
        <div className="grid gap-3 sm:grid-cols-3">
          <Num label="Limite semanal (horas) p/ 1ª faixa" valor={form.limiteSemanalHoras} onChange={(v) => set({ limiteSemanalHoras: v })} />
          <Num label="% 1ª faixa (até o limite)" valor={form.percentUtilFaixa1} onChange={(v) => set({ percentUtilFaixa1: v })} />
          <Num label="% 2ª faixa (acima)" valor={form.percentUtilFaixa2} onChange={(v) => set({ percentUtilFaixa2: v })} />
        </div>
      </fieldset>

      <fieldset className="rounded-xl bg-slate-800 p-4 space-y-3">
        <legend className="px-1 text-sm font-medium text-sky-300">Sábado</legend>
        <div className="grid gap-3 sm:grid-cols-3">
          <Num label="% base" valor={form.sabadoPercent} onChange={(v) => set({ sabadoPercent: v })} />
          <Num label="Limite diário (horas)" valor={form.sabadoLimiteHoras} onChange={(v) => set({ sabadoLimiteHoras: v })} />
          <Num label="% acima do limite" valor={form.sabadoPercentAcima} onChange={(v) => set({ sabadoPercentAcima: v })} />
        </div>
        <label className="flex items-center gap-2 text-sm text-slate-300">
          <input type="checkbox" checked={form.sabadoUsaCorteHorario} onChange={(e) => set({ sabadoUsaCorteHorario: e.target.checked })} />
          Usar corte por horário (ex.: até meio-dia uma %, depois outra) — ignora o limite por horas acima
        </label>
        {form.sabadoUsaCorteHorario && (
          <div className="grid gap-3 sm:grid-cols-2">
            <label className="block text-sm">
              <span className="mb-1 block text-slate-400">Hora de corte</span>
              <input type="time" className={inputCls} value={form.sabadoHoraCorte ?? ""} onChange={(e) => set({ sabadoHoraCorte: e.target.value || null })} />
            </label>
            <Num label="% após o corte" valor={form.sabadoPercentAposCorte ?? 0} onChange={(v) => set({ sabadoPercentAposCorte: v })} />
          </div>
        )}
      </fieldset>

      <fieldset className="rounded-xl bg-slate-800 p-4 space-y-3">
        <legend className="px-1 text-sm font-medium text-sky-300">Domingo</legend>
        <div className="grid gap-3 sm:grid-cols-3">
          <Num label="% base" valor={form.domingoPercent} onChange={(v) => set({ domingoPercent: v })} />
          <Num label="Limite diário (horas)" valor={form.domingoLimiteHoras} onChange={(v) => set({ domingoLimiteHoras: v })} />
          <Num label="% acima do limite" valor={form.domingoPercentAcima} onChange={(v) => set({ domingoPercentAcima: v })} />
        </div>
      </fieldset>

      <fieldset className="rounded-xl bg-slate-800 p-4 space-y-3">
        <legend className="px-1 text-sm font-medium text-sky-300">Feriado</legend>
        <div className="grid gap-3 sm:grid-cols-3">
          <Num label="% base" valor={form.feriadoPercent} onChange={(v) => set({ feriadoPercent: v })} />
          <Num label="Limite diário (horas)" valor={form.feriadoLimiteHoras} onChange={(v) => set({ feriadoLimiteHoras: v })} />
          <Num label="% acima do limite" valor={form.feriadoPercentAcima} onChange={(v) => set({ feriadoPercentAcima: v })} />
        </div>
      </fieldset>

      {erro && <p className="text-sm text-red-400">{erro}</p>}
      {msg && <p className="text-sm text-emerald-400">{msg}</p>}
      <button disabled={salvar.isPending} className="rounded-lg bg-sky-600 px-4 py-2 font-semibold hover:bg-sky-500 disabled:opacity-50">
        {salvar.isPending ? "Salvando…" : "Salvar configurações"}
      </button>
    </form>
  );
}
