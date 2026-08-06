import { useState } from "react";
import { useAuth, podeGerirObras, podeGerirUsuarios } from "../store/auth";
import Obras from "./Obras";
import Clientes from "./Clientes";
import Usuarios from "./Usuarios";
import Equipamentos from "./Equipamentos";
import Auditoria from "./Auditoria";

type Aba = "obras" | "clientes" | "equipamentos" | "usuarios" | "auditoria";

export default function Home() {
  const { usuario, logout } = useAuth();
  const gereObras = podeGerirObras(usuario?.funcao);
  const gereUsuarios = podeGerirUsuarios(usuario?.funcao);
  const ehAdmin = usuario?.funcao === "Admin"; // Admin do tenant (o super-admin usa o Console de Plataforma)
  const [aba, setAba] = useState<Aba>("obras");

  const abas: { id: Aba; rotulo: string; visivel: boolean }[] = [
    { id: "obras", rotulo: "Obras", visivel: true },
    { id: "clientes", rotulo: "Clientes", visivel: gereObras },
    { id: "equipamentos", rotulo: "Equipamentos", visivel: gereObras },
    { id: "usuarios", rotulo: "Usuários", visivel: gereUsuarios },
    { id: "auditoria", rotulo: "Auditoria", visivel: ehAdmin },
  ];

  return (
    <main className="min-h-screen bg-slate-900 text-slate-100">
      <header className="flex items-center justify-between border-b border-slate-800 px-4 py-3">
        <div>
          <h1 className="text-lg font-bold">IndustrialOS</h1>
          <p className="text-slate-400 text-xs">Olá, {usuario?.nome} ({usuario?.funcao})</p>
        </div>
        <button onClick={logout} className="rounded-lg bg-slate-800 px-3 py-1.5 text-sm">Sair</button>
      </header>

      <nav className="flex gap-1 border-b border-slate-800 px-4">
        {abas.filter((a) => a.visivel).map((a) => (
          <button
            key={a.id}
            onClick={() => setAba(a.id)}
            className={`px-4 py-2 text-sm border-b-2 -mb-px ${aba === a.id ? "border-sky-500 text-white" : "border-transparent text-slate-400"}`}
          >
            {a.rotulo}
          </button>
        ))}
      </nav>

      <section className="mx-auto max-w-3xl p-4">
        {aba === "obras" && <Obras />}
        {aba === "clientes" && gereObras && <Clientes />}
        {aba === "equipamentos" && gereObras && <Equipamentos />}
        {aba === "usuarios" && gereUsuarios && <Usuarios />}
        {aba === "auditoria" && ehAdmin && <Auditoria />}
      </section>
    </main>
  );
}
