import { useState } from "react";
import { apiPublico } from "../lib/api";
import { useAuth, type Usuario } from "../store/auth";

type Resp = { accessToken: string; refreshToken: string; expiraEm: string; usuario: Usuario };

/** Autocadastro público (/criar-conta): cria empresa + admin e já entra no app. */
export default function CriarConta() {
  const login = useAuth((s) => s.login);
  const [nomeEmpresa, setNomeEmpresa] = useState("");
  const [cnpj, setCnpj] = useState("");
  const [adminNome, setAdminNome] = useState("");
  const [adminEmail, setAdminEmail] = useState("");
  const [adminSenha, setAdminSenha] = useState("");
  const [erro, setErro] = useState<string | null>(null);
  const [carregando, setCarregando] = useState(false);

  async function criar(e: React.FormEvent) {
    e.preventDefault();
    setErro(null);
    if (adminSenha.length < 6) { setErro("A senha precisa ter ao menos 6 caracteres."); return; }
    setCarregando(true);
    try {
      const r = await apiPublico<Resp>("/api/v1/signup", {
        method: "POST",
        body: JSON.stringify({ nomeEmpresa, cnpj: cnpj || null, adminNome, adminEmail, adminSenha }),
      });
      login(r.accessToken, r.refreshToken, r.usuario);
      location.assign("/"); // entra direto no app
    } catch (err) {
      setErro((err as Error).message);
    } finally {
      setCarregando(false);
    }
  }

  const inp = "w-full rounded-lg bg-slate-800 px-4 py-3 outline-none focus:ring-2 focus:ring-sky-500";
  return (
    <main className="min-h-screen bg-slate-900 text-slate-100 flex items-center justify-center p-6">
      <form onSubmit={criar} className="w-full max-w-sm space-y-4">
        <div className="text-center">
          <h1 className="text-2xl font-bold">Criar conta</h1>
          <p className="text-slate-400 text-sm">14 dias grátis · sem cartão</p>
        </div>
        <input className={inp} placeholder="Nome da empresa *" value={nomeEmpresa} onChange={(e) => setNomeEmpresa(e.target.value)} required />
        <input className={inp} placeholder="CNPJ (opcional)" value={cnpj} onChange={(e) => setCnpj(e.target.value)} />
        <input className={inp} placeholder="Seu nome *" value={adminNome} onChange={(e) => setAdminNome(e.target.value)} required />
        <input type="email" className={inp} placeholder="Seu e-mail *" value={adminEmail} onChange={(e) => setAdminEmail(e.target.value)} autoComplete="email" required />
        <input type="password" className={inp} placeholder="Senha (mín. 6) *" value={adminSenha} onChange={(e) => setAdminSenha(e.target.value)} autoComplete="new-password" required />
        {erro && <p className="text-sm text-red-400">{erro}</p>}
        <button disabled={carregando} className="w-full rounded-lg bg-sky-600 py-3 font-semibold hover:bg-sky-500 disabled:opacity-50">
          {carregando ? "Criando…" : "Criar conta grátis"}
        </button>
        <a href="/" className="block text-center text-sm text-slate-400 hover:text-slate-200">Já tenho conta — entrar</a>
      </form>
    </main>
  );
}
