import { useEffect, useState, useCallback } from "react";
import { contarPendentes } from "./db";

/** true/false conforme a conexão. Reage aos eventos online/offline do navegador. */
export function useOnline() {
  const [online, setOnline] = useState(navigator.onLine);
  useEffect(() => {
    const on = () => setOnline(true);
    const off = () => setOnline(false);
    window.addEventListener("online", on);
    window.addEventListener("offline", off);
    return () => { window.removeEventListener("online", on); window.removeEventListener("offline", off); };
  }, []);
  return online;
}

/** Quantidade de RDOs ainda não sincronizados (badge de "pendentes"). Atualiza em evento custom. */
export function usePendentes() {
  const [n, setN] = useState(0);
  const recarregar = useCallback(() => { contarPendentes().then(setN); }, []);
  useEffect(() => {
    recarregar();
    const h = () => recarregar();
    window.addEventListener("rdo-sync-mudou", h);
    return () => window.removeEventListener("rdo-sync-mudou", h);
  }, [recarregar]);
  return n;
}

/** dispare após criar/salvar/sincronizar um RDO local, pra o badge atualizar. */
export function notificarSyncMudou() {
  window.dispatchEvent(new Event("rdo-sync-mudou"));
}
