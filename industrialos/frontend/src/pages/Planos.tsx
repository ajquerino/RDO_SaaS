import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { api, type Plano } from "../lib/api";

const brl = (v?: number | null) => (v == null ? "—" : v.toLocaleString("pt-BR", { style: "currency", currency: "BRL" }));

/** Catálogo de planos do SaaS (Admin) — só o modelo; nenhuma cobrança implementada. */
export default function Planos() {
  const qc = useQueryClient();
  const [nome, setNome] = useState("");
  const [obras, setObras] = useState("");
  const [usuarios, setUsuarios] = useState("");
  const [preco, setPreco] = useState("");
  const [erro, setErro] = useState<string | null>(null);

  const { data: planos } = useQuery({ queryKey: ["planos"], queryFn: () => api<Plano[]>("/api/v1/planos") });

  const criar = useMutation({
    mutationFn: () => api<Plano>("/api/v1/planos", {
      method: "POST",
      body: JSON.stringify({
        nome,
        limiteObras: obras ? Number(obras) : null,
        limiteUsuarios: usuarios ? Number(usuarios) : null,
        precoMensal: preco ? Number(preco) : null,
      }),
    }),
    onSuccess: () => { setNome(""); setObras(""); setUsuarios(""); setPreco(""); setErro(null); qc.invalidateQueries({ queryKey: ["planos"] }); },
    onError: (e) => setErro((e as Error).message),
  });

  return (
    <div className="space-y-6">
      <p className="rounded-lg bg-slate-800/60 px-3 py-2 text-xs text-slate-400">
        Apenas o modelo de planos. Cobrança, checkout e trial não estão implementados (dependem de decisão sobre provedor de pagamento).
      </p>

      <form onSubmit={(e) => { e.preventDefault(); criar.mutate(); }} className="rounded-xl bg-slate-800 p-4 space-y-3">
        <h2 className="font-semibold">Novo plano</h2>
        <div className="grid gap-3 sm:grid-cols-4">
          <input className="rounded-lg bg-slate-900 px-3 py-2" placeholder="Nome *" value={nome} onChange={(e) => setNome(e.target.value)} required />
          <input className="rounded-lg bg-slate-900 px-3 py-2" type="number" placeholder="Limite de obras" value={obras} onChange={(e) => setObras(e.target.value)} />
          <input className="rounded-lg bg-slate-900 px-3 py-2" type="number" placeholder="Limite de usuários" value={usuarios} onChange={(e) => setUsuarios(e.target.value)} />
          <input className="rounded-lg bg-slate-900 px-3 py-2" type="number" step="0.01" placeholder="Preço mensal (R$)" value={preco} onChange={(e) => setPreco(e.target.value)} />
        </div>
        {erro && <p className="text-sm text-red-400">{erro}</p>}
        <button disabled={criar.isPending} className="rounded-lg bg-sky-600 px-4 py-2 font-semibold hover:bg-sky-500 disabled:opacity-50">
          {criar.isPending ? "Salvando..." : "Adicionar"}
        </button>
      </form>

      <ul className="space-y-2">
        {planos?.map((p) => (
          <li key={p.id} className="flex items-center justify-between rounded-lg bg-slate-800 px-4 py-3">
            <span className="font-medium">{p.nome}</span>
            <span className="text-sm text-slate-400">
              {p.limiteObras ?? "∞"} obras · {p.limiteUsuarios ?? "∞"} usuários · {brl(p.precoMensal)}/mês
            </span>
          </li>
        ))}
        {planos?.length === 0 && <p className="text-slate-400 text-sm">Nenhum plano cadastrado.</p>}
      </ul>
    </div>
  );
}
