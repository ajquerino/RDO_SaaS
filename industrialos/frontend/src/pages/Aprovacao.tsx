import { useState } from "react";
import { useMutation, useQuery } from "@tanstack/react-query";
import { apiPublico, urlPublica, type AprovacaoView } from "../lib/api";

/** Tela publica (sem login) onde o fiscal do cliente aprova ou pede revisao de um RDO. */
export default function Aprovacao({ token }: { token: string }) {
  const [nome, setNome] = useState("");
  const [motivo, setMotivo] = useState("");
  const [modo, setModo] = useState<"ver" | "revisar">("ver");
  const [erro, setErro] = useState<string | null>(null);
  // Resultado da ação (aprovado/revisão). Quando setado, mostramos a tela de sucesso SEM refazer o GET
  // — o token é consumido na aprovação, então refazer a busca daria 404 "link expirado" (era o bug).
  const [feito, setFeito] = useState<null | { tipo: "aprovado" | "revisao"; por: string; em?: string; motivo?: string }>(null);

  const { data, isLoading, isError } = useQuery({
    queryKey: ["aprovacao", token],
    queryFn: () => apiPublico<AprovacaoView>(`/api/v1/aprovacao/${token}`),
    retry: false,
    enabled: !feito, // não refetch depois de agir (token já consumido)
  });

  const aprovar = useMutation({
    mutationFn: () => apiPublico<{ status: string; aprovadoPor: string; aprovadoEm: string }>(`/api/v1/aprovacao/${token}/aprovar`, { method: "POST", body: JSON.stringify({ nome }) }),
    onSuccess: (r) => { setErro(null); setFeito({ tipo: "aprovado", por: r.aprovadoPor ?? nome, em: r.aprovadoEm }); },
    onError: (e) => setErro((e as Error).message),
  });
  const solicitar = useMutation({
    mutationFn: () => apiPublico(`/api/v1/aprovacao/${token}/revisao`, { method: "POST", body: JSON.stringify({ nome, motivo }) }),
    onSuccess: () => { setErro(null); setFeito({ tipo: "revisao", por: nome, motivo }); },
    onError: (e) => setErro((e as Error).message),
  });

  if (feito) return (
    <Casca>
      {feito.tipo === "aprovado"
        ? <Aviso tipo="ok" titulo="RDO aprovado ✓" texto={`Obrigado, ${feito.por}! Aprovação registrada${feito.em ? ` em ${new Date(feito.em).toLocaleString("pt-BR")}` : ""}. A equipe da obra foi notificada.`} />
        : <Aviso tipo="alerta" titulo="Revisão solicitada" texto={`Enviado por ${feito.por}. A equipe da obra vai corrigir e reenviar.${feito.motivo ? `\n\nMotivo: ${feito.motivo}` : ""}`} />}
      <p className="text-center text-sm text-slate-500">Você já pode fechar esta página.</p>
    </Casca>
  );
  if (isLoading) return <Casca><p className="text-slate-400">Carregando…</p></Casca>;
  if (isError || !data) return <Casca><Aviso tipo="erro" titulo="Link inválido ou expirado" texto="Peça um novo link de aprovação à equipe da obra." /></Casca>;

  const jaAprovado = data.status === "Aprovado";
  const jaRevisao = data.status === "RevisaoSolicitada" || data.status === "EmRevisao";
  const pendente = data.status === "Enviado";

  return (
    <Casca>
      <header className="mb-4">
        <p className="text-xs uppercase tracking-wide text-sky-400">Aprovação de RDO</p>
        <h1 className="text-xl font-bold">{data.obra.nome ?? "Obra"}</h1>
        <p className="text-sm text-slate-400">
          {data.obra.contrato ? `Contrato ${data.obra.contrato}` : "Sem contrato"}
          {data.obra.cliente ? ` · ${data.obra.cliente}` : ""}{data.obra.local ? ` · ${data.obra.local}` : ""}
        </p>
      </header>

      {jaAprovado && <Aviso tipo="ok" titulo="RDO aprovado ✓" texto={`Aprovado por ${data.aprovadoPor ?? "—"}${data.aprovadoEm ? ` em ${new Date(data.aprovadoEm).toLocaleString("pt-BR")}` : ""}.`} />}
      {jaRevisao && <Aviso tipo="alerta" titulo="Revisão solicitada" texto={data.motivoRevisao ?? "Aguardando correção da equipe."} />}

      <div className="rounded-xl bg-slate-800 p-4 mb-4 space-y-1 text-sm">
        <Linha rotulo="RDO" valor={`Nº ${data.numero}${data.revisao > 0 ? ` · rev.${data.revisao}` : ""}`} />
        <Linha rotulo="Data" valor={`${new Date(data.data).toLocaleDateString("pt-BR")}${data.turno ? ` · ${data.turno}` : ""}`} />
        <Linha rotulo="Responsável" valor={data.responsavel ?? "—"} />
        <Linha rotulo="Status" valor={data.status} />
      </div>

      {data.servicos.length > 0 && (
        <Bloco titulo="Serviços / avanço">
          <ul className="space-y-2">
            {data.servicos.map((s, i) => (
              <li key={i}>
                <div className="flex justify-between text-sm">
                  <span>{s.atividade || s.item || "—"}</span>
                  <span className="text-slate-400">{s.pct}%</span>
                </div>
                <div className="h-1.5 rounded-full bg-slate-700 mt-1">
                  <div className="h-1.5 rounded-full bg-sky-500" style={{ width: `${Math.min(100, s.pct)}%` }} />
                </div>
              </li>
            ))}
          </ul>
        </Bloco>
      )}

      {data.efetivo.length > 0 && (
        <Bloco titulo={`Efetivo (${data.efetivo.reduce((n, e) => n + e.quantidade, 0)})`}>
          <ul className="text-sm text-slate-300 space-y-0.5">
            {data.efetivo.map((e, i) => <li key={i} className="flex justify-between"><span>{e.funcao || "—"}</span><span className="text-slate-400">{e.quantidade}{e.horaExtra ? ` · HE ${e.horaExtra}` : ""}</span></li>)}
          </ul>
        </Bloco>
      )}

      {data.paralisacoes.length > 0 && (
        <Bloco titulo="Paralisações">
          <ul className="text-sm text-slate-300 space-y-0.5">
            {data.paralisacoes.map((p, i) => <li key={i}>{p.inicio}–{p.fim} · {p.motivo || p.descricao || "—"}</li>)}
          </ul>
        </Bloco>
      )}

      {data.ocorrencias && <Bloco titulo="Ocorrências"><p className="text-sm text-slate-300 whitespace-pre-wrap">{data.ocorrencias}</p></Bloco>}

      <a href={urlPublica(`/api/v1/aprovacao/${token}/pdf`)} target="_blank" rel="noreferrer"
        className="block text-center rounded-lg bg-slate-700 py-2.5 text-sm font-medium hover:bg-slate-600 mb-5">
        Abrir PDF completo
      </a>

      {pendente && (
        <div className="rounded-xl bg-slate-800 p-4 space-y-3">
          <input className="w-full rounded-lg bg-slate-900 px-3 py-2" placeholder="Seu nome *" value={nome} onChange={(e) => setNome(e.target.value)} />
          {modo === "revisar" && (
            <textarea className="w-full rounded-lg bg-slate-900 px-3 py-2" rows={3} placeholder="O que precisa ser corrigido? *" value={motivo} onChange={(e) => setMotivo(e.target.value)} />
          )}
          {erro && <p className="text-sm text-red-400">{erro}</p>}
          {modo === "ver" ? (
            <div className="grid grid-cols-2 gap-2">
              <button
                onClick={() => { if (!nome.trim()) { setErro("Informe seu nome."); return; } aprovar.mutate(); }}
                disabled={aprovar.isPending}
                className="rounded-lg bg-emerald-600 py-2.5 font-semibold hover:bg-emerald-500 disabled:opacity-50">
                {aprovar.isPending ? "Aprovando…" : "Aprovar"}
              </button>
              <button onClick={() => { setErro(null); setModo("revisar"); }} className="rounded-lg bg-amber-600 py-2.5 font-semibold hover:bg-amber-500">
                Solicitar revisão
              </button>
            </div>
          ) : (
            <div className="grid grid-cols-2 gap-2">
              <button onClick={() => { setErro(null); setModo("ver"); }} className="rounded-lg bg-slate-700 py-2.5 font-medium hover:bg-slate-600">
                Voltar
              </button>
              <button
                onClick={() => { if (!nome.trim()) { setErro("Informe seu nome."); return; } if (!motivo.trim()) { setErro("Descreva o que revisar."); return; } solicitar.mutate(); }}
                disabled={solicitar.isPending}
                className="rounded-lg bg-amber-600 py-2.5 font-semibold hover:bg-amber-500 disabled:opacity-50">
                {solicitar.isPending ? "Enviando…" : "Enviar pedido"}
              </button>
            </div>
          )}
        </div>
      )}
    </Casca>
  );
}

