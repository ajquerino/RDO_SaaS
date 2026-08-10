// Página pública "Conheça o IndustrialOS" (rota /conheca, sem login). Explica o produto em detalhe.
// Mobile-first: uma coluna, texto legível, CTAs grandes; no desktop só centraliza com max-width.

const PASSOS = [
  { n: "1", t: "EXECUTAR", d: "A equipe trabalha na frente de serviço." },
  { n: "2", t: "REGISTRAR", d: "O RDO do dia: efetivo, horas, avanço, fotos e segurança (até offline no canteiro)." },
  { n: "3", t: "MEDIR", d: "O avanço vira % físico, HH e boletim de medição, com farol e Curva S." },
  { n: "4", t: "FATURAR", d: "A medição vira evento de faturamento conforme o contrato." },
  { n: "5", t: "COMPROVAR", d: "PDF, fotos e assinatura do fiscal comprovam tudo, com aprovação por link." },
];

const BENEFICIOS = [
  { i: "📋", t: "RDO digital", d: "Efetivo, HH com hora-extra, fotos, assinatura; funciona OFFLINE no canteiro." },
  { i: "📊", t: "Dashboards e farol", d: "Avanço, Curva S e produtividade em tempo real." },
  { i: "💰", t: "Medição e faturamento", d: "Do boletim ao recebimento, configurável por contrato." },
  { i: "👥", t: "Multi-empresa e permissões", d: "Cada função vê o que precisa; valores só p/ quem pode." },
  { i: "✅", t: "Aprovação por link", d: "O fiscal aprova/solicita revisão sem precisar de conta." },
];

function Ctas({ centralizado = false }: { centralizado?: boolean }) {
  return (
    <div className={`flex flex-col gap-3 sm:flex-row ${centralizado ? "sm:justify-center" : ""}`}>
      <a
        href="/criar-conta"
        className="flex items-center justify-center gap-2 rounded-xl bg-sky-600 px-5 py-3.5 text-center font-semibold hover:bg-sky-500"
      >
        Criar conta grátis
        <span className="rounded-full bg-white/15 px-2 py-0.5 text-xs font-medium">14 dias</span>
      </a>
      <a
        href="/"
        className="rounded-xl border border-slate-600 px-5 py-3.5 text-center font-semibold text-slate-200 hover:bg-slate-800"
      >
        Entrar
      </a>
    </div>
  );
}

function Secao({ rotulo, titulo, children }: { rotulo: string; titulo: string; children: React.ReactNode }) {
  return (
    <section className="space-y-4">
      <div>
        <p className="text-xs font-semibold uppercase tracking-wider text-sky-400">{rotulo}</p>
        <h2 className="mt-1 text-xl font-bold sm:text-2xl">{titulo}</h2>
      </div>
      {children}
    </section>
  );
}

export default function Conheca() {
  return (
    <main className="min-h-screen bg-gradient-to-b from-slate-900 to-slate-950 text-slate-100">
      <div className="mx-auto w-full max-w-2xl space-y-14 px-5 py-12">

        {/* HERO */}
        <header className="space-y-5 text-center">
          <div className="inline-flex items-center gap-2 text-lg font-bold tracking-tight">
            <span aria-hidden>🏗️</span>
            <span>Industrial<span className="text-sky-400">OS</span></span>
          </div>
          <h1 className="text-3xl font-bold leading-tight sm:text-4xl">
            O sistema operacional da execução da sua obra industrial.
          </h1>
          <p className="mx-auto max-w-xl text-base leading-relaxed text-slate-300">
            O RDO que sua equipe preenche no campo alimenta sozinho avanço, medição e faturamento — do
            canteiro ao recebimento.
          </p>
          <Ctas centralizado />
        </header>

        {/* 1. O PROBLEMA */}
        <Secao rotulo="O problema" titulo="O diário de obra hoje">
          <p className="rounded-2xl bg-slate-800/60 p-5 text-slate-300 ring-1 ring-slate-700/50">
            Vira papel ou PDF que ninguém usa. O dado do campo não vira decisão, nem medição, nem
            faturamento — e chega atrasado.
          </p>
        </Secao>

        {/* 2. COMO FUNCIONA */}
        <Secao rotulo="Como funciona" titulo="Do campo ao recebimento">
          <ol className="space-y-3">
            {PASSOS.map((p) => (
              <li key={p.n} className="flex gap-4 rounded-xl bg-slate-800/50 p-4 ring-1 ring-slate-700/40">
                <span className="flex h-8 w-8 flex-none items-center justify-center rounded-full bg-sky-600/20 text-sm font-bold text-sky-300">
                  {p.n}
                </span>
                <div>
                  <p className="font-semibold tracking-wide">{p.t}</p>
                  <p className="text-sm leading-snug text-slate-400">{p.d}</p>
                </div>
              </li>
            ))}
          </ol>
        </Secao>

        {/* 3. O QUE VOCÊ GANHA */}
        <Secao rotulo="O que você ganha" titulo="Tudo num sistema só">
          <ul className="space-y-3">
            {BENEFICIOS.map((b) => (
              <li key={b.t} className="flex items-start gap-3 rounded-xl bg-slate-800/50 p-4 ring-1 ring-slate-700/40">
                <span className="text-xl leading-none" aria-hidden>{b.i}</span>
                <div>
                  <p className="font-medium">{b.t}</p>
                  <p className="text-sm leading-snug text-slate-400">{b.d}</p>
                </div>
              </li>
            ))}
          </ul>
        </Secao>

        {/* 4. POR QUE NÃO É "SÓ MAIS UM APP" */}
        <Secao rotulo="Diferencial" titulo="Nem ERP, nem diário genérico">
          <p className="rounded-2xl bg-slate-800/60 p-5 text-slate-300 ring-1 ring-slate-700/50">
            Não tentamos ser ERP. Não somos um diário genérico. Somos o dado de campo{" "}
            <span className="font-semibold text-slate-100">INDUSTRIAL</span> que fecha o ciclo até o
            faturamento.
          </p>
        </Secao>

        {/* 5. FECHO / CTA final */}
        <section className="space-y-6 text-center">
          <p className="text-2xl font-bold leading-snug sm:text-3xl">
            Não é um diário de obra.<br />
            <span className="text-sky-400">É o sistema operacional da execução industrial.</span>
          </p>
          <Ctas centralizado />
        </section>

        {/* RODAPÉ */}
        <footer className="flex flex-wrap items-center justify-center gap-x-3 gap-y-1 border-t border-slate-800 pt-6 text-center text-xs text-slate-500">
          <a href="https://wa.me/" target="_blank" rel="noopener noreferrer" className="hover:text-slate-300">
            Fale conosco (WhatsApp)
          </a>
          <span aria-hidden>·</span>
          <a href="#" className="hover:text-slate-300">Termos</a>
          <span aria-hidden>·</span>
          <a href="#" className="hover:text-slate-300">Privacidade</a>
        </footer>
      </div>
    </main>
  );
}
