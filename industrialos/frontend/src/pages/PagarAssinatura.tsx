import { useEffect, useRef, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { api, type AssinaturaStatus, type CobrancaPix, type Plano } from "../lib/api";

const brl = (v?: number | null) => (v == null ? "" : v.toLocaleString("pt-BR", { style: "currency", currency: "BRL" }));

/** Painel de assinatura por PIX. Se a empresa ainda não tem plano, primeiro escolhe (cards); depois
 *  gera a cobrança PIX (QR + copia-e-cola) e faz polling até confirmar. Funciona em Trial/Vencido/
 *  Bloqueada — /assinatura é rota livre. Permite trocar de plano antes de pagar. */
export default function PagarAssinatura({ onClose }: { onClose: () => void }) {
  const qc = useQueryClient();
  const [confirmado, setConfirmado] = useState(false);
  const [escolhendo, setEscolhendo] = useState<boolean | null>(null); // null = decidindo (status carregando)
  const fechouRef = useRef(false);
  const cobrouRef = useRef(false);

  const { data: status } = useQuery({
    queryKey: ["assinatura-minha"],
    queryFn: () => api<AssinaturaStatus>("/api/v1/assinatura/minha"),
    refetchInterval: confirmado ? false : 5000,
  });

  const { data: planos } = useQuery({
    queryKey: ["assinatura-planos"],
    queryFn: () => api<Plano[]>("/api/v1/assinatura/planos"),
    enabled: escolhendo === true,
  });

  const cobrar = useMutation({
    mutationFn: () => api<CobrancaPix>("/api/v1/assinatura/cobrar", { method: "POST" }),
  });

  const escolher = useMutation({
    mutationFn: (planoId: string) => api("/api/v1/assinatura/escolher-plano", { method: "POST", body: JSON.stringify({ planoId }) }),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ["assinatura-minha"] }); cobrouRef.current = false; setEscolhendo(false); },
  });

  // 1ª carga do status: sem plano => escolher; com plano => pagar.
  useEffect(() => { if (status && escolhendo === null) setEscolhendo(!status.planoNome); }, [status, escolhendo]);

  // Ao entrar na etapa de pagar, gera a cobrança (uma vez).
  useEffect(() => { if (escolhendo === false && !cobrouRef.current) { cobrouRef.current = true; cobrar.mutate(); } /* eslint-disable-next-line react-hooks/exhaustive-deps */ }, [escolhendo]);

  // Pagamento empurra o vencimento +1 mês => estado volta para "EmDia" e bloqueada=false.
  useEffect(() => {
    if (!status || confirmado) return;
    if (status.estado === "EmDia" && !status.bloqueada) {
      setConfirmado(true);
      qc.invalidateQueries({ queryKey: ["assinatura-minha"] });
      if (!fechouRef.current) { fechouRef.current = true; setTimeout(onClose, 1800); }
    }
  }, [status, confirmado, qc, onClose]);

  const trocarPlano = () => { cobrar.reset(); cobrouRef.current = false; setEscolhendo(true); };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 p-4" onClick={onClose}>
      <div className="w-full max-w-sm rounded-2xl bg-slate-800 p-5 text-slate-100 shadow-xl" onClick={(e) => e.stopPropagation()}>
        <div className="mb-3 flex items-center justify-between">
          <h2 className="text-lg font-semibold">Assinatura</h2>
          <button onClick={onClose} className="text-slate-400 hover:text-slate-200" aria-label="Fechar">✕</button>
        </div>

        {confirmado ? (
          <div className="py-8 text-center">
            <p className="text-3xl">✅</p>
            <p className="mt-2 font-semibold text-emerald-400">Pagamento confirmado!</p>
            <p className="text-sm text-slate-400">Acesso liberado. Fechando…</p>
          </div>
        ) : escolhendo === null ? (
          <p className="py-8 text-center text-slate-300">Carregando…</p>
        ) : escolhendo ? (
          <div className="space-y-3">
            <p className="text-sm text-slate-400">Escolha o plano da sua empresa:</p>
            {planos?.length === 0 && <p className="text-sm text-amber-400">Nenhum plano disponível. Contate o suporte.</p>}
            {planos?.map((p) => (
              <div key={p.id} className="flex items-center justify-between gap-3 rounded-xl border border-slate-700 bg-slate-900/50 p-3">
                <div>
                  <p className="font-semibold">{p.nome}</p>
                  <p className="text-xs text-slate-400">{p.limiteObras ?? "∞"} obras · {p.limiteUsuarios ?? "∞"} usuários</p>
                  <p className="text-sm text-sky-300">{brl(p.precoMensal) || "—"}<span className="text-xs text-slate-500">/mês</span></p>
                </div>
                <button onClick={() => escolher.mutate(p.id)} disabled={escolher.isPending} className="shrink-0 rounded-lg bg-sky-600 px-3 py-2 text-sm font-semibold hover:bg-sky-500 disabled:opacity-50">
                  {escolher.isPending ? "…" : "Escolher"}
                </button>
              </div>
            ))}
            {escolher.isError && <p className="text-sm text-red-400">{(escolher.error as Error).message}</p>}
          </div>
        ) : cobrar.isPending ? (
          <p className="py-8 text-center text-slate-300">Gerando cobrança…</p>
        ) : cobrar.isError ? (
          <div className="space-y-3">
            <p className="text-sm text-red-400">{(cobrar.error as Error).message}</p>
            <button onClick={() => cobrar.mutate()} className="w-full rounded-lg bg-sky-600 py-2 font-semibold hover:bg-sky-500">Tentar de novo</button>
            <button onClick={trocarPlano} className="w-full text-center text-sm text-slate-400 hover:text-slate-200">Escolher outro plano</button>
          </div>
        ) : cobrar.data ? (
          <div className="space-y-4">
            <div className="flex items-center justify-between text-sm">
              <span className="text-slate-400">Plano <span className="text-slate-100">{status?.planoNome}</span>{cobrar.data.valor != null ? ` · ${brl(cobrar.data.valor)}` : ""}</span>
              <button onClick={trocarPlano} className="text-xs text-sky-400 hover:text-sky-300">Trocar</button>
            </div>
            {cobrar.data.url && (
              <a href={cobrar.data.url} target="_blank" rel="noopener noreferrer"
                 className="block w-full rounded-lg bg-emerald-600 py-3 text-center font-semibold hover:bg-emerald-500">
                Ir para o pagamento
              </a>
            )}
            <p className="text-center text-xs text-slate-400">Você abre a página segura do AbacatePay (PIX, cartão ou boleto). Após o pagamento, o acesso é liberado automaticamente.</p>
            <p className="text-center text-xs text-slate-500">Aguardando pagamento…</p>
          </div>
        ) : null}
      </div>
    </div>
  );
}
