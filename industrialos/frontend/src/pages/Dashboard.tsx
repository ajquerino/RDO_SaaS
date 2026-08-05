import { useQuery } from "@tanstack/react-query";
import { api, type Dashboard as Dash, type Produtividade as Prod } from "../lib/api";

const FAROL: Record<string, { cor: string; rotulo: string }> = {
  verde: { cor: "#10b981", rotulo: "No prazo" },
  amarelo: { cor: "#f59e0b", rotulo: "Atenção" },
  vermelho: { cor: "#ef4444", rotulo: "Atrasado" },
  cinza: { cor: "#64748b", rotulo: "Sem prazo definido" },
};

/** Aba Dashboard da obra (Sprint 6): faróis, avanço, HH, Curva S e Paretos. */
export default function Dashboard({ obraId }: { obraId: string }) {
  const { data, isLoading, isError } = useQuery({
    queryKey: ["dashboard", obraId],
    queryFn: () => api<Dash>(`/api/v1/obras/${obraId}/dashboard`),
  });

  if (isLoading) return <p className="text-slate-400 text-sm py-4">Carregando indicadores…</p>;
  if (isError || !data) return <p className="text-slate-400 text-sm py-4">Não foi possível carregar o dashboard.</p>;

  const farol = FAROL[data.farol.cor] ?? FAROL.cinza;

  return (
    <div className="space-y-5">
      {/* Cartoes de topo */}
      <div className="grid grid-cols-2 gap-3 sm:grid-cols-4">
        <Cartao rotulo="Avanço da obra" valor={`${data.avanco.pct}%`} sub={`base ${data.avanco.baseAvanco}`} />
        <Cartao rotulo="Farol" valor={<span style={{ color: farol.cor }}>●</span>} sub={`${farol.rotulo}${data.farol.desvio != null ? ` · desvio ${data.farol.desvio}%` : ""}`} />
        <Cartao rotulo="Efetivo médio" valor={data.hh.efetivoMedio} sub={`${data.rdos} RDOs`} />
        <Cartao rotulo="HH realizado" valor={data.hh.realizado.toLocaleString("pt-BR")} sub={`de ${data.hh.previsto.toLocaleString("pt-BR")} prev.`} />
      </div>

      {/* Avanco geral (barra) */}
      <Painel titulo="Avanço geral">
        <Barra pct={data.avanco.pct} cor="#0ea5e9" />
      </Painel>

      {/* HH previsto x realizado */}
      <Painel titulo="HH previsto × realizado">
        <div className="space-y-2">
          <BarraRotulada rotulo="Previsto" valor={data.hh.previsto} max={Math.max(data.hh.previsto, data.hh.realizado, 1)} cor="#64748b" />
          <BarraRotulada rotulo="Realizado" valor={data.hh.realizado} max={Math.max(data.hh.previsto, data.hh.realizado, 1)} cor="#0ea5e9" />
        </div>
      </Painel>

      {/* Produtividade — HH direto x indireto */}
      <Painel titulo="HH direto × indireto">
        <Produtividade p={data.produtividade} />
      </Painel>

      {/* Efetivo por função */}
      <Painel titulo="Efetivo por função (HH)">
        {data.produtividade.porFuncao.length > 0 ? (
          <Pareto itens={data.produtividade.porFuncao.map((f) => ({ rotulo: f.funcao, valor: f.hh, extra: `${f.pessoas} pess.` }))} cor="#8b5cf6" sufixo=" HH" />
        ) : <Vazio texto="Sem efetivo lançado." />}
      </Painel>

      {/* Curva S */}
      <Painel titulo="Curva S (acumulado % do HH)">
        {data.curvaS.length > 0 ? <CurvaS pontos={data.curvaS} /> : <Vazio texto="Sem RDOs para plotar ainda." />}
      </Painel>

      {/* Avanco por item */}
      <Painel titulo="Avanço por item da EAP">
        {data.avanco.itens.length > 0 ? (
          <ul className="space-y-2">
            {data.avanco.itens.map((i, k) => (
              <li key={k}>
                <div className="flex justify-between text-sm">
                  <span className="truncate pr-2">{i.descricao}</span>
                  <span className="text-slate-400 shrink-0">{i.pct}%{i.hhPrevisto ? ` · ${i.hhPrevisto} HH` : ""}</span>
                </div>
                <Barra pct={i.pct} cor="#0ea5e9" fino />
              </li>
            ))}
          </ul>
        ) : <Vazio texto="Nenhum item na EAP." />}
      </Painel>

      {/* Pareto paralisacoes */}
      <Painel titulo="Pareto — paralisações (minutos)">
        {data.paralisacoes.length > 0 ? (
          <Pareto itens={data.paralisacoes.map((p) => ({ rotulo: p.motivo, valor: p.minutos, extra: `${p.ocorrencias}×` }))} cor="#f59e0b" sufixo=" min" />
        ) : <Vazio texto="Sem paralisações registradas." />}
      </Painel>

      {/* Pareto retrabalho */}
      <Painel titulo="Pareto — retrabalho (HH perdido)">
        {data.retrabalho.length > 0 ? (
          <Pareto itens={data.retrabalho.map((r) => ({ rotulo: r.causa, valor: r.hh }))} cor="#ef4444" sufixo=" HH" />
        ) : <Vazio texto="Sem retrabalho registrado." />}
      </Painel>
    </div>
  );
}

