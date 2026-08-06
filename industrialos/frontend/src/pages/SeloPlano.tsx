import { useQuery } from "@tanstack/react-query";
import { api, type UsoPlano } from "../lib/api";

/** Selo discreto de uso vs limite do plano (obras/usuários). Amarelo quando atinge o limite.
 * É só informativo — nada é bloqueado. Sem plano/limite, mostra só a contagem. */
export default function SeloPlano() {
  const { data } = useQuery({ queryKey: ["uso-plano"], queryFn: () => api<UsoPlano>("/api/v1/uso-plano") });
  if (!data) return null;

  const parte = (n: number, lim?: number | null) => (
    <span className={lim != null && n >= lim ? "text-amber-400 font-medium" : ""}>
      {n}{lim != null ? `/${lim}` : ""}
    </span>
  );

  return (
    <div className="rounded-lg bg-slate-800/60 px-3 py-1.5 text-xs text-slate-400">
      {data.plano ? <span className="text-slate-500">Plano {data.plano} · </span> : null}
      Obras: {parte(data.nObras, data.limiteObras)} · Usuários: {parte(data.nUsuarios, data.limiteUsuarios)}
    </div>
  );
}
