import { useState } from "react";
import { apiPublico } from "../lib/api";

/** Tela pública /redefinir-senha/:token — cria a nova senha a partir do link do e-mail. */
export default function RedefinirSenha({ token }: { token: string }) {
  const [senha, setSenha] = useState("");
  const [conf, setConf] = useState("");
  const [erro, setErro] = useState<string | null>(null);
  const [ok, setOk] = useState(false);
  const [carregando, setCarregando] = useState(false);

  async function enviar(e: React.FormEvent) {
    e.preventDefault();
    setErro(null);
    if (senha.length < 6) { setErro("A senha precisa ter ao menos 6 caracteres."); return; }
    if (senha !== conf) { setErro("As senhas não conferem."); return; }
    setCarregando(true);
    try {
      await apiPublico("/api/v1/auth/redefinir-senha", { method: "POST", body: JSON.stringify({ token, novaSenha: senha }) });
      setOk(true);
    } catch (err) {
      setErro((err as Error).message);
    } finally {
      setCarregando(false);
    }
  }

  return (
    <main className="min-h-screen bg-slate-900 text-slate-100 flex items-center justify-center p-6">
      <div className="w-full max-w-sm space-y-4">
        <div className="text-center">
          <h1 className="text-2xl font-bold">IndustrialOS</h1>
          <p className="text-slate-400 text-sm">Criar nova senha</p>
        </div>
        {ok ? (
          <div className="space-y-4 text-center">
            <p className="text-emerald-400">Senha redefinida com sucesso.</p>
            <a href="/" className="inline-block rounded-lg bg-sky-600 px-4 py-2 font-semibold hover:bg-sky-500">Ir para o login</a>
          </div>
        ) : (
          <form onSubmit={enviar} className="space-y-4">
            <input type="password" className="w-full rounded-lg bg-slate-800 px-4 py-3 outline-none focus:ring-2 focus:ring-sky-500" placeholder="Nova senha" value={senha} onChange={(e) => setSenha(e.target.value)} autoComplete="new-password" />
            <input type="password" className="w-full rounded-lg bg-slate-800 px-4 py-3 outline-none focus:ring-2 focus:ring-sky-500" placeholder="Confirmar nova senha" value={conf} onChange={(e) => setConf(e.target.value)} autoComplete="new-password" />
            {erro && <p className="text-sm text-red-400">{erro}</p>}
            <button disabled={carregando} className="w-full rounded-lg bg-sky-600 py-3 font-semibold hover:bg-sky-500 disabled:opacity-50">
              {carregando ? "Salvando..." : "Redefinir senha"}
            </button>
            <a href="/" className="block text-center text-sm text-slate-400 hover:text-slate-200">Voltar ao login</a>
          </form>
        )}
      </div>
    </main>
  );
}