// ---- componentes visuais ----
function Cartao({ rotulo, valor, sub }: { rotulo: string; valor: React.ReactNode; sub?: string }) {
  return (
    <div className="rounded-xl bg-slate-800 p-3">
      <p className="text-xs text-slate-400">{rotulo}</p>
      <p className="text-2xl font-bold leading-tight">{valor}</p>
      {sub && <p className="text-xs text-slate-500">{sub}</p>}
    </div>
  );
}

function Painel({ titulo, children }: { titulo: string; children: React.ReactNode }) {
  return (
    <section className="rounded-xl bg-slate-800 p-4">
      <h3 className="text-sm font-semibold text-slate-300 mb-3">{titulo}</h3>
      {children}
    </section>
  );
}

function Barra({ pct, cor, fino }: { pct: number; cor: string; fino?: boolean }) {
  return (
    <div className={`${fino ? "h-1.5" : "h-3"} rounded-full bg-slate-700 mt-1`}>
      <div className={`${fino ? "h-1.5" : "h-3"} rounded-full`} style={{ width: `${Math.min(100, Math.max(0, pct))}%`, background: cor }} />
    </div>
  );
}

function BarraRotulada({ rotulo, valor, max, cor }: { rotulo: string; valor: number; max: number; cor: string }) {
  return (
    <div>
      <div className="flex justify-between text-xs text-slate-400"><span>{rotulo}</span><span>{valor.toLocaleString("pt-BR")} HH</span></div>
      <div className="h-3 rounded-full bg-slate-700 mt-1">
        <div className="h-3 rounded-full" style={{ width: `${(valor / max) * 100}%`, background: cor }} />
      </div>
    </div>
  );
}

function Pareto({ itens, cor, sufixo }: { itens: { rotulo: string; valor: number; extra?: string }[]; cor: string; sufixo: string }) {
  const max = Math.max(...itens.map((i) => i.valor), 1);
  return (
    <ul className="space-y-2">
      {itens.map((i, k) => (
        <li key={k}>
          <div className="flex justify-between text-sm">
            <span className="truncate pr-2">{i.rotulo}</span>
            <span className="text-slate-400 shrink-0">{i.valor.toLocaleString("pt-BR")}{sufixo}{i.extra ? ` · ${i.extra}` : ""}</span>
          </div>
          <div className="h-2 rounded-full bg-slate-700 mt-1">
            <div className="h-2 rounded-full" style={{ width: `${(i.valor / max) * 100}%`, background: cor }} />
          </div>
        </li>
      ))}
    </ul>
  );
}

