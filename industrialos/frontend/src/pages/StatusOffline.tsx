import { useOnline, usePendentes } from "../lib/useOnline";

/** Faixa fina de status de conexão + pendentes de sincronização. Fica no topo do app. */
export default function StatusOffline() {
  const online = useOnline();
  const pendentes = usePendentes();

  if (online && pendentes === 0) return null;

  return (
    <div className={`px-4 py-1.5 text-center text-xs font-medium ${online ? "bg-sky-900/70 text-sky-200" : "bg-amber-900/80 text-amber-100"}`}>
      {!online && <span>📴 Sem conexão — você pode preencher RDOs; eles sincronizam ao voltar o sinal.</span>}
      {online && pendentes > 0 && <span>🔄 Sincronizando {pendentes} RDO{pendentes > 1 ? "s" : ""} pendente{pendentes > 1 ? "s" : ""}…</span>}
      {!online && pendentes > 0 && <span className="ml-1">({pendentes} pendente{pendentes > 1 ? "s" : ""})</span>}
    </div>
  );
}
