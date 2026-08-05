import { create } from "zustand";

export type Usuario = { id: string; nome: string; email: string | null; funcao: string };

// Regras de acesso (front). O backend também valida.
export const podeGerirObras = (f?: string) => f === "Planejador" || f === "Gestor" || f === "Admin";
export const podeGerirUsuarios = (f?: string) => f === "Gestor" || f === "Admin";
export const podeVerValores = (f?: string) => f === "Planejador" || f === "Gestor" || f === "Admin";

type AuthState = {
  usuario: Usuario | null;
  login: (accessToken: string, refreshToken: string, usuario: Usuario) => void;
  logout: () => void;
};

const usuarioSalvo = (): Usuario | null => {
  const raw = localStorage.getItem("usuario");
  return raw ? (JSON.parse(raw) as Usuario) : null;
};

export const useAuth = create<AuthState>((set) => ({
  usuario: usuarioSalvo(),
  login: (accessToken, refreshToken, usuario) => {
    localStorage.setItem("accessToken", accessToken);
    localStorage.setItem("refreshToken", refreshToken);
    localStorage.setItem("usuario", JSON.stringify(usuario));
    set({ usuario });
  },
  logout: () => {
    localStorage.clear();
    set({ usuario: null });
  },
}));
