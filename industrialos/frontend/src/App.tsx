import { useAuth } from "./store/auth";
import Login from "./pages/Login";
import Home from "./pages/Home";

// Guarda de rota simples: sem router por enquanto (Sprint 1).
export default function App() {
  const usuario = useAuth((s) => s.usuario);
  return usuario ? <Home /> : <Login />;
}
