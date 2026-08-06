import { useAuth } from "./store/auth";
import Login from "./pages/Login";
import Home from "./pages/Home";
import Aprovacao from "./pages/Aprovacao";
import PlataformaConsole from "./pages/PlataformaConsole";

// Guarda de rota simples: sem router por enquanto.
// Rota PUBLICA /aprovacao/{token} — o fiscal do cliente aprova sem login.
export default function App() {
  const usuario = useAuth((s) => s.usuario);

  const mAprov = window.location.pathname.match(/^\/aprovacao\/([^/]+)$/);
  if (mAprov) return <Aprovacao token={mAprov[1]} />;

  if (!usuario) return <Login />;
  // Super-admin (dono do SaaS) usa o Console de Plataforma — não as telas de tenant.
  if (usuario.funcao === "SuperAdmin") return <PlataformaConsole />;
  return <Home />;
}
