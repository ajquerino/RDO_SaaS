import { create } from "zustand";

export type Usuario = { id: string; nome: string; email: string | null; funcao: string };

// Regras de acesso (front). O backend também valida.
export const podeGerirObras = (f?: string) => f === "Planejador" || f === "Gestor" || f === "Admin";
export const podeGerirUsuarios = (f?: string) => f === "Gestor" || f === "Admin";
export const podeExcluirClientes = (f?: string) => f === "Planejador" || f === "Gestor" || f === "Admin";
export const podeVerValores = (f?: string) => f === "Planejador" || f === "Gestor" || f === "Admin";

// Modo suporte: super-admin "acessando como" uma empresa (só a empresa em foco).
export type Suporte = { empresaNome: string } | null;

/** Decodifica o payload de um JWT (sem validar assinatura — só p/ ler claims no front). */
export function decodeJwt(token?: string | null): Record<string, any> {
  if (!token) return {};
  const parts = token.split(".");
  if (parts.length !== 3) return {};
  try {
    const s = parts[1].replace(/-/g, "+").replace(/_/g, "/");
    return JSON.parse(decodeURIComponent(escape(atob(s))));
  } catch {
    return {};
  }
}

type AuthState = {
  usuario: Usuario | null;
  suporte: Suporte;
  login: (accessToken: string, refreshToken: string, usuario: Usuario) => void;
  logout: () => void;
  // Entra no modo suporte com o token emitido pelo /plataforma/tenants/{id}/acessar.
  entrarSuporte: (supportToken: string, empresaNome: string) => void;
  // Sai do modo suporte, restaura a sessão do super-admin e volta ao Console.
  sairSuporte: () => void;
};

const usuarioSalvo = (): Usuario | null => {
  const raw = localStorage.getItem("usuario");
  return raw ? (JSON.parse(raw) as Usuario) : null;
};

const suporteSalvo = (): Suporte => {
  const raw = sessionStorage.getItem("suporte");
  return raw ? (JSON.parse(raw) as Suporte) : null;
};

export const useAuth = create<AuthState>((set) => ({
  usuario: usuarioSalvo(),
  suporte: suporteSalvo(),
  login: (accessToken, refreshToken, usuario) => {
    localStorage.setItem("accessToken", accessToken);
    localStorage.setItem("refreshToken", refreshToken);
    localStorage.setItem("usuario", JSON.stringify(usuario));
    set({ usuario });
  },
  logout: () => {
    localStorage.clear();
    sessionStorage.removeItem("suporte");
    sessionStorage.removeItem("superSessao");
    set({ usuario: null, suporte: null });
  },
  entrarSuporte: (supportToken, empresaNome) => {
    // Guarda a sessão atual do super-admin para restaurar ao sair do modo suporte.
    sessionStorage.setItem("superSessao", JSON.stringify({
      accessToken: localStorage.getItem("accessToken"),
      refreshToken: localStorage.getItem("refreshToken"),
      usuario: localStorage.getItem("usuario"),
    }));
    // O usuário efetivo passa a ser o admin da empresa (lido das claims do token de suporte).
    const p = decodeJwt(supportToken);
    const usuario: Usuario = {
      id: p.sub ?? "",
      nome: p.name ?? p.unique_name ?? "Admin",
      email: p.email ?? null,
      funcao: p.funcao ?? "Admin",
    };
    localStorage.setItem("accessToken", supportToken);
    localStorage.removeItem("refreshToken"); // token de suporte é curto (1h) e não tem refresh
    localStorage.setItem("usuario", JSON.stringify(usuario));
    const suporte: Suporte = { empresaNome };
    sessionStorage.setItem("suporte", JSON.stringify(suporte));
    set({ usuario, suporte });
  },
  sairSuporte: () => {
    const raw = sessionStorage.getItem("superSessao");
    sessionStorage.removeItem("superSessao");
    sessionStorage.removeItem("suporte");
    if (raw) {
      const s = JSON.parse(raw) as { accessToken?: string | null; refreshToken?: string | null; usuario?: string | null };
      if (s.accessToken) localStorage.setItem("accessToken", s.accessToken); else localStorage.removeItem("accessToken");
      if (s.refreshToken) localStorage.setItem("refreshToken", s.refreshToken); else localStorage.removeItem("refreshToken");
      if (s.usuario) localStorage.setItem("usuario", s.usuario); else localStorage.removeItem("usuario");
    }
    set({ usuario: usuarioSalvo(), suporte: null });
    // Recarrega na raiz para o Console montar limpo (zera caches do React Query do tenant).
    if (location.pathname !== "/") location.assign("/"); else location.reload();
  },
}));
