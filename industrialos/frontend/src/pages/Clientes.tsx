import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { api, type Cliente } from "../lib/api";
import { useAuth, podeExcluirClientes } from "../store/auth";

export default function Clientes() {
  const qc = useQueryClient();
  const podeExcluir = podeExcluirClientes(useAuth((s) => s.usuario)?.funcao);
  const [nome, setNome] = useState("");
  const [cnpj, setCnpj] = useState("");
  const [contato, setContato] = useState("");
  const [erro, setErro] = useState<string | null>(null);

  const { data: clientes } = useQuery({ queryKey: ["clientes"], queryFn: () => api<Cliente[]>("/api/v1/clientes") });

  const criar = useMutation({
    mutationFn: () => api<Cliente>("/api/v1/clientes", { method: "POST", body: JSON.stringify({ nome, cnpj, contato }) }),
    onSuccess: () => { setNome(""); setCnpj(""); setContato(""); setErro(null); qc.invalidateQueries({ queryKey: ["clientes"] }); },
    onError: (e) => setErro((e as Error).message),
  });

  const excluir = useMutation({
    mutationFn: (id: string) => api(`/api/v1/clientes/${id}`, { method: "DELETE" }),
    onSuccess: () => { setErro(null); qc.invalidateQueries({ queryKey: ["clientes"] }); },
    onError: (e) => setErro((e as Error).message),
  });

  return (
    <div className="space-y-6">
      <form
        onSubmit={(e) => { e.preventDefault(); criar.mutate(); }}
        className="rounded-xl bg-slate-800 p-4 space-y-3"
      >
        <h2 className="font-semibold">Novo cliente</h2>
        <div className="grid gap-3 sm:grid-cols-3">
          <input className="rounded-lg bg-slate-900 px-3 py-2" placeholder="Nome *" value={nome} onChange={(e) => setNome(e.target.value)} required />
          <input className="rounded-lg bg-slate-900 px-3 py-2" placeholder="CNPJ" value={cnpj} onChange={(e) => setCnpj(e.target.value)} />
          <input className="rounded-lg bg-slate-900 px-3 py-2" placeholder="Contato" value={contato} onChange={(e) => setContato(e.target.value)} />
        </div>
        {erro && <p className="text-sm text-red-400">{erro}</p>}
        <button disabled={criar.isPending} className="rounded-lg bg-sky-600 px-4 py-2 font-semibold hover:bg-sky-500 disabled:opacity-50">
          {criar.isPending ? "Salvando..." : "Adicionar"}
        </button>
      </form>

      <ul className="space-y-2">
        {clientes?.map((c) => (
          <li key={c.id} className="rounded-lg bg-slate-800 px-4 py-3 flex items-center justify-between gap-3">
            <div>
              <p className="font-medium">{c.nome}</p>
              <p className="text-sm text-slate-400">{c.cnpj ?? "sem CNPJ"} · {c.contato ?? "—"}</p>
            </div>
            {podeExcluir && (
              <button
                onClick={() => { if (confirm(`Excluir o cliente "${c.nome}"?`)) excluir.mutate(c.id); }}
                disabled={excluir.isPending}
                className="shrink-0 rounded-lg bg-red-600/80 px-3 py-1.5 text-sm font-medium hover:bg-red-600 disabled:opacity-50"
              >
                Excluir
              </button>
            )}
          </li>
        ))}
        {clientes?.length === 0 && <p className="text-slate-400 text-sm">Nenhum cliente ainda.</p>}
      </ul>
    </div>
  );
}
