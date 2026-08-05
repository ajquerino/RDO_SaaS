import { useState } from "react";
import { api } from "../lib/api";
import { useAuth, type Usuario } from "../store/auth";

type LoginResp = {
  accessToken: string;
  refreshToken: string;
  expiraEm: string;
  usuario: Usuario;
};

export default function Login() {
  const login = useAuth((s) => s.login);
  const [emailOuNome, setEmailOuNome] = useState("admin@demo.com");
  const [senha, setSenha] = useState("");
  const [erro, setErro] = useState<string | null>(null);
  const [carregando, setCarregando] = useState(false);

  async function entrar(e: React.FormEvent) {
    e.preventDefault();
    setErro(null);
    setCarregando(true);
    try {
      const r = await api<LoginResp>("/api/v1/auth/login", {
        method: "POST",
        body: JSON.stringify({ emailOuNome, senha }),
      });
      login(r.accessToken, r.refreshToken, r.usuario);
    } catch (err) {
      setErro((err as Error).message);
    } finally {
      setCarregando(false);
    }
  }

  return (
    <main className="min-h-screen bg-slate-900 text-slate-100 flex items-center justify-center p-6">
      <form onSubmit={entrar} className="w-full max-w-sm space-y-4">
        <div className="text-center">
          <h1 className="text-2xl font-bold">IndustrialOS</h1>
          <p className="text-slate-400 text-sm">Gestão Operacional de Obras</p>
        </div>
        <input
          className="w-full rounded-lg bg-slate-800 px-4 py-3 outline-none focus:ring-2 focus:ring-sky-500"
          placeholder="E-mail ou nome"
          value={emailOuNome}
          onChange={(e) => setEmailOuNome(e.target.value)}
          autoComplete="username"
        />
        <input
          type="password"
          className="w-full rounded-lg bg-slate-800 px-4 py-3 outline-none focus:ring-2 focus:ring-sky-500"
          placeholder="Senha"
          value={senha}
          onChange={(e) => setSenha(e.target.value)}
          autoComplete="current-password"
        />
        {erro && <p className="text-sm text-red-400">{erro}</p>}
        <button
          disabled={carregando}
          className="w-full rounded-lg bg-sky-600 py-3 font-semibold hover:bg-sky-500 disabled:opacity-50"
        >
          {carregando ? "Entrando..." : "Entrar"}
        </button>
      </form>
    </main>
  );
}
