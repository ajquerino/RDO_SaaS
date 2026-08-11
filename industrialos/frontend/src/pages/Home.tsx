import { useEffect, useState } from "react";
import { useAuth, podeGerirObras, podeGerirUsuarios } from "../store/auth";
import Obras from "./Obras";
import Clientes from "./Clientes";
import Usuarios from "./Usuarios";
import Equipamentos from "./Equipamentos";
import Auditoria from "./Auditoria";
import BannerAssinatura from "./BannerAssinatura";
import Configuracoes from "./Configuracoes";
import StatusOffline from "./StatusOffline";
import GuiaUso from "./GuiaUso";

type Aba = "obras" | "clientes" | "equipamentos" | "usuarios" | "auditoria" | "config";

export default function Home() {
  const { usuario, logout } = useAuth();
  const gereObras = podeGerirObras(usuario?.funcao);
  const gereUsuarios = podeGerirUsuarios(usuario?.funcao);
  const ehAdmin = usuario?.funcao === "Admin"; // Admin do tenant (o super-admin usa o Console de Plataforma)
  const [aba, setAba] = useState<Aba>("obras");
  const [guiaAberto, setGuiaAberto] = useState(false);
  const [menuAberto, setMenuAberto] = useState(false); // drawer de navegação no mobile
  const [resetKey, setResetKey] = useState(0);         // muda para remontar o conteúdo (colapsa obra/RDO aberto)

  const abas: { id: Aba; rotulo: string; visivel: boolean }[] = [
    { id: "obras", rotulo: "Obras", visivel: true },
    { id: "clientes", rotulo: "Clientes", visivel: gereObras },
    { id: "equipamentos", rotulo: "Equipamentos", visivel: gereObras },
    { id: "usuarios", rotulo: "Usuários", visivel: gereUsuarios },
    { id: "auditoria", rotulo: "Auditoria", visivel: ehAdmin },
    { id: "config", rotulo: "Configurações", visivel: gereUsuarios },
  ];
  const abasVisiveis = abas.filter((a) => a.visivel);
  const rotuloAtual = abasVisiveis.find((a) => a.id === aba)?.rotulo ?? "";

  // Fecha o drawer no ESC.
  useEffect(() => {
    if (!menuAberto) return;
    const onKey = (e: KeyboardEvent) => { if (e.key === "Escape") setMenuAberto(false); };
    window.addEventListener("keydown", onKey);
    return () => window.removeEventListener("keydown", onKey);
  }, [menuAberto]);

  const selecionar = (id: Aba) => { setAba(id); setMenuAberto(false); };
  // Home (🏠): volta pro início (aba Obras) e REMONTA o conteúdo (resetKey) — colapsa obra/RDO aberto.
  const irParaHome = () => { setAba("obras"); setMenuAberto(false); setResetKey((k) => k + 1); };

  return (
    <main className="min-h-screen bg-slate-900 text-slate-100">
      <StatusOffline />
      <header className="flex items-center justify-between gap-2 border-b border-slate-800 px-4 py-3">
        <div className="flex min-w-0 items-center gap-2">
          {/* Hambúrguer só no mobile */}
          <button onClick={() => setMenuAberto(true)} aria-label="Abrir menu"
            className="flex-none rounded-lg bg-slate-800 px-3 py-1.5 text-sm hover:bg-slate-700 md:hidden">☰</button>
          <div className="min-w-0">
            <h1 className="truncate text-lg font-bold">Montaris</h1>
            <p className="truncate text-xs text-slate-400">Olá, {usuario?.nome} ({usuario?.funcao})</p>
          </div>
        </div>
        <div className="flex flex-none items-center gap-2">
          <button onClick={irParaHome} title="Início" className="rounded-lg bg-slate-800 px-3 py-1.5 text-sm hover:bg-slate-700">🏠<span className="hidden sm:inline"> Início</span></button>
          <button onClick={() => setGuiaAberto(true)} title="Guia de uso" className="rounded-lg bg-slate-800 px-3 py-1.5 text-sm hover:bg-slate-700">❓<span className="hidden sm:inline"> Ajuda</span></button>
          <button onClick={logout} className="rounded-lg bg-slate-800 px-3 py-1.5 text-sm">Sair</button>
        </div>
      </header>

      {guiaAberto && <GuiaUso onFechar={() => setGuiaAberto(false)} />}

      <BannerAssinatura />

      {/* Abas horizontais — só no desktop (md+). Preserva o layout atual. */}
      <nav className="hidden gap-1 border-b border-slate-800 px-4 md:flex">
        {abasVisiveis.map((a) => (
          <button
            key={a.id}
            onClick={() => setAba(a.id)}
            className={`px-4 py-2 text-sm border-b-2 -mb-px ${aba === a.id ? "border-sky-500 text-white" : "border-transparent text-slate-400"}`}
          >
            {a.rotulo}
          </button>
        ))}
      </nav>

      {/* Indicador da aba atual — só no mobile (a navegação vive no drawer). */}
      <div className="flex items-center gap-2 border-b border-slate-800 px-4 py-2 text-sm md:hidden">
        <span className="text-slate-500">Seção:</span>
        <span className="font-medium text-slate-200">{rotuloAtual}</span>
      </div>

      {/* Drawer de navegação — só no mobile. */}
      {menuAberto && (
        <div className="fixed inset-0 z-[70] md:hidden">
          <div className="absolute inset-0 bg-black/50" onClick={() => setMenuAberto(false)} aria-hidden />
          <div className="absolute inset-y-0 left-0 flex w-4/5 max-w-xs flex-col overflow-y-auto bg-slate-900 shadow-xl ring-1 ring-slate-800">
            <div className="flex items-center justify-between border-b border-slate-800 px-4 py-3">
              <span className="font-bold">Menu</span>
              <button onClick={() => setMenuAberto(false)} aria-label="Fechar menu" className="rounded-lg bg-slate-800 px-3 py-1.5 text-sm hover:bg-slate-700">✕</button>
            </div>
            <nav className="flex flex-col gap-1 p-2">
              {abasVisiveis.map((a) => (
                <button
                  key={a.id}
                  onClick={() => selecionar(a.id)}
                  className={`rounded-lg px-3 py-2.5 text-left text-sm ${aba === a.id ? "bg-sky-600 text-white" : "text-slate-300 hover:bg-slate-800"}`}
                >
                  {a.rotulo}
                </button>
              ))}
            </nav>
          </div>
        </div>
      )}

      <section key={resetKey} className="mx-auto w-full max-w-3xl p-4">
        {aba === "obras" && <Obras />}
        {aba === "clientes" && gereObras && <Clientes />}
        {aba === "equipamentos" && gereObras && <Equipamentos />}
        {aba === "usuarios" && gereUsuarios && <Usuarios />}
        {aba === "auditoria" && ehAdmin && <Auditoria />}
        {aba === "config" && gereUsuarios && <Configuracoes />}
      </section>
    </main>
  );
}
