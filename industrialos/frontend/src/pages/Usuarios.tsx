import { useQuery } from "@tanstack/react-query";
import { api } from "../lib/api";
import type { Usuario } from "../store/auth";

export default function Usuarios() {
  const { data, error } = useQuery({ queryKey: ["usuarios"], queryFn: () => api<Usuario[]>("/api/v1/usuarios") });
  return (
    <div>
      {error && <p className="text-red-400 text-sm">{(error as Error).message}</p>}
      <ul className="space-y-2">
        {data?.map((u) => (
          <li key={u.id} className="rounded-lg bg-slate-800 px-4 py-3 flex justify-between">
            <span>{u.nome}</span>
            <span className="text-slate-400 text-sm">{u.email ?? "—"} · {u.funcao}</span>
          </li>
        ))}
      </ul>
    </div>
  );
}