function Casca({ children }: { children: React.ReactNode }) {
  return (
    <main className="min-h-screen bg-slate-900 text-slate-100">
      <div className="mx-auto max-w-lg px-4 py-6">
        <p className="text-center text-xs text-slate-500 mb-4">Montaris</p>
        {children}
      </div>
    </main>
  );
}

function Bloco({ titulo, children }: { titulo: string; children: React.ReactNode }) {
  return (
    <section className="rounded-xl bg-slate-800 p-4 mb-4">
      <h2 className="text-sm font-semibold text-slate-300 mb-2">{titulo}</h2>
      {children}
    </section>
  );
}

function Linha({ rotulo, valor }: { rotulo: string; valor: string }) {
  return <div className="flex justify-between"><span className="text-slate-400">{rotulo}</span><span className="font-medium">{valor}</span></div>;
}

function Aviso({ tipo, titulo, texto }: { tipo: "ok" | "alerta" | "erro"; titulo: string; texto: string }) {
  const cor = tipo === "ok" ? "bg-emerald-900/40 border-emerald-700 text-emerald-200"
    : tipo === "alerta" ? "bg-amber-900/40 border-amber-700 text-amber-200"
    : "bg-red-900/40 border-red-700 text-red-200";
  return (
    <div className={`rounded-xl border p-4 mb-4 ${cor}`}>
      <p className="font-semibold">{titulo}</p>
      <p className="text-sm mt-0.5 whitespace-pre-wrap">{texto}</p>
    </div>
  );
}
