import { useState } from "react";
import { useAssinatura } from "../lib/useAssinatura";
import PagarAssinatura from "./PagarAssinatura";

/** Banner de estado da assinatura no topo do app do tenant.
 *  - PrestesAVencer: amarelo (mais forte nos avisos de 3/1 dia; botão de PIX a partir do aviso de 3 dias)
 *  - Vencido: laranja (ainda dá pra regularizar antes de bloquear)
 *  - Bloqueada: vermelho fixo (acesso somente-leitura)
 *  Em Vencido/Bloqueada (e PrestesAVencer com avisoNivel<=3) mostra "Pagar agora (PIX)". */
export default function BannerAssinatura() {
  const a = useAssinatura();
  const [pagar, setPagar] = useState(false);
  if (!a.estado) return null;

  const botaoPix = (
    <button
      onClick={() => setPagar(true)}
      className="ml-2 inline-block rounded-md bg-white/15 px-2.5 py-1 text-xs font-semibold text-white hover:bg-white/25"
    >
      Pagar agora (PIX)
    </button>
  );

  let banner: React.ReactNode = null;

  if (a.estado === "PrestesAVencer") {
    const forte = a.avisoNivel === 1 || a.avisoNivel === 3;
    const dias = a.diasParaVencer ?? 0;
    banner = (
      <div className={`px-4 py-2 text-sm text-center ${forte ? "bg-amber-500/25 text-amber-100 font-medium" : "bg-amber-500/15 text-amber-200"}`}>
        Sua assinatura vence {dias <= 0 ? "hoje" : `em ${dias} dia${dias > 1 ? "s" : ""}`}. Regularize para não interromper o cadastro.
        {(a.avisoNivel ?? 9) <= 3 && botaoPix}
      </div>
    );
  } else if (a.estado === "Vencido") {
    banner = (
      <div className="bg-orange-600/25 px-4 py-2 text-center text-sm font-medium text-orange-100">
        Assinatura vencida há {a.diasAtraso} dia{(a.diasAtraso ?? 0) > 1 ? "s" : ""} — regularize para não bloquear o acesso.
        {botaoPix}
      </div>
    );
  } else if (a.bloqueada || a.estado === "Bloqueada") {
    banner = (
      <div className="sticky top-0 z-20 bg-red-700 px-4 py-2 text-center text-sm font-semibold text-white">
        Acesso somente leitura — assinatura vencida há {a.diasAtraso} dia{(a.diasAtraso ?? 0) > 1 ? "s" : ""}. Você vê e baixa o que já foi salvo, mas não cria nem edita.
        {botaoPix}
      </div>
    );
  }

  if (!banner) return null;
  return (
    <>
      {banner}
      {pagar && <PagarAssinatura onClose={() => setPagar(false)} />}
    </>
  );
}
