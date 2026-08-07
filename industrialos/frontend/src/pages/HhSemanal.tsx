import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { api } from "../lib/api";

type DiaHh = {
  data: string; diaTipo: string; nPessoas: number;
  normalHH: number; extra50HH: number; extra70HH: number; fds100HH: number; fds150HH: number;
};
type HhSemana = {
  de: string; ate: string;
  normalHH: number; extra50HH: number; extra70HH: number; fds100HH: number; fds150HH: number; totalHH: number;
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
const fmtDia = (iso: string) => new Date(iso + "T00:00:00").toLocaleDateString("pt-BR", { weekday: "short", day: "2-digit", month: "2-digit" });

export default function HhSemanal({ obraId }: { obraId: string }) {
  const [de, setDe] = useState(() => segundaDe(new Date().toISOString().slice(0, 10)));
  const { data, isLoading } = useQuery({
    queryKey: ["hh-semanal", obraId, de],
    queryFn: () => api<HhSemana>(`/api/v1/obras/${obraId}/hh-semanal?de=${de}`),
  });

  const baldes: { rotulo: string; valor?: number; cor: string }[] = [
    { rotulo: "Normal", valor: data?.normalHH, cor: "text-slate-100" },
    { rotulo: "Extra 50%", valor: data?.extra50HH, cor: "text-amber-300" },
    { rotulo: "Extra 70%", valor: data?.extra70HH, cor: "text-amber-400" },
    { rotulo: "100%", valor: data?.fds100HH, cor: "text-orange-300" },
    { rotulo: "150%", valor: data?.fds150HH, cor: "text-orange-400" },
    { rotulo: "Total", valor: data?.totalHH, cor: "text-sky-300 font-bold" },
  ];

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between gap-2">
        <button onClick={() => setDe(somaDias(de, -7))} className="rounded-lg bg-slate-700 px-3 py-1.5 text-sm hover:bg-slate-600">‹ Semana</button>
        <span className="text-sm text-slate-300">
          {data ? `${fmtDia(data.de)} — ${fmtDia(data.ate)}` : "…"}
        </span>
        <button onClick={() => setDe(somaDias(de, 7))} className="rounded-lg bg-slate-700 px-3 py-1.5 text-sm hover:bg-slate-600">Semana ›</button>
      </div>

      <div className="grid grid-cols-2 gap-2 sm:grid-cols-6">
        {baldes.map((b) => (
          <div key={b.rotulo} className="rounded-xl bg-slate-800 p-3 text-center">
            <p className="text-xs text-slate-400">{b.rotulo}</p>
            <p className={`text-xl leading-tight ${b.cor}`}>{b.valor != null ? fmtHH(b.valor) : "—"}</p>
            <p className="text-[10px] text-slate-500">HH</p>
          </div>
        ))}
      </div>

      <div className="overflow-x-auto rounded-xl bg-slate-800">
        <table className="w-full text-sm">
          <thead className="text-left text-slate-400">
            <tr>
              <th className="px-3 py-2">Dia</th><th className="px-2">Tipo</th><th className="px-2 text-right">Pessoas</th>
              <th className="px-2 text-right">Normal</th><th className="px-2 text-right">50%</th><th className="px-2 text-right">70%</th>
              <th className="px-2 text-right">100%</th><th className="px-2 text-right">150%</th>
            </tr>
          </thead>
          <tbody className="tabular-nums">
            {data?.porDia.map((d) => (
              <tr key={d.data} className="border-t border-slate-700/50">
                <td className="px-3 py-1.5">{fmtDia(d.data)}</td>
                <td className="px-2 text-slate-400">{d.diaTipo}</td>
                <td className="px-2 text-right">{d.nPessoas}</td>
                <td className="px-2 text-right">{fmtHH(d.normalHH)}</td>
                <td className="px-2 text-right text-amber-300">{fmtHH(d.extra50HH)}</td>
                <td className="px-2 text-right text-amber-400">{fmtHH(d.extra70HH)}</td>
                <td className="px-2 text-right text-orange-300">{fmtHH(d.fds100HH)}</td>
                <td className="px-2 text-right text-orange-400">{fmtHH(d.fds150HH)}</td>
              </tr>
            ))}
            {!isLoading && data?.porDia.length === 0 && (
              <tr><td colSpan={8} className="px-3 py-3 text-slate-400">Nenhum RDO nesta semana.</td></tr>
            )}
          </tbody>
        </table>
      </div>

      <p className="text-xs text-slate-500">
        Extra de dia útil: as primeiras 10h da semana (por pessoa) entram em 50%, o excedente em 70%.
        Sábado/domingo/feriado: 100% até 8h e 150% acima. Só quantidade de HH (sem valores R$).
      </p>
    </div>
  );
}
