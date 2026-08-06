import { useEffect, useState } from "react";
import { assinarStatus, sincronizar, type StatusOffline } from "../lib/offline";

/** Selo fixo (canto inferior) do estado offline: mostra quando está sem rede e
 *  quantas alterações estão na fila para sincronizar. Discreto quando tudo está ok. */
export default function OfflineIndicator() {
  const [st, setSt] = useState<StatusOffline>({ online: navigator.onLine, sincronizando: false, pendentes: 0 });
  useEffect(() => assinarStatus(setSt), []);

  // Online e sem pendências: nada a mostrar.
  if (st.online && st.pendentes === 0 && !st.sincronizando) return null;

  const cor = !st.online ? "bg-amber-600" : "bg-sky-700";
  const texto = !st.online
    ? st.pendentes > 0 ? `Offline · ${st.pendentes} p/ sincronizar` : "Offline"
    : st.sincronizando ? "Sincronizando…" : `${st.pendentes} p/ sincronizar`;

  return (
    <div className={`fixed bottom-3 right-3 z-50 flex items-center gap-2 rounded-full ${cor} px-3 py-1.5 text-xs text-white shadow-lg`}>
      <span className={`inline-block h-2 w-2 rounded-full ${st.online ? "bg-emerald-300" : "bg-amber-200"} ${st.sincronizando ? "animate-pulse" : ""}`} />
      <span>{texto}</span>
      {st.online && st.pendentes > 0 && !st.sincronizando && (
        <button onClick={() => sincronizar()} className="ml-1 rounded bg-white/20 px-2 py-0.5 hover:bg-white/30">agora</button>
      )}
    </div>
  );
}