function CurvaS({ pontos }: { pontos: { data: string; previstoPct: number; realizadoPct: number }[] }) {
  const W = 320, H = 160, P = 28;
  const n = pontos.length;
  const maxY = Math.max(100, ...pontos.map((p) => Math.max(p.previstoPct, p.realizadoPct)));
  const x = (i: number) => P + (n <= 1 ? 0 : (i / (n - 1)) * (W - 2 * P));
  const y = (v: number) => H - P - (v / maxY) * (H - 2 * P);
  const linha = (sel: (p: typeof pontos[number]) => number) =>
    pontos.map((p, i) => `${i === 0 ? "M" : "L"}${x(i).toFixed(1)},${y(sel(p)).toFixed(1)}`).join(" ");

  return (
    <div className="overflow-x-auto">
      <svg viewBox={`0 0 ${W} ${H}`} className="w-full min-w-[280px]" role="img" aria-label="Curva S">
        {[0, 25, 50, 75, 100].map((g) => (
          <g key={g}>
            <line x1={P} y1={y(g)} x2={W - P} y2={y(g)} stroke="#334155" strokeWidth="0.5" />
            <text x={P - 4} y={y(g) + 3} textAnchor="end" fontSize="7" fill="#64748b">{g}</text>
          </g>
        ))}
        <path d={linha((p) => p.previstoPct)} fill="none" stroke="#64748b" strokeWidth="1.5" strokeDasharray="4 3" />
        <path d={linha((p) => p.realizadoPct)} fill="none" stroke="#0ea5e9" strokeWidth="2" />
        {n === 1 && (
          <>
            <circle cx={x(0)} cy={y(pontos[0].previstoPct)} r="2.5" fill="#64748b" />
            <circle cx={x(0)} cy={y(pontos[0].realizadoPct)} r="2.5" fill="#0ea5e9" />
          </>
        )}
      </svg>
      <div className="flex gap-4 text-xs text-slate-400 mt-1">
        <span className="flex items-center gap-1"><span className="inline-block w-4 border-t-2 border-dashed border-slate-500" /> Previsto</span>
        <span className="flex items-center gap-1"><span className="inline-block w-4 border-t-2 border-sky-500" /> Realizado</span>
      </div>
    </div>
  );
}

function Produtividade({ p }: { p: Prod }) {
  const total = p.hhDireto + p.hhIndireto;
  if (total <= 0 && p.hhNaoClassificado <= 0) return <Vazio texto="Sem HH lançado." />;
  const fmt = (v: number) => v.toLocaleString("pt-BR");
  return (
    <div className="space-y-3">
      <div className="grid grid-cols-2 gap-2 sm:grid-cols-3">
        <Cartao rotulo="HH direto" valor={fmt(p.hhDireto)} sub={`${p.pctDireto}%`} />
        <Cartao rotulo="HH indireto" valor={fmt(p.hhIndireto)} sub={`${p.pctIndireto}%`} />
        <Cartao rotulo="Não classificado" valor={fmt(p.hhNaoClassificado)} sub="função fora do catálogo" />
      </div>
      {total > 0 && (
        <div className="flex h-4 overflow-hidden rounded-full bg-slate-700">
          <div className="h-4" style={{ width: `${(p.hhDireto / total) * 100}%`, background: "#10b981" }} title={`Direto ${p.pctDireto}%`} />
          <div className="h-4" style={{ width: `${(p.hhIndireto / total) * 100}%`, background: "#f59e0b" }} title={`Indireto ${p.pctIndireto}%`} />
        </div>
      )}
      <div className="flex gap-4 text-xs text-slate-400">
        <span className="flex items-center gap-1"><span className="inline-block h-2 w-3 rounded" style={{ background: "#10b981" }} /> Direto</span>
        <span className="flex items-center gap-1"><span className="inline-block h-2 w-3 rounded" style={{ background: "#f59e0b" }} /> Indireto</span>
      </div>
    </div>
  );
}

function Vazio({ texto }: { texto: string }) {
  return <p className="text-sm text-slate-500">{texto}</p>;
}
