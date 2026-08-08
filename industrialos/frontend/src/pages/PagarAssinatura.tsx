import { useEffect, useRef, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { api, type AssinaturaStatus, type CobrancaPix } from "../lib/api";

const brl = (v?: number | null) => (v == null ? "" : v.toLocaleString("pt-BR", { style: "currency", currency: "BRL" }));

/** Painel de pagamento por PIX: gera a cobrança, mostra QR + copia-e-cola e faz polling do status
 *  até o pagamento ser confirmado (acesso liberado automaticamente). Funciona mesmo com a empresa
 *  bloqueada — /assinatura é rota livre. */
export default function PagarAssinatura({ onClose }: { onClose: () => void }) {
  const qc = useQueryClient();
  const [copiado, setCopiado] = useState(false);
  const [confirmado, setConfirmado] = useState(false);
  const fechouRef = useRef(false);

  const cobrar = useMutation({
    mutationFn: () => api<CobrancaPix>("/api/v1/assinatura/cobrar", { method: "POST" }),
  });

  // Dispara a cobrança ao abrir o painel.
  useEffect(() => { cobrar.mutate(); /* eslint-disable-next-line react-hooks/exhaustive-deps */ }, []);

  // Polling do status a cada 5s enquanto o painel está aberto (para quando confirmar).
  const { data: status } = useQuery({
    queryKey: ["assinatura-minha"],
    queryFn: () => api<AssinaturaStatus>("/api/v1/assinatura/minha"),
    refetchInterval: confirmado ? false : 5000,
  });

  // Pagamento empurra o vencimento +1 mês => estado volta para "EmDia" e bloqueada=false.
  useEffect(() => {
    if (!status || confirmado) return;
    if (status.estado === "EmDia" && !status.bloqueada) {
      setConfirmado(true);
      qc.invalidateQueries({ queryKey: ["assinatura-minha"] });
      if (!fechouRef.current) { fechouRef.current = true; setTimeout(onClose, 1800); }
    }
  }, [status, confirmado, qc, onClose]);

  const copiar = async () => {
    const code = cobrar.data?.brCode;
    if (!code) return;
    try { await navigator.clipboard.writeText(code); setCopiado(true); setTimeout(() => setCopiado(false), 2000); }
    catch { /* clipboard indisponível — o usuário copia manualmente do campo */ }
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 p-4" onClick={onClose}>
      <div className="w-full max-w-sm rounded-2xl bg-slate-800 p-5 text-slate-100 shadow-xl" onClick={(e) => e.stopPropagation()}>
        <div className="mb-3 flex items-center justify-between">
          <h2 className="text-lg font-semibold">Pagar assinatura (PIX)</h2>
          <button onClick={onClose} className="text-slate-400 hover:text-slate-200" aria-label="Fechar">✕</button>
        </div>

        {confirmado ? (
          <div className="py-8 text-center">
            <p className="text-3xl">✅</p>
            <p className="mt-2 font-semibold text-emerald-400">Pagamento confirmado!</p>
            <p className="text-sm text-slate-400">Acesso liberado. Fechando…</p>
          </div>
        ) : cobrar.isPending ? (
          <p className="py-8 text-center text-slate-300">Gerando cobrança…</p>
        ) : cobrar.isError ? (
          <div className="space-y-3">
            <p className="text-sm text-red-400">{(cobrar.error as Error).message}</p>
            <button onClick={() => cobrar.mutate()} className="w-full rounded-lg bg-sky-600 py-2 font-semibold hover:bg-sky-500">Tentar de novo</button>
          </div>
        ) : cobrar.data ? (
          <div className="space-y-4">
            {cobrar.data.valor != null && (
              <p className="text-center text-sm text-slate-400">Valor: <span className="font-semibold text-slate-100">{brl(cobrar.data.valor)}</span></p>
            )}
            {cobrar.data.brCodeBase64 && (
              <img src={`data:image/png;base64,${cobrar.data.brCodeBase64}`} alt="QR Code PIX" className="mx-auto h-56 w-56 rounded-lg bg-white p-2" />
            )}
            {cobrar.data.brCode && (
              <div className="flex items-center gap-2">
                <input readOnly value={cobrar.data.brCode} className="min-w-0 flex-1 truncate rounded-lg bg-slate-900 px-3 py-2 text-xs" />
                <button onClick={copiar} className="shrink-0 rounded-lg bg-sky-600 px-3 py-2 text-sm font-semibold hover:bg-sky-500">{copiado ? "Copiado!" : "Copiar"}</button>
              </div>
            )}
            <p className="text-center text-xs text-slate-400">Escaneie no app do banco ou copie o código. Após o pagamento, o acesso é liberado automaticamente.</p>
            <p className="text-center text-xs text-slate-500">Aguardando pagamento…</p>
          </div>
        ) : null}
      </div>
    </div>
  );
}
