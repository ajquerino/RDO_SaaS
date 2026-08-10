import { useState } from "react";
import { api, apiPublico } from "../lib/api";
import { useAuth, type Usuario } from "../store/auth";

type LoginResp = {
  accessToken: string;
  refreshToken: string;
  expiraEm: string;
  usuario: Usuario;
};

// Benefícios curtos (marketing) mostrados abaixo do card no celular. Só ícone + título + 1 linha.
const BENEFICIOS = [
  { icone: "📋", titulo: "RDO digital", texto: "Efetivo, HH, fotos e assinatura — até offline no canteiro." },
  { icone: "📊", titulo: "Dashboards e farol", texto: "Avanço, Curva S e produtividade em tempo real." },
  { icone: "💰", titulo: "Medição e faturamento", texto: "Do boletim ao recebimento, num só lugar." },
];

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

  const inputCls =
    "w-full rounded-lg bg-slate-900 px-4 py-3 text-base outline-none ring-1 ring-slate-700 focus:ring-2 focus:ring-sky-500";
  const botaoCls =
    "w-full rounded-lg bg-sky-600 py-3.5 text-base font-semibold hover:bg-sky-500 active:bg-sky-600 disabled:opacity-50";

  return (
    // Mobile-first: uma coluna centralizada; no desktop só cresce o max-width e centraliza com margem.
    <main className="min-h-screen bg-gradient-to-b from-slate-900 to-slate-950 text-slate-100">
      <div className="mx-auto flex min-h-screen w-full max-w-md flex-col justify-center gap-6 px-5 py-10">

        {/* 1. Marca + frase de valor */}
        <header className="text-center">
          <div className="inline-flex items-center gap-2 text-2xl font-bold tracking-tight">
            <span aria-hidden>🏗️</span>
            <span>Industrial<span className="text-sky-400">OS</span></span>
          </div>
          <p className="mx-auto mt-2 max-w-xs text-sm leading-relaxed text-slate-400">
            Gestão de obras industriais no campo — RDO digital, medição e faturamento num só lugar.
          </p>
          <a href="/conheca" className="mt-2 inline-block text-xs font-medium text-sky-400 hover:text-sky-300">
            Conheça o IndustrialOS →
          </a>
        </header>

        {/* 2. Card de login / recuperar senha */}
        <div className="rounded-2xl bg-slate-800 p-5 shadow-xl ring-1 ring-slate-700/50 sm:p-6">
          {modo === "login" ? (
            <form onSubmit={entrar} className="space-y-4">
              <p className="text-sm font-medium text-slate-300">Acesse sua conta</p>
              <input
                className={inputCls}
                placeholder="E-mail ou nome"
                value={emailOuNome}
                onChange={(e) => setEmailOuNome(e.target.value)}
                autoComplete="username"
              />
              <input
                type="password"
                className={inputCls}
                placeholder="Senha"
                value={senha}
                onChange={(e) => setSenha(e.target.value)}
                autoComplete="current-password"
              />
              {erro && (
                <p className="rounded-lg bg-red-500/10 px-3 py-2 text-sm text-red-400" role="alert">{erro}</p>
              )}
              <button disabled={carregando} className={botaoCls}>
                {carregando ? "Entrando..." : "Entrar"}
              </button>
              <button
                type="button"
                onClick={() => { setModo("esqueci"); setErro(null); setMsgEsq(null); }}
                className="w-full py-1 text-center text-sm text-slate-400 hover:text-slate-200"
              >
                Esqueci minha senha
              </button>
            </form>
          ) : (
            <form onSubmit={esqueci} className="space-y-4">
              <div>
                <p className="text-sm font-medium text-slate-300">Recuperar senha</p>
                <p className="mt-0.5 text-xs text-slate-500">Enviaremos um link para o seu e-mail.</p>
              </div>
              <input
                type="email"
                className={inputCls}
                placeholder="Seu e-mail"
                value={emailEsq}
                onChange={(e) => setEmailEsq(e.target.value)}
                autoComplete="email"
              />
              {msgEsq && (
                <p className="rounded-lg bg-emerald-500/10 px-3 py-2 text-sm text-emerald-400">{msgEsq}</p>
              )}
              <button disabled={carregando} className={botaoCls}>
                {carregando ? "Enviando..." : "Enviar instruções"}
              </button>
              <button
                type="button"
                onClick={() => { setModo("login"); setMsgEsq(null); }}
                className="w-full py-1 text-center text-sm text-slate-400 hover:text-slate-200"
              >
                Voltar ao login
              </button>
            </form>
          )}
        </div>

        {/* 3. Criar conta grátis (destaque) — só no modo login */}
        {modo === "login" && (
          <a
            href="/criar-conta"
            className="flex items-center justify-center gap-2 rounded-xl border border-sky-500/40 bg-sky-500/10 py-3.5 text-center font-semibold text-sky-300 hover:bg-sky-500/20"
          >
            Criar conta grátis
            <span className="rounded-full bg-emerald-500/20 px-2 py-0.5 text-xs font-medium text-emerald-300">
              14 dias grátis
            </span>
          </a>
        )}

        {/* 4. Benefícios curtos */}
        <ul className="space-y-2.5">
          {BENEFICIOS.map((b) => (
            <li key={b.titulo} className="flex items-start gap-3 rounded-xl bg-slate-800/50 p-3 ring-1 ring-slate-700/40">
              <span className="text-xl leading-none" aria-hidden>{b.icone}</span>
              <div>
                <p className="text-sm font-medium">{b.titulo}</p>
                <p className="text-xs leading-snug text-slate-400">{b.texto}</p>
              </div>
            </li>
          ))}
        </ul>

        {/* 5. Rodapé discreto */}
        <footer className="flex flex-wrap items-center justify-center gap-x-3 gap-y-1 text-center text-xs text-slate-500">
          <a href="https://wa.me/" target="_blank" rel="noopener noreferrer" className="hover:text-slate-300">
            Fale conosco (WhatsApp)
          </a>
          <span aria-hidden>·</span>
          <a href="#" className="hover:text-slate-300">Termos</a>
          <span aria-hidden>·</span>
          <a href="#" className="hover:text-slate-300">Privacidade</a>
        </footer>
      </div>
    </main>
  );
}
