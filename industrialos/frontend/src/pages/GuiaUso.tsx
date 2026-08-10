import { useEffect } from "react";

// Guia de uso do app — overlay de tela cheia, mobile-first, acessível a todo usuário logado.
// Sem dependência de permissão. Fecha no X e no ESC.

type Secao = { icone: string; titulo: string; itens: string[] };

const SECOES: Secao[] = [
  {
    icone: "🧭",
    titulo: "Comece por aqui",
    itens: [
      "1) Abra ou crie uma obra.",
      "2) Monte a EAP (o escopo/itens).",
      "3) Faça o RDO do dia.",
      "4) Acompanhe o Dashboard.",
      "5) Gere a Medição.",
      "Dica: toda conta já vem com uma obra “[EXEMPLO]” preenchida pra você explorar — pode apagar quando quiser.",
    ],
  },
  {
    icone: "📝",
    titulo: "RDO — Relatório Diário de Obra (o coração)",
    itens: [
      "Efetivo por função com entrada/saída e hora-extra (HH calculado).",
      "Serviços do dia vinculados à EAP, com avanço (% ou quantidade executada).",
      "Paralisações (chuva etc.) e retrabalho (HH perdido, causa, ação corretiva).",
      "Segurança: DDS, APR, PT, EPIs.",
      "Clima e localização por GPS; fotos; assinaturas (encarregado/fiscal).",
      "Gera PDF e salva sozinho (autosave).",
      "Funciona OFFLINE no canteiro e sincroniza depois.",
      "Mantém histórico de revisões.",
    ],
  },
  {
    icone: "🏗️",
    titulo: "Obras & EAP",
    itens: [
      "Cadastro da obra: contrato, local, frente de serviço, prazo de pagamento.",
      "EAP/escopo com itens: unidade, quantidade prevista, HH previsto, valor, disciplina.",
      "Importe a EAP de qualquer planilha (CSV/XLSX) apontando as colunas na tela, ou adicione manual.",
      "Edite os itens a qualquer momento.",
    ],
  },
  {
    icone: "📊",
    titulo: "Dashboards & indicadores",
    itens: [
      "Avanço físico (base HH ou quantidade) e % por item.",
      "Curva S (previsto x realizado) e farol de status.",
      "Pareto de paralisações e causas de retrabalho.",
      "Produtividade.",
    ],
  },
  {
    icone: "💰",
    titulo: "Medição & Faturamento",
    itens: [
      "Boletim de medição a partir do avanço.",
      "Faturamento configurável por contrato (entrada/mobilização/medição/comissionamento; condições tipo 30/60).",
    ],
  },
  {
    icone: "✅",
    titulo: "Aprovação por link",
    itens: [
      "Envie o RDO por um link para o fiscal aprovar ou solicitar revisão — ele não precisa ter conta.",
    ],
  },
  {
    icone: "👥",
    titulo: "Equipe, empresas & permissões",
    itens: [
      "Multi-empresa.",
      "Papéis: Admin, Gestor, Planejador, Encarregado, Líder, Supervisor.",
      "Valores em R$ só para Planejador/Gestor/Admin.",
      "Vínculo usuário–obra.",
      "Configure funções de mão de obra e a regra de hora-extra por empresa.",
    ],
  },
  {
    icone: "📴",
    titulo: "Offline no campo",
    itens: [
      "Crie e edite RDO sem internet.",
      "Fila de sincronização automática.",
      "Marcação de “não sincronizado” até subir.",
    ],
  },
  {
    icone: "💳",
    titulo: "Conta & assinatura",
    itens: [
      "Trial de 14 dias.",
      "Pagamento por PIX, boleto ou cartão.",
      "Avisos de vencimento.",
      "Ao vencer, a conta fica somente-leitura (não perde nada, só não cria/edita).",
    ],
  },
  {
    icone: "🔒",
    titulo: "Segurança & dados",
    itens: [
      "Uma sessão por usuário (anti-compartilhamento).",
      "Trilha de auditoria e LGPD.",
      "Cada empresa só enxerga os próprios dados.",
    ],
  },
];

