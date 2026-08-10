import { useAuth } from "./store/auth";
import Login from "./pages/Login";
import Home from "./pages/Home";
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
  // Super-admin (dono do SaaS) usa o Console de Plataforma — não as telas de tenant.
  if (usuario.funcao === "SuperAdmin") return <PlataformaConsole />;
  // Demais papéis => telas de tenant. Em modo suporte, o banner fixo fica por cima de qualquer tela.
  return <><SuporteBanner /><Home /></>;
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
