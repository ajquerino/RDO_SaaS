import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { api, type AuditoriaPagina } from "../lib/api";

const ENTIDADES = ["", "Obra", "Cliente", "Usuario", "Rdo", "Medicao", "Equipamento", "Documento"];
const COR: Record<string, string> = { create: "text-emerald-400", update: "text-sky-400", delete: "text-red-400" };

/** Trilha de auditoria (Admin): quem fez o quê e quando. */
export default function Auditoria() {
  const [page, setPage] = useState(1);
  const [entidade, setEntidade] = useState("");
  const size = 50;

  const { data } = useQuery({
    queryKey: ["auditoria", page, entidade],
    queryFn: () => api<AuditoriaPagina>(`/api/v1/auditoria?page=${page}&size=${size}${entidade ? `&entidade=${entidade}` : ""}`),
  });

  const total = data?.total ?? 0;
  const paginas = Math.max(1, Math.ceil(total / size));

  const campos = (detalhe: string): string => {
    try { const d = JSON.parse(detalhe); return Array.isArray(d?.campos) ? d.campos.join(", ") : ""; }
    catch { return ""; }
  };

  return (
    <div className="space-y-4">
      <div className="flex flex-wrap items-center gap-2">
        <select value={entidade} onChange={(e) => { setEntidade(e.target.value); setPage(1); }} className="rounded-lg bg-slate-900 px-3 py-2 text-sm">
          {ENTIDADES.map((e) => <option key={e} value={e}>{e || "Todas as entidades"}</option>)}
        </select>
        <span className="text-sm text-slate-400">{total} registro(s)</span>
      </div>

      <div className="overflow-x-auto">
        <table className="w-full text-sm">
          <thead className="text-left text-slate-400">
            <tr><th className="py-1 pr-3">Quando</th><th className="pr-3">Ação</th><th className="pr-3">Entidade</th><th className="pr-3">Registro</th><th>Campos</th></tr>
          </thead>
          <tbody>
            {data?.itens.map((a) => (
              <tr key={a.id} className="border-t border-slate-700/50">
                <td className="py-1 pr-3 whitespace-nowrap">{new Date(a.criadoEm).toLocaleString("pt-BR")}</td>
                <td className={`pr-3 font-medium ${COR[a.acao] ?? ""}`}>{a.acao}</td>
                <td className="pr-3">{a.entidade}</td>
                <td className="pr-3 text-slate-500">{a.entidadeId ? a.entidadeId.slice(0, 8) : "—"}</td>
                <td className="text-slate-400">{campos(a.detalhe) || "—"}</td>
              </tr>
            ))}
            {data?.itens.length === 0 && <tr><td colSpan={5} className="py-2 text-slate-400">Nenhum registro.</td></tr>}
          </tbody>
        </table>
      </div>

      {paginas > 1 && (
        <div className="flex items-center gap-3 text-sm">
          <button disabled={page <= 1} onClick={() => setPage((p) => p - 1)} className="rounded-lg bg-slate-700 px-3 py-1 disabled:opacity-40">‹ Anterior</button>
          <span className="text-slate-400">Página {page} de {paginas}</span>
          <button disabled={page >= paginas} onClick={() => setPage((p) => p + 1)} className="rounded-lg bg-slate-700 px-3 py-1 disabled:opacity-40">Próxima ›</button>
        </div>
      )}
    </div>
  );
}
