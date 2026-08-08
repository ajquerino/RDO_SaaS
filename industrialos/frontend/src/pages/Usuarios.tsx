import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { api } from "../lib/api";
import { useAuth, podeGerirUsuarios, type Usuario } from "../store/auth";
import SeloPlano from "./SeloPlano";

// Funções que um gestor/admin pode atribuir — sem SuperAdmin (dono do SaaS).
const FUNCOES = ["Encarregado", "Lider", "Supervisor", "Planejador", "Gestor", "Admin"];

export default function Usuarios() {
  const qc = useQueryClient();
  const eu = useAuth((s) => s.usuario);
  const gere = podeGerirUsuarios(eu?.funcao);
  const { data, error } = useQuery({ queryKey: ["usuarios"], queryFn: () => api<Usuario[]>("/api/v1/usuarios") });

  const [nome, setNome] = useState("");
  const [email, setEmail] = useState("");
  const [senha, setSenha] = useState("");
  const [funcao, setFuncao] = useState("Encarregado");
  const [erro, setErro] = useState<string | null>(null);
  const [aviso, setAviso] = useState<string | null>(null);

  const criar = useMutation({
    mutationFn: () =>
      api<{ aviso?: string | null }>("/api/v1/usuarios", {
        method: "POST",
        body: JSON.stringify({ nome, email: email || null, senha, funcao }),
      }),
    onSuccess: (r) => {
      setNome(""); setEmail(""); setSenha(""); setFuncao("Encarregado"); setErro(null);
      setAviso(r.aviso ?? null);
      qc.invalidateQueries({ queryKey: ["usuarios"] });
      qc.invalidateQueries({ queryKey: ["uso-plano"] });
    },
    onError: (e) => setErro((e as Error).message),
  });

  const excluir = useMutation({
    mutationFn: (id: string) => api(`/api/v1/usuarios/${id}`, { method: "DELETE" }),
    onSuccess: () => qc.invalidateQueries({ queryKey: ["usuarios"] }),
    onError: (e) => setErro((e as Error).message),
  });

  return (
    <div className="space-y-4">
      <div className="flex justify-end"><SeloPlano /></div>

      {gere && (
        <form
          onSubmit={(e) => { e.preventDefault(); if (nome && senha) criar.mutate(); }}
          className="space-y-3 rounded-lg bg-slate-800 p-4"
        >
          <h2 className="font-semibold">Novo usuário</h2>
          <div className="grid gap-3 sm:grid-cols-2">
            <input
              className="rounded-lg bg-slate-900 px-3 py-2 outline-none focus:ring-2 focus:ring-sky-500"
              placeholder="Nome *"
              value={nome}
              onChange={(e) => setNome(e.target.value)}
              required
            />
            <input
              type="email"
              className="rounded-lg bg-slate-900 px-3 py-2 outline-none focus:ring-2 focus:ring-sky-500"
              placeholder="E-mail"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              autoComplete="off"
            />
            <input
              type="password"
              className="rounded-lg bg-slate-900 px-3 py-2 outline-none focus:ring-2 focus:ring-sky-500"
              placeholder="Senha *"
              value={senha}
              onChange={(e) => setSenha(e.target.value)}
              autoComplete="new-password"
              required
            />
            <select
              className="rounded-lg bg-slate-900 px-3 py-2 outline-none focus:ring-2 focus:ring-sky-500"
              value={funcao}
              onChange={(e) => setFuncao(e.target.value)}
            >
              {FUNCOES.map((f) => <option key={f} value={f}>{f}</option>)}
            </select>
          </div>
          {erro && <p className="text-sm text-red-400">{erro}</p>}
          <button
            disabled={criar.isPending || !nome || !senha}
            className="rounded-lg bg-sky-600 px-4 py-2 font-semibold hover:bg-sky-500 disabled:opacity-50"
          >
            {criar.isPending ? "Criando…" : "Criar usuário"}
          </button>
        </form>
      )}

      {aviso && (
        <div className="flex items-start justify-between gap-3 rounded-lg border border-amber-500/40 bg-amber-500/10 px-4 py-3 text-sm text-amber-300">
          <span>{aviso}</span>
          <button onClick={() => setAviso(null)} className="shrink-0 text-amber-400 hover:text-amber-200" aria-label="Fechar">✕</button>
        </div>
      )}

      {error && <p className="text-red-400 text-sm">{(error as Error).message}</p>}

      <ul className="space-y-2">
        {data?.map((u) => (
          <li key={u.id} className="flex items-center justify-between gap-3 rounded-lg bg-slate-800 px-4 py-3">
            <span className="min-w-0">
              <span className="font-medium">{u.nome}</span>
              <span className="block truncate text-sm text-slate-400">{u.email ?? "—"} · {u.funcao}</span>
            </span>
            {gere && u.id !== eu?.id && (
              <button
                onClick={() => { if (confirm(`Excluir o usuário "${u.nome}"?`)) excluir.mutate(u.id); }}
                disabled={excluir.isPending}
                className="shrink-0 rounded-lg bg-red-950 px-2 py-1 text-xs text-red-300 hover:bg-red-900 disabled:opacity-50"
              >
                Excluir
              </button>
            )}
          </li>
        ))}
      </ul>
    </div>
  );
}