const FUTURO = {
  aviso:
    "Estamos sempre evoluindo. Isto está no RADAR, sem data e sujeito a mudança conforme a prioridade e o feedback de vocês.",
  itens: [
    {
      titulo: "IA industrial",
      texto:
        "Resumo automático do RDO e alerta de risco de atraso e de produtividade, lendo a Curva S, as paralisações e o retrabalho (IA de obra industrial, não chatbot genérico).",
    },
    {
      titulo: "Integração com seu ERP/financeiro",
      texto: "Ponte (API/webhook) para o dado de campo alimentar o sistema que você já usa, sem redigitar.",
    },
    {
      titulo: "RDO em minutos",
      texto: "Modo rápido com efetivo e equipamento padrão da frente pré-preenchidos.",
    },
  ],
};

export default function GuiaUso({ onFechar }: { onFechar: () => void }) {
  // Fecha no ESC.
  useEffect(() => {
    const onKey = (e: KeyboardEvent) => { if (e.key === "Escape") onFechar(); };
    window.addEventListener("keydown", onKey);
    return () => window.removeEventListener("keydown", onKey);
  }, [onFechar]);

  return (
    <div className="fixed inset-0 z-[80] overflow-y-auto overscroll-contain bg-slate-900 text-slate-100">
      {/* Cabeçalho fixo com título + fechar */}
      <div className="sticky top-0 z-10 flex items-center justify-between gap-3 border-b border-slate-800 bg-slate-900/95 px-4 py-3 backdrop-blur">
        <h2 className="text-base font-bold sm:text-lg">📖 Guia do IndustrialOS — tudo que dá pra fazer</h2>
        <button
          onClick={onFechar}
          aria-label="Fechar guia"
          className="flex-none rounded-lg bg-slate-800 px-3 py-1.5 text-sm hover:bg-slate-700"
        >
          ✕
        </button>
      </div>

      <div className="mx-auto max-w-2xl space-y-3 px-4 py-5">
        {SECOES.map((s, i) => (
          <details
            key={s.titulo}
            open={i === 0}
            className="group rounded-xl bg-slate-800/60 ring-1 ring-slate-700/50"
          >
            <summary className="flex cursor-pointer list-none items-center justify-between gap-2 px-4 py-3 font-semibold [&::-webkit-details-marker]:hidden">
              <span className="flex items-center gap-2">
                <span aria-hidden>{s.icone}</span>
                {s.titulo}
              </span>
              <span aria-hidden className="text-slate-500 transition-transform group-open:rotate-90">›</span>
            </summary>
            <ul className="space-y-1.5 px-4 pb-4 pl-11 text-sm text-slate-300">
              {s.itens.map((it, j) => (
                <li key={j} className="list-disc leading-snug marker:text-slate-600">{it}</li>
              ))}
            </ul>
          </details>
        ))}

        {/* O que vem por aí — FUTURO: visual diferente (tracejado + badge "Em breve"), fechado. */}
        <details className="group rounded-xl border border-dashed border-sky-500/40 bg-sky-500/5">
          <summary className="flex cursor-pointer list-none items-center justify-between gap-2 px-4 py-3 font-semibold [&::-webkit-details-marker]:hidden">
            <span className="flex items-center gap-2">
              <span aria-hidden>🚀</span>
              O que vem por aí
              <span className="rounded-full bg-sky-500/20 px-2 py-0.5 text-xs font-medium text-sky-300">Em breve</span>
            </span>
            <span aria-hidden className="text-slate-500 transition-transform group-open:rotate-90">›</span>
          </summary>
          <div className="space-y-3 px-4 pb-4 text-sm">
            <p className="rounded-lg bg-slate-800/60 px-3 py-2 text-xs text-slate-400">{FUTURO.aviso}</p>
            <ul className="space-y-2.5">
              {FUTURO.itens.map((it) => (
                <li key={it.titulo} className="rounded-lg bg-slate-800/40 p-3 ring-1 ring-slate-700/40">
                  <p className="font-medium text-slate-200">{it.titulo}</p>
                  <p className="text-xs leading-snug text-slate-400">{it.texto}</p>
                </li>
              ))}
            </ul>
          </div>
        </details>

        <div className="pt-2 text-center">
          <button onClick={onFechar} className="rounded-lg bg-sky-600 px-5 py-2.5 text-sm font-semibold hover:bg-sky-500">
            Entendi, fechar
          </button>
        </div>
      </div>
    </div>
  );
}
