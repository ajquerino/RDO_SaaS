import { useQuery } from "@tanstack/react-query";
import { api, type AssinaturaStatus } from "./api";

/** Estado da assinatura do tenant atual (para banner + modo somente-leitura).
 *  O servidor é quem realmente bloqueia (402); aqui é só experiência de uso. */
export function useAssinatura() {
  const { data } = useQuery({
    queryKey: ["assinatura-minha"],
    queryFn: () => api<AssinaturaStatus>("/api/v1/assinatura/minha"),
    staleTime: 60_000,
  });
  return {
    status: data,
    estado: data?.estado,
    bloqueada: data?.bloqueada ?? false,
    diasParaVencer: data?.diasParaVencer ?? null,
    diasAtraso: data?.diasAtraso ?? null,
    avisoNivel: data?.avisoNivel ?? null,
    vencimentoEm: data?.vencimentoEm ?? null,
    planoNome: data?.planoNome ?? null,
  };
}
