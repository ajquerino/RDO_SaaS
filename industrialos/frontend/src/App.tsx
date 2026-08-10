import { useEffect, useState } from "react";
import { useAuth } from "./store/auth";
import { api } from "./lib/api";
import Login from "./pages/Login";
import Home from "./pages/Home";
import Rdo from "./pages/Rdo";
import Aprovacao from "./pages/Aprovacao";
import RedefinirSenha from "./pages/RedefinirSenha";
import CriarConta from "./pages/CriarConta";
import Conheca from "./pages/Conheca";
import PlataformaConsole from "./pages/PlataformaConsole";

// Guarda de rota simples: sem router por enquanto.
// Rotas PUBLICAS: /conheca (institucional), /aprovacao/{token} (fiscal aprova) e /redefinir-senha/{token}.
export default function App() {
  const usuario = useAuth((s) => s.usuario);

  // Página institucional pública (acessível logado ou não).
  if (window.location.pathname === "/conheca") return <Conheca />;

  const mAprov = window.location.pathname.match(/^\/aprovacao\/([^/]+)$/);
  if (mAprov) return <Aprovacao token={mAprov[1]} />;

  const mReset = window.location.pathname.match(/^\/redefinir-senha\/([^/]+)$/);
  if (mReset) return <RedefinirSenha token={mReset[1]} />;

  // Autocadastro público (só quando não logado).
  if (!usuario && window.location.pathname === "/criar-conta") return <CriarConta />;

  if (!usuario) return <Login />;

  // Link permanente de um RDO (ex.: vídeos no PDF apontam pra cá). Exige login (cai no <Login/> acima).
  const mRdo = window.location.pathname.match(/^\/rdo\/([^/]+)$/);
  if (mRdo) return <RdoStandalone rdoId={mRdo[1]} />;

  // Super-admin (dono do SaaS) usa o Console de Plataforma — não as telas de tenant.
  if (usuario.funcao === "SuperAdmin") return <PlataformaConsole />;
  // Demais papéis => telas de tenant. Em modo suporte, o banner fixo fica por cima de qualquer tela.
  return <><SuporteBanner /><Home /></>;
}

/** Abre um RDO específico em tela cheia via /rdo/{id}. Descobre o obraId pelo próprio RDO e reaproveita
 * o componente Rdo. "Voltar" leva à Home. */
function RdoStandalone({ rdoId }: { rdoId: string }) {
  const [obraId, setObraId] = useState<string | null>(null);
  const [erro, setErro] = useState<string | null>(null);

  useEffect(() => {
    api<{ obraId: string }>(`/api/v1/rdos/${rdoId}`)
      .then((r) => setObraId(r.obraId))
      .catch((e) => setErro((e as Error).message));
  }, [rdoId]);

  const voltar = () => window.location.assign("/");

  if (erro)
    return (
      <main className="min-h-screen bg-slate-900 p-6 text-slate-100">
        <p className="text-red-400">{erro}</p>
        <button onClick={voltar} className="mt-3 rounded-lg bg-slate-800 px-3 py-1.5 text-sm hover:bg-slate-700">Voltar</button>
      </main>
    );
  if (!obraId) return <main className="min-h-screen bg-slate-900 p-6 text-slate-400">Carregando…</main>;

  return (
    <main className="min-h-screen bg-slate-900 text-slate-100">
      <div className="mx-auto w-full max-w-3xl p-4">
        <Rdo obraId={obraId} rdoId={rdoId} onClose={voltar} />
      </div>
    </main>
  );
}

/** Barra fixa no topo quando o super-admin está "acessando como" uma empresa (modo suporte). */
function SuporteBanner() {
  const suporte = useAuth((s) => s.suporte);
  const sairSuporte = useAuth((s) => s.sairSuporte);
  if (!suporte) return null;
  return (
    <>
      <div className="fixed inset-x-0 top-0 z-[60] flex items-center justify-center gap-3 bg-amber-500 px-4 py-1.5 text-sm font-medium text-amber-950 shadow">
        <span>Modo suporte — <strong>{suporte.empresaNome}</strong></span>
        <button onClick={sairSuporte} className="rounded bg-amber-950/20 px-2 py-0.5 text-xs font-semibold hover:bg-amber-950/30">
          Sair do modo suporte
        </button>
      </div>
      <div className="h-9" aria-hidden /> {/* espaçador: evita a barra fixa cobrir o topo da tela */}
    </>
  );
}
