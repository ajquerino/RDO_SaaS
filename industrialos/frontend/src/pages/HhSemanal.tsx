import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { api } from "../lib/api";

type Faixa = { percentual: number; horas: number };
type DiaHh = { data: string; diaTipo: string; nPessoas: number; normalHH: number; extras: Faixa[] };
type HhSemana = {
  de: string; ate: string;
  normalHH: number; extras: Faixa[]; totalHH: number;
  porDia: DiaHh[];
};

// segunda-feira da semana de uma data (YYYY-MM-DD), em horário local
function segundaDe(iso: string): string {
  const d = new Date(iso + "T00:00:00");
  const diff = (d.getDay() + 6) % 7; // 0 = segunda
  d.setDate(d.getDate() - diff);
  return d.toISOString().slice(0, 10);
}
function somaDias(iso: string, n: number): string {
  const d = new Date(iso + "T00:00:00");
  d.setDate(d.getDate() + n);
  return d.toISOString().slice(0, 10);
}
const fmtHH = (h: number) => h.toLocaleString("pt-BR", { minimumFractionDigits: 0, maximumFractionDigits: 2 });
const fmtPct = (p: number) => `${p.toLocaleString("pt-BR", { maximumFractionDigits: 2 })}%`;
const fmtDia = (iso: string) => new Date(iso + "T00:00:00").toLocaleDateString("pt-BR", { weekday: "short", day: "2-digit", month: "2-digit" });
const horasDe = (extras: Faixa[], pct: number) => extras.find((f) => f.percentual === pct)?.horas ?? 0;

export default function HhSemanal({ obraId }: { obraId: string }) {
  const [de, setDe] = useState(() => segundaDe(new Date().toISOString().slice(0, 10)));
  const { data, isLoading } = useQuery({
    queryKey: ["hh-semanal", obraId, de],
    queryFn: () => api<HhSemana>(`/api/v1/obras/${obraId}/hh-semanal?de=${de}`),
  });

  // Percentuais presentes na semana (colunas dinâmicas — variam com a regra da empresa).
  const percentuais = data?.extras.map((f) => f.percentual) ?? [];

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between gap-2">
        <button onClick={() => setDe(somaDias(de, -7))} className="rounded-lg bg-slate-700 px-3 py-1.5 text-sm hover:bg-slate-600">‹ Semana</button>
        <span className="text-sm text-slate-300">{data ? `${fmtDia(data.de)} — ${fmtDia(data.ate)}` : "…"}</span>
        <button onClick={() => setDe(somaDias(de, 7))} className="rounded-lg bg-slate-700 px-3 py-1.5 text-sm hover:bg-slate-600">Semana ›</button>
      </div>

      <div className="grid grid-cols-2 gap-2 sm:grid-cols-4">
        <div className="rounded-xl bg-slate-800 p-3 text-center">
          <p className="text-xs text-slate-400">Normal</p>
          <p className="text-xl leading-tight text-slate-100">{data ? fmtHH(data.normalHH) : "—"}</p>
          <p className="text-[10px] text-slate-500">HH</p>
        </div>
        {data?.extras.map((f) => (
          <div key={f.percentual} className="rounded-xl bg-slate-800 p-3 text-center">
            <p className="text-xs text-slate-400">Extra {fmtPct(f.percentual)}</p>
            <p className="text-xl leading-tight text-amber-300">{fmtHH(f.horas)}</p>
            <p className="text-[10px] text-slate-500">HH</p>
          </div>
        ))}
        <div className="rounded-xl bg-slate-800 p-3 text-center">
          <p className="text-xs text-slate-400">Total</p>
          <p className="text-xl font-bold leading-tight text-sky-300">{data ? fmtHH(data.totalHH) : "—"}</p>
          <p className="text-[10px] text-slate-500">HH</p>
        </div>
      </div>

      <div className="overflow-x-auto rounded-xl bg-slate-800">
        <table className="w-full text-sm">
          <thead className="text-left text-slate-400">
            <tr>
              <th className="px-3 py-2">Dia</th><th className="px-2">Tipo</th><th className="px-2 text-right">Pessoas</th>
              <th className="px-2 text-right">Normal</th>
              {percentuais.map((p) => <th key={p} className="px-2 text-right">{fmtPct(p)}</th>)}
            </tr>
          </thead>
          <tbody className="tabular-nums">
            {data?.porDia.map((d) => (
              <tr key={d.data} className="border-t border-slate-700/50">
                <td className="px-3 py-1.5">{fmtDia(d.data)}</td>
                <td className="px-2 text-slate-400">{d.diaTipo}</td>
                <td className="px-2 text-right">{d.nPessoas}</td>
                <td className="px-2 text-right">{fmtHH(d.normalHH)}</td>
                {percentuais.map((p) => <td key={p} className="px-2 text-right text-amber-300">{fmtHH(horasDe(d.extras, p))}</td>)}
              </tr>
            ))}
            {!isLoading && data?.porDia.length === 0 && (
              <tr><td colSpan={4 + percentuais.length} className="px-3 py-3 text-slate-400">Nenhum RDO nesta semana.</td></tr>
            )}
          </tbody>
        </table>
      </div>

      <p className="text-xs text-slate-500">
        As taxas de hora-extra seguem a configuração da empresa (Configurações › Horas extras).
        Só quantidade de HH (sem valores R$).
      </p>
    </div>
  );
}
