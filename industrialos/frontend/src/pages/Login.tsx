import { useState } from "react";
import { api, apiPublico } from "../lib/api";
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
  // Mostra (uma vez) a mensagem de "sessão encerrada em outro dispositivo", se o api.ts a deixou.
  const [erro, setErro] = useState<string | null>(() => {
    const m = localStorage.getItem("authMsg");
    if (m) { localStorage.removeItem("authMsg"); return m; }
    return null;
  });
  const [carregando, setCarregando] = useState(false);
  const [modo, setModo] = useState<"login" | "esqueci">("login");
  const [emailEsq, setEmailEsq] = useState("");
  const [msgEsq, setMsgEsq] = useState<string | null>(null);

  async function esqueci(e: React.FormEvent) {
    e.preventDefault();
    setMsgEsq(null);
    setCarregando(true);
    try {
      const r = await apiPublico<{ mensagem: string }>("/api/v1/auth/esqueci-senha", {
        method: "POST",
        body: JSON.stringify({ email: emailEsq }),
      });
      setMsgEsq(r.mensagem ?? "Se o e-mail existir, enviamos as instruções.");
    } catch {
      // Mensagem neutra mesmo em erro — nunca revela se o e-mail existe.
      setMsgEsq("Se o e-mail existir, enviamos as instruções.");
    } finally {
      setCarregando(false);
    }
  }

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
      {modo === "login" ? (
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
          <button
            type="button"
            onClick={() => { setModo("esqueci"); setErro(null); setMsgEsq(null); }}
            className="w-full text-center text-sm text-slate-400 hover:text-slate-200"
          >
            Esqueci minha senha
          </button>
          <a href="/criar-conta" className="block text-center text-sm text-sky-400 hover:text-sky-300">
            Criar conta grátis
          </a>
        </form>
      ) : (
        <form onSubmit={esqueci} className="w-full max-w-sm space-y-4">
          <div className="text-center">
            <h1 className="text-2xl font-bold">Recuperar senha</h1>
            <p className="text-slate-400 text-sm">Enviaremos um link para o seu e-mail</p>
          </div>
          <input
            type="email"
            className="w-full rounded-lg bg-slate-800 px-4 py-3 outline-none focus:ring-2 focus:ring-sky-500"
            placeholder="Seu e-mail"
            value={emailEsq}
            onChange={(e) => setEmailEsq(e.target.value)}
            autoComplete="email"
          />
          {msgEsq && <p className="text-sm text-emerald-400">{msgEsq}</p>}
          <button
            disabled={carregando}
            className="w-full rounded-lg bg-sky-600 py-3 font-semibold hover:bg-sky-500 disabled:opacity-50"
          >
            {carregando ? "Enviando..." : "Enviar instruções"}
          </button>
          <button
            type="button"
            onClick={() => { setModo("login"); setMsgEsq(null); }}
            className="w-full text-center text-sm text-slate-400 hover:text-slate-200"
          >
            Voltar ao login
          </button>
        </form>
      )}
    </main>
  );
}
