import { useAuth } from "./store/auth";
import Login from "./pages/Login";
import Home from "./pages/Home";
import Aprovacao from "./pages/Aprovacao";
import RedefinirSenha from "./pages/RedefinirSenha";
import CriarConta from "./pages/CriarConta";
import PlataformaConsole from "./pages/PlataformaConsole";

// Guarda de rota simples: sem router por enquanto.
// Rotas PUBLICAS: /aprovacao/{token} (fiscal aprova) e /redefinir-senha/{token}.
export default function App() {
  const usuario = useAuth((s) => s.usuario);

  const mAprov = window.location.pathname.match(/^\/aprovacao\/([^/]+)$/);
  if (mAprov) return <Aprovacao token={mAprov[1]} />;

  const mReset = window.location.pathname.match(/^\/redefinir-senha\/([^/]+)$/);
  if (mReset) return <RedefinirSenha token={mReset[1]} />;

  // Autocadastro público (só quando não logado).
  if (!usuario && window.location.pathname === "/criar-conta") return <CriarConta />;

  if (!usuario) return <Login />;
  // Super-admin (dono do SaaS) usa o Console de Plataforma — não as telas de tenant.
  if (usuario.funcao === "SuperAdmin") return <PlataformaConsole />;
  return <Home />;
}
