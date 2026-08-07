import { useAssinatura } from "../lib/useAssinatura";

/** Banner de estado da assinatura no topo do app do tenant.
 *  - PrestesAVencer: amarelo (mais forte nos avisos de 3/1 dia)
 *  - Vencido: laranja (ainda dá pra regularizar antes de bloquear)
 *  - Bloqueada: vermelho fixo (acesso somente-leitura) */
export default function BannerAssinatura() {
  const a = useAssinatura();
  if (!a.estado) return null;

  if (a.estado === "PrestesAVencer") {
    const forte = a.avisoNivel === 1 || a.avisoNivel === 3;
    const dias = a.diasParaVencer ?? 0;
    return (
      <div className={`px-4 py-2 text-sm text-center ${forte ? "bg-amber-500/25 text-amber-100 font-medium" : "bg-amber-500/15 text-amber-200"}`}>
        Sua assinatura vence {dias <= 0 ? "hoje" : `em ${dias} dia${dias > 1 ? "s" : ""}`}. Regularize para não interromper o cadastro.
      </div>
    );
  }

  if (a.estado === "Vencido")
    return (
      <div className="bg-orange-600/25 px-4 py-2 text-center text-sm font-medium text-orange-100">
        Assinatura vencida há {a.diasAtraso} dia{(a.diasAtraso ?? 0) > 1 ? "s" : ""} — regularize para não bloquear o acesso.
      </div>
    );

  if (a.bloqueada || a.estado === "Bloqueada")
    return (
      <div className="sticky top-0 z-20 bg-red-700 px-4 py-2 text-center text-sm font-semibold text-white">
        Acesso somente leitura — assinatura vencida há {a.diasAtraso} dia{(a.diasAtraso ?? 0) > 1 ? "s" : ""}. Você vê e baixa o que já foi salvo, mas não cria nem edita.
      </div>
    );

  return null;
}
