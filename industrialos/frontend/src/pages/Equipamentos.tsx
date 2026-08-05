import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { api, type Equipamento } from "../lib/api";

/** Catálogo de equipamentos do tenant (usado no RDO). Gestão para Planejador/Gestor/Admin. */
export default function Equipamentos() {
  const qc = useQueryClient();
  const [nome, setNome] = useState("");
  const [tipo, setTipo] = useState("");
  const [proprioLocado, setProprioLocado] = useState("proprio");
  const [custoHora, setCustoHora] = useState("");
  const [erro, setErro] = useState<string | null>(null);

  const { data: equipamentos } = useQuery({ queryKey: ["equipamentos"], queryFn: () => api<Equipamento[]>("/api/v1/equipamentos") });

  const criar = useMutation({
    mutationFn: () => api<Equipamento>("/api/v1/equipamentos", {
      method: "POST",
      body: JSON.stringify({ nome, tipo: tipo || null, proprioLocado, custoHora: custoHora ? Number(custoHora) : null }),
    }),
    onSuccess: () => { setNome(""); setTipo(""); setProprioLocado("proprio"); setCustoHora(""); setErro(null); qc.invalidateQueries({ queryKey: ["equipamentos"] }); },
    onError: (e) => setErro((e as Error).message),
  });
  const apagar = useMutation({
    mutationFn: (id: string) => api(`/api/v1/equipamentos/${id}`, { method: "DELETE" }),
    onSuccess: () => qc.invalidateQueries({ queryKey: ["equipamentos"] }),
  });

  return (
    <div className="space-y-6">
      <form onSubmit={(e) => { e.preventDefault(); criar.mutate(); }} className="rounded-xl bg-slate-800 p-4 space-y-3">
        <h2 className="font-semibold">Novo equipamento</h2>
        <div className="grid gap-3 sm:grid-cols-4">
          <input className="rounded-lg bg-slate-900 px-3 py-2" placeholder="Nome *" value={nome} onChange={(e) => setNome(e.target.value)} required />
          <input className="rounded-lg bg-slate-900 px-3 py-2" placeholder="Tipo" value={tipo} onChange={(e) => setTipo(e.target.value)} />
          <select className="rounded-lg bg-slate-900 px-3 py-2" value={proprioLocado} onChange={(e) => setProprioLocado(e.target.value)}>
            <option value="proprio">Próprio</option>
            <option value="locado">Locado</option>
          </select>
          <input className="rounded-lg bg-slate-900 px-3 py-2" type="number" step="0.01" placeholder="Custo/hora (R$)" value={custoHora} onChange={(e) => setCustoHora(e.target.value)} />
        </div>
        {erro && <p className="text-sm text-red-400">{erro}</p>}
        <button disabled={criar.isPending} className="rounded-lg bg-sky-600 px-4 py-2 font-semibold hover:bg-sky-500 disabled:opacity-50">
          {criar.isPending ? "Salvando..." : "Adicionar"}
        </button>
      </form>

      <ul className="space-y-2">
        {equipamentos?.map((e) => (
          <li key={e.id} className="flex items-center justify-between rounded-lg bg-slate-800 px-4 py-3">
            <span>
              <span className="font-medium">{e.nome}</span>
              <span className="block text-sm text-slate-400">
                {e.tipo ?? "sem tipo"} · {e.proprioLocado}{e.custoHora != null ? ` · ${e.custoHora.toLocaleString("pt-BR", { style: "currency", currency: "BRL" })}/h` : ""}
              </span>
            </span>
            <button onClick={() => { if (confirm(`Excluir "${e.nome}"?`)) apagar.mutate(e.id); }} className="rounded-lg bg-red-900/60 px-3 py-1 text-xs text-red-200 hover:bg-red-900">Excluir</button>
          </li>
        ))}
        {equipamentos?.length === 0 && <p className="text-slate-400 text-sm">Nenhum equipamento ainda.</p>}
      </ul>
    </div>
  );
}
