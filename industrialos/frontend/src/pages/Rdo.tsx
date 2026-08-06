import { useEffect, useRef, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { api, apiBlob, apiUpload, type Midia, type ObraItem, type RdoDetalhe, type Efetivo, type Paralisacao, type Recurso, type Servico, type Retrabalho, type Pendencia } from "../lib/api";

const CLIMAS = ["Ensolarado", "Parcialmente Nublado", "Chuva Fraca", "Chuva Forte", "Neblina", "Vento"];

// "HH:MM" -> minutos
function hm(t?: string): number | null {
  if (!t || !/^\d{1,2}:\d{2}$/.test(t)) return null;
  const [h, m] = t.split(":").map(Number);
  return h * 60 + m;
}
const fmt = (min: number) => `${Math.floor(min / 60)}h${String(Math.round(min % 60)).padStart(2, "0")}`;
const FIM_EXPEDIENTE = 16 * 60 + 48; // 16:48

type Jornada = { inicio?: string; almoco?: string; retorno?: string; termino?: string; feriado?: boolean };
type CalcHoras = { dayType: string; trabalhado: number; almoco: number; extraUtil: number; extra100: number; extra150: number } | null;

// Calcula horas do dia a partir da jornada única do RDO e do tipo de dia.
function calcHoras(j: Jornada, dataStr: string): CalcHoras {
  const ini = hm(j.inicio), alm = hm(j.almoco), ret = hm(j.retorno), ter = hm(j.termino);
  if (ini == null || ter == null || ter <= ini) return null;
  const almoco = alm != null && ret != null && ret > alm ? ret - alm : 0;
  const trabalhado = Math.max(0, ter - ini - almoco);

  const dow = new Date(dataStr + "T00:00:00").getDay(); // 0 dom, 6 sáb
  const dayType = j.feriado ? "Feriado" : dow === 0 ? "Domingo" : dow === 6 ? "Sábado" : "Útil";

  let extraUtil = 0, extra100 = 0, extra150 = 0;
  if (dayType === "Útil") {
    extraUtil = Math.max(0, ter - FIM_EXPEDIENTE); // hora extra = tudo trabalhado após 16:48
  } else {
    extra100 = Math.min(trabalhado, 480);        // até 8h
    extra150 = Math.max(0, trabalhado - 480);    // acima de 8h
  }
  return { dayType, trabalhado, almoco, extraUtil, extra100, extra150 };
}

// Mapeia o código WMO do Open-Meteo para uma das condições do RDO.
function mapWeather(code: number): string | null {
  if (code === 0) return "Ensolarado";
  if (code <= 3) return "Parcialmente Nublado";
  if (code === 45 || code === 48) return "Neblina";
  if ([51, 53, 55, 56, 57, 61, 80].includes(code)) return "Chuva Fraca";
  if ([63, 65, 66, 67, 81, 82, 95, 96, 99].includes(code)) return "Chuva Forte";
  return null;
}
const STATUS_SERV = ["Nao iniciado", "Em andamento", "Em espera", "Concluido"];

type Seguranca = { dds: boolean; apr: boolean; pt: boolean; areaIsolada: boolean; epis: boolean; ferramentas: boolean; observacoes: string };
type ProximoDia = { maoObra: string; equipamentos: string; materiais: string; ferramentas: string; pendencias: Pendencia[] };
type Planejamento = { servicos: string; prioridades: string; areas: string; observacoes: string };
type Assinatura = { nome?: string; img?: string };
type Assinaturas = { encarregado: Assinatura; fiscal: Assinatura; supervisor?: Assinatura };

type Form = {
  data: string; turno: string; ocorrencias: string;
  clima: { condicoes: string[]; temperatura?: number };
  jornada: { inicio?: string; almoco?: string; retorno?: string; termino?: string; feriado?: boolean };
  efetivo: Efetivo[]; paralisacoes: Paralisacao[]; recursos: Recurso[]; servicos: Servico[];
  retrabalho: Retrabalho[]; dificuldades: { descricao: string };
  proximoDia: ProximoDia; planejamento: Planejamento; seguranca: Seguranca;
  assinaturas: Assinaturas;
};

const SEG_VAZIA: Seguranca = { dds: false, apr: false, pt: false, areaIsolada: false, epis: false, ferramentas: false, observacoes: "" };
const PROX_VAZIO: ProximoDia = { maoObra: "", equipamentos: "", materiais: "", ferramentas: "", pendencias: [] };
const PLAN_VAZIO: Planejamento = { servicos: "", prioridades: "", areas: "", observacoes: "" };

export default function Rdo({ obraId, rdoId, onClose }: { obraId: string; rdoId: string; onClose: () => void }) {
  const qc = useQueryClient();
  const [form, setForm] = useState<Form | null>(null);
  const [numero, setNumero] = useState<number>(0);
  const [status, setStatus] = useState<string>("Rascunho");
  const [token, setToken] = useState<string | null>(null);
  const [motivoRevisao, setMotivoRevisao] = useState<string | null>(null);
  const [aprovadoPor, setAprovadoPor] = useState<string | null>(null);
  const [linkCopiado, setLinkCopiado] = useState(false);
  const [salvo, setSalvo] = useState<"" | "salvando" | "salvo" | "erro">("");
  const [resumo, setResumo] = useState<{ texto: string; origem: string } | null>(null);
  const [gerandoResumo, setGerandoResumo] = useState(false);
  const [buscandoClima, setBuscandoClima] = useState(false);
  const primeiraCarga = useRef(true);

  const { data: itens } = useQuery({ queryKey: ["itens", obraId], queryFn: () => api<ObraItem[]>(`/api/v1/obras/${obraId}/itens`) });
  const { data: funcoes } = useQuery({ queryKey: ["funcoes"], queryFn: () => api<{ id: string; nome: string }[]>("/api/v1/funcoes") });
  const { data: equipamentos } = useQuery({ queryKey: ["equipamentos"], queryFn: () => api<{ id: string; nome: string }[]>("/api/v1/equipamentos") });

  // carrega o RDO
  useEffect(() => {
    api<RdoDetalhe>(`/api/v1/rdos/${rdoId}`).then((r) => {
      setNumero(r.numero); setStatus(r.status);
      setToken(r.tokenAprovacao ?? null);
      setMotivoRevisao(r.motivoRevisao ?? null);
      setAprovadoPor(r.aprovadoPor ?? null);
      setForm({
        data: r.data, turno: r.turno ?? "", ocorrencias: r.ocorrencias ?? "",
        clima: { condicoes: r.clima?.condicoes ?? [], temperatura: r.clima?.temperatura },
        jornada: r.jornada ?? {},
        efetivo: r.efetivo ?? [], paralisacoes: r.paralisacoes ?? [], recursos: r.recursos ?? [], servicos: r.servicos ?? [],
        retrabalho: r.retrabalho ?? [],
        dificuldades: { descricao: r.dificuldades?.descricao ?? "" },
        proximoDia: { ...PROX_VAZIO, ...(r.proximoDia ?? {}), pendencias: r.proximoDia?.pendencias ?? [] },
        planejamento: { ...PLAN_VAZIO, ...(r.planejamento ?? {}) },
        seguranca: { ...SEG_VAZIA, ...(r.seguranca ?? {}) },
        assinaturas: {
          encarregado: r.assinaturas?.encarregado ?? {},
          fiscal: r.assinaturas?.fiscal ?? {},
          supervisor: r.assinaturas?.supervisor,
        },
      });
    });
  }, [rdoId]);

  // autosave (debounce 800ms)
  useEffect(() => {
    if (!form) return;
    if (primeiraCarga.current) { primeiraCarga.current = false; return; }
    setSalvo("salvando");
    const t = setTimeout(async () => {
      try {
        await api(`/api/v1/rdos/${rdoId}`, { method: "PUT", body: JSON.stringify({
          data: form.data, turno: form.turno, ocorrencias: form.ocorrencias,
          clima: form.clima, jornada: form.jornada,
          dificuldades: form.dificuldades, proximoDia: form.proximoDia,
          planejamento: form.planejamento, seguranca: form.seguranca, assinaturas: form.assinaturas,
          efetivo: form.efetivo, paralisacoes: form.paralisacoes, recursos: form.recursos,
          servicos: form.servicos, retrabalho: form.retrabalho,
        }) });
        setSalvo("salvo");
        qc.invalidateQueries({ queryKey: ["rdos", obraId] });
      } catch { setSalvo("erro"); }
    }, 800);
    return () => clearTimeout(t);
  }, [form, rdoId, obraId, qc]);

  if (!form) return <p className="text-slate-400 p-4">Carregando RDO…</p>;

  const up = (patch: Partial<Form>) => setForm({ ...form, ...patch });
  const toggleClima = (c: string) => up({ clima: { ...form.clima, condicoes: form.clima.condicoes.includes(c) ? form.clima.condicoes.filter((x) => x !== c) : [...form.clima.condicoes, c] } });

  const bloqueado = status === "Aprovado";

  async function finalizar() {
    const r = await api<{ status: string; tokenAprovacao?: string }>(`/api/v1/rdos/${rdoId}/finalizar`, { method: "POST" });
    setStatus(r.status);
    setToken(r.tokenAprovacao ?? null);
    setMotivoRevisao(null);
    qc.invalidateQueries({ queryKey: ["rdos", obraId] });
  }

  const linkAprovacao = token ? `${window.location.origin}/aprovacao/${token}` : null;
  async function copiarLink() {
    if (!linkAprovacao) return;
    try { await navigator.clipboard.writeText(linkAprovacao); setLinkCopiado(true); setTimeout(() => setLinkCopiado(false), 2000); }
    catch { /* clipboard indisponivel — o usuario copia manualmente do campo */ }
  }

  async function abrirPdf() {
    const blob = await apiBlob(`/api/v1/rdos/${rdoId}/pdf`);
    window.open(URL.createObjectURL(blob), "_blank");
  }

  async function gerarResumo() {
    setGerandoResumo(true);
    try { setResumo(await api<{ texto: string; origem: string }>(`/api/v1/rdos/${rdoId}/resumo`)); }
    catch { setResumo({ texto: "Não foi possível gerar o resumo.", origem: "erro" }); }
    finally { setGerandoResumo(false); }
  }

  function buscarClima() {
    if (!navigator.geolocation) { alert("GPS não disponível neste dispositivo."); return; }
    setBuscandoClima(true);
    navigator.geolocation.getCurrentPosition(
      async (pos) => {
        try {
          const { latitude, longitude } = pos.coords;
          const r = await fetch(`https://api.open-meteo.com/v1/forecast?latitude=${latitude}&longitude=${longitude}&current=temperature_2m,weather_code`);
          const j = await r.json();
          const temp = Math.round(j.current.temperature_2m);
          const cond = mapWeather(j.current.weather_code);
          setForm((f) => f && ({ ...f, clima: { condicoes: cond ? [cond] : f.clima.condicoes, temperatura: temp } }));
        } finally { setBuscandoClima(false); }
      },
      () => { setBuscandoClima(false); alert("Não foi possível obter a localização."); },
      { enableHighAccuracy: true, timeout: 10000 }
    );
  }

  return (
    <div className="space-y-5">
      <datalist id="funcoes-list">
        {funcoes?.map((f) => <option key={f.id} value={f.nome} />)}
      </datalist>
      <datalist id="equipamentos-list">
        {equipamentos?.map((e) => <option key={e.id} value={e.nome} />)}
      </datalist>

      <div className="flex items-center justify-between sticky top-0 bg-slate-900 py-2 z-10">
        <button onClick={onClose} className="text-sky-400 text-sm">‹ Voltar</button>
        <span className="font-semibold">RDO {numero} · {status}</span>
        <div className="flex items-center gap-3">
          <span className={`text-xs ${salvo === "erro" ? "text-red-400" : "text-emerald-400"}`}>
            {salvo === "salvando" ? "Salvando…" : salvo === "salvo" ? "Salvo ✓" : salvo === "erro" ? "Erro ao salvar" : ""}
          </span>
          <button onClick={gerarResumo} disabled={gerandoResumo} className="rounded-lg bg-slate-700 px-3 py-1 text-xs hover:bg-slate-600 disabled:opacity-50">
            {gerandoResumo ? "Gerando…" : "Resumo do dia"}
          </button>
          <button onClick={abrirPdf} className="rounded-lg bg-slate-700 px-3 py-1 text-xs hover:bg-slate-600">Abrir PDF</button>
        </div>
      </div>

      {resumo && (
        <div className="rounded-xl border border-slate-700 bg-slate-800/60 p-4">
          <div className="mb-1 flex items-center justify-between">
            <span className="text-sm font-semibold">Resumo automático do dia</span>
            <span className="text-[10px] uppercase tracking-wide text-slate-500">
              {resumo.origem === "regras" ? "gerado por regras" : resumo.origem === "ia" ? "gerado por IA" : "—"}
            </span>
          </div>
          <p className="text-sm text-slate-200 whitespace-pre-wrap">{resumo.texto}</p>
          <p className="mt-1 text-[10px] text-slate-500">Resumo automático — por ora por regras; IA entra depois sem mudar a tela.</p>
        </div>
      )}

      {/* Identificação */}
      <Secao titulo="Identificação">
        <div className="grid grid-cols-2 gap-3">
          <Campo label="Data"><input type="date" value={form.data} onChange={(e) => up({ data: e.target.value })} className={inp} disabled={bloqueado} /></Campo>
          <Campo label="Turno">
            <select value={form.turno} onChange={(e) => up({ turno: e.target.value })} className={inp} disabled={bloqueado}>
              <option value="">—</option><option>Diurno</option><option>Noturno</option>
            </select>
          </Campo>
        </div>
      </Secao>

      {/* Clima */}
      <Secao titulo="Clima">
        {!bloqueado && (
          <button type="button" onClick={buscarClima} disabled={buscandoClima}
            className="rounded-lg bg-slate-700 px-3 py-1.5 text-sm hover:bg-slate-600 disabled:opacity-50">
            {buscandoClima ? "Buscando…" : "📍 Buscar clima automático (GPS)"}
          </button>
        )}
        <div className="flex flex-wrap gap-2">
          {CLIMAS.map((c) => (
            <button key={c} type="button" disabled={bloqueado} onClick={() => toggleClima(c)}
              className={`rounded-full px-3 py-1 text-sm ${form.clima.condicoes.includes(c) ? "bg-sky-600" : "bg-slate-700"}`}>{c}</button>
          ))}
        </div>
        <Campo label="Temperatura (°C)">
          <input type="number" value={form.clima.temperatura ?? ""} disabled={bloqueado}
            onChange={(e) => up({ clima: { ...form.clima, temperatura: e.target.value ? Number(e.target.value) : undefined } })} className={inp} />
        </Campo>
      </Secao>

      {/* Jornada (única do RDO) — calcula horas e extras automaticamente */}
      <Secao titulo="Jornada">
        <p className="text-xs text-slate-400">Expediente padrão: seg–sex 07:00–16:48 (8h48). Preencha a hora real da frente.</p>
        <div className="grid grid-cols-2 gap-3">
          {(["inicio", "almoco", "retorno", "termino"] as const).map((k) => (
            <Campo key={k} label={({ inicio: "Início", almoco: "Saída almoço", retorno: "Retorno almoço", termino: "Término" })[k]}>
              <input type="time" value={form.jornada[k] ?? ""} disabled={bloqueado}
                onChange={(e) => up({ jornada: { ...form.jornada, [k]: e.target.value } })} className={inp} />
            </Campo>
          ))}
        </div>
        <label className="flex items-center gap-2 text-sm">
          <input type="checkbox" checked={!!form.jornada.feriado} disabled={bloqueado}
            onChange={(e) => up({ jornada: { ...form.jornada, feriado: e.target.checked } })} />
          É feriado
        </label>
        {(() => {
          const h = calcHoras(form.jornada, form.data);
          if (!h) return null;
          return (
            <div className="rounded-lg bg-slate-900 p-3 text-sm space-y-1">
              <div className="flex justify-between"><span className="text-slate-400">Dia</span><span>{h.dayType}</span></div>
              <div className="flex justify-between"><span className="text-slate-400">Horas trabalhadas</span><span className="font-semibold">{fmt(h.trabalhado)}</span></div>
              {h.dayType === "Útil" && h.extraUtil > 0 && (
                <div className="flex justify-between text-amber-400"><span>Hora extra (após 16:48)</span><span>{fmt(h.extraUtil)} <span className="text-xs text-slate-400">· 50%/70% no fech. semanal</span></span></div>
              )}
              {h.dayType !== "Útil" && (
                <>
                  <div className="flex justify-between text-amber-400"><span>Extra 100% (até 8h)</span><span>{fmt(h.extra100)}</span></div>
                  {h.extra150 > 0 && <div className="flex justify-between text-red-400"><span>Extra 150% (acima 8h)</span><span>{fmt(h.extra150)}</span></div>}
                </>
              )}
            </div>
          );
        })()}
      </Secao>

      {/* Efetivo */}
      <ListaSecao titulo="Efetivo" itens={form.efetivo} disabled={bloqueado}
        novo={(): Efetivo => ({ funcao: "", quantidade: 1 })}
        onChange={(efetivo) => up({ efetivo })}
        render={(e, set) => (
          <div className="grid grid-cols-3 gap-2">
            <input list="funcoes-list" placeholder="Função" value={e.funcao ?? ""} onChange={(ev) => set({ ...e, funcao: ev.target.value })} className={inp} disabled={bloqueado} />
            <input type="number" placeholder="Qtd" value={e.quantidade} onChange={(ev) => set({ ...e, quantidade: Number(ev.target.value) })} className={inp} disabled={bloqueado} />
            <input placeholder="Obs" value={e.obs ?? ""} onChange={(ev) => set({ ...e, obs: ev.target.value })} className={inp} disabled={bloqueado} />
          </div>
        )} />

      {/* Paralisações */}
      <ListaSecao titulo="Paralisações" itens={form.paralisacoes} disabled={bloqueado}
        novo={(): Paralisacao => ({})}
        onChange={(paralisacoes) => up({ paralisacoes })}
        render={(p, set) => (
          <div className="grid grid-cols-3 gap-2">
            <input type="time" value={p.inicio ?? ""} onChange={(ev) => set({ ...p, inicio: ev.target.value })} className={inp} disabled={bloqueado} />
            <input type="time" value={p.fim ?? ""} onChange={(ev) => set({ ...p, fim: ev.target.value })} className={inp} disabled={bloqueado} />
            <input placeholder="Motivo" value={p.motivo ?? ""} onChange={(ev) => set({ ...p, motivo: ev.target.value })} className={inp} disabled={bloqueado} />
          </div>
        )} />

      {/* Recursos */}
      <ListaSecao titulo="Recursos / Equipamentos" itens={form.recursos} disabled={bloqueado}
        novo={(): Recurso => ({ quantidade: 1 })}
        onChange={(recursos) => up({ recursos })}
        render={(r, set) => (
          <div className="grid grid-cols-3 gap-2">
            <input list="equipamentos-list" placeholder="Equipamento" value={r.equipamento ?? ""} onChange={(ev) => set({ ...r, equipamento: ev.target.value })} className={inp} disabled={bloqueado} />
            <input type="number" placeholder="Qtd" value={r.quantidade} onChange={(ev) => set({ ...r, quantidade: Number(ev.target.value) })} className={inp} disabled={bloqueado} />
            <input placeholder="Horas" value={r.horas ?? ""} onChange={(ev) => set({ ...r, horas: ev.target.value })} className={inp} disabled={bloqueado} />
          </div>
        )} />

      {/* Serviços (vínculo EAP + avanço) */}
      <ListaSecao titulo="Serviços (avanço)" itens={form.servicos} disabled={bloqueado}
        novo={(): Servico => ({ status: "Em andamento" })}
        onChange={(servicos) => up({ servicos })}
        render={(s, set) => (
          <div className="space-y-2">
            <select value={s.obraItemId ?? ""} onChange={(ev) => set({ ...s, obraItemId: ev.target.value || null })} className={inp} disabled={bloqueado}>
              <option value="">Serviço extra (fora do escopo)</option>
              {itens?.map((i) => <option key={i.id} value={i.id}>{i.descricao}</option>)}
            </select>
            <input placeholder="Atividade" value={s.atividade ?? ""} onChange={(ev) => set({ ...s, atividade: ev.target.value })} className={inp} disabled={bloqueado} />
            <div className="grid grid-cols-3 gap-2">
              <select value={s.status ?? ""} onChange={(ev) => set({ ...s, status: ev.target.value })} className={inp} disabled={bloqueado}>
                {STATUS_SERV.map((st) => <option key={st}>{st}</option>)}
              </select>
              <input type="number" placeholder="Qtd exec" value={s.qtdExec ?? ""} onChange={(ev) => set({ ...s, qtdExec: ev.target.value ? Number(ev.target.value) : null })} className={inp} disabled={bloqueado} />
              <input type="number" placeholder="% inform." value={s.pctInformado ?? ""} onChange={(ev) => set({ ...s, pctInformado: ev.target.value ? Number(ev.target.value) : null })} className={inp} disabled={bloqueado} />
            </div>
            {s.pctItem != null && <p className="text-xs text-emerald-400">Avanço calculado: {Math.round((s.pctItem ?? 0) * 100)}%</p>}
          </div>
        )} />

      {/* Retrabalho */}
      <ListaSecao titulo="Retrabalho" itens={form.retrabalho} disabled={bloqueado}
        novo={(): Retrabalho => ({ pessoas: 1 })}
        onChange={(retrabalho) => up({ retrabalho })}
        render={(r, set) => (
          <div className="space-y-2">
            <input placeholder="Atividade" value={r.atividade ?? ""} onChange={(e) => set({ ...r, atividade: e.target.value })} className={inp} disabled={bloqueado} />
            <div className="grid grid-cols-3 gap-2">
              <input placeholder="Local" value={r.local ?? ""} onChange={(e) => set({ ...r, local: e.target.value })} className={inp} disabled={bloqueado} />
              <input type="number" placeholder="Pessoas" value={r.pessoas} onChange={(e) => set({ ...r, pessoas: Number(e.target.value) })} className={inp} disabled={bloqueado} />
              <input type="number" placeholder="Horas" value={r.horas ?? ""} onChange={(e) => set({ ...r, horas: e.target.value ? Number(e.target.value) : null })} className={inp} disabled={bloqueado} />
            </div>
            {r.pessoas > 0 && r.horas ? <p className="text-xs text-red-400">HH perdido: {(r.pessoas * (r.horas ?? 0)).toLocaleString("pt-BR")}</p> : null}
            <div className="grid grid-cols-2 gap-2">
              <input placeholder="Causa" value={r.causa ?? ""} onChange={(e) => set({ ...r, causa: e.target.value })} className={inp} disabled={bloqueado} />
              <input placeholder="Origem" value={r.origem ?? ""} onChange={(e) => set({ ...r, origem: e.target.value })} className={inp} disabled={bloqueado} />
            </div>
            <input placeholder="Ação corretiva" value={r.acaoCorretiva ?? ""} onChange={(e) => set({ ...r, acaoCorretiva: e.target.value })} className={inp} disabled={bloqueado} />
          </div>
        )} />

      {/* Segurança */}
      <Secao titulo="Segurança">
        <div className="grid grid-cols-2 gap-2">
          {([["dds", "DDS realizado"], ["apr", "APR emitida"], ["pt", "PT emitida"], ["areaIsolada", "Área isolada"], ["epis", "EPIs OK"], ["ferramentas", "Ferramentas inspecionadas"]] as const).map(([k, label]) => (
            <label key={k} className="flex items-center gap-2 text-sm">
              <input type="checkbox" checked={form.seguranca[k]} disabled={bloqueado}
                onChange={(e) => up({ seguranca: { ...form.seguranca, [k]: e.target.checked } })} />
              {label}
            </label>
          ))}
        </div>
        <textarea placeholder="Observações de segurança" value={form.seguranca.observacoes} disabled={bloqueado}
          onChange={(e) => up({ seguranca: { ...form.seguranca, observacoes: e.target.value } })} className={ta} />
      </Secao>

      {/* Ocorrências */}
      <Secao titulo="Ocorrências">
        <textarea placeholder="Descreva os eventos relevantes do dia…" value={form.ocorrencias} disabled={bloqueado}
          onChange={(e) => up({ ocorrencias: e.target.value })} className={ta} />
      </Secao>

      {/* Dificuldades */}
      <Secao titulo="Dificuldades">
        <textarea placeholder="Dificuldades encontradas…" value={form.dificuldades.descricao} disabled={bloqueado}
          onChange={(e) => up({ dificuldades: { descricao: e.target.value } })} className={ta} />
      </Secao>

      {/* Próximo Dia */}
      <Secao titulo="Recursos para o próximo dia">
        <div className="grid gap-2 sm:grid-cols-2">
          <Campo label="Mão de obra"><textarea value={form.proximoDia.maoObra} disabled={bloqueado} onChange={(e) => up({ proximoDia: { ...form.proximoDia, maoObra: e.target.value } })} className={ta} /></Campo>
          <Campo label="Equipamentos"><textarea value={form.proximoDia.equipamentos} disabled={bloqueado} onChange={(e) => up({ proximoDia: { ...form.proximoDia, equipamentos: e.target.value } })} className={ta} /></Campo>
          <Campo label="Materiais"><textarea value={form.proximoDia.materiais} disabled={bloqueado} onChange={(e) => up({ proximoDia: { ...form.proximoDia, materiais: e.target.value } })} className={ta} /></Campo>
          <Campo label="Ferramentas"><textarea value={form.proximoDia.ferramentas} disabled={bloqueado} onChange={(e) => up({ proximoDia: { ...form.proximoDia, ferramentas: e.target.value } })} className={ta} /></Campo>
        </div>
      </Secao>

      {/* Pendências */}
      <ListaSecao titulo="Pendências" itens={form.proximoDia.pendencias} disabled={bloqueado}
        novo={(): Pendencia => ({ status: "Aberta" })}
        onChange={(pendencias) => up({ proximoDia: { ...form.proximoDia, pendencias } })}
        render={(p, set) => (
          <div className="grid grid-cols-2 gap-2">
            <input placeholder="Pendência" value={p.descricao ?? ""} onChange={(e) => set({ ...p, descricao: e.target.value })} className={inp} disabled={bloqueado} />
            <input placeholder="Responsável" value={p.responsavel ?? ""} onChange={(e) => set({ ...p, responsavel: e.target.value })} className={inp} disabled={bloqueado} />
            <input type="date" value={p.prazo ?? ""} onChange={(e) => set({ ...p, prazo: e.target.value })} className={inp} disabled={bloqueado} />
            <select value={p.status ?? "Aberta"} onChange={(e) => set({ ...p, status: e.target.value })} className={inp} disabled={bloqueado}>
              <option>Aberta</option><option>Em andamento</option><option>Concluída</option>
            </select>
          </div>
        )} />

      {/* Planejamento */}
      <Secao titulo="Planejamento do próximo dia">
        <Campo label="Serviços"><textarea value={form.planejamento.servicos} disabled={bloqueado} onChange={(e) => up({ planejamento: { ...form.planejamento, servicos: e.target.value } })} className={ta} /></Campo>
        <div className="grid gap-2 sm:grid-cols-2">
          <Campo label="Prioridades"><textarea value={form.planejamento.prioridades} disabled={bloqueado} onChange={(e) => up({ planejamento: { ...form.planejamento, prioridades: e.target.value } })} className={ta} /></Campo>
          <Campo label="Áreas / frentes"><textarea value={form.planejamento.areas} disabled={bloqueado} onChange={(e) => up({ planejamento: { ...form.planejamento, areas: e.target.value } })} className={ta} /></Campo>
        </div>
      </Secao>

      <SecaoFotos rdoId={rdoId} disabled={bloqueado} />

      {/* Assinaturas */}
      <Secao titulo="Assinaturas">
        <div className="grid gap-4 sm:grid-cols-2">
          <SignaturePad label="Encarregado" value={form.assinaturas.encarregado} disabled={bloqueado}
            onChange={(a) => up({ assinaturas: { ...form.assinaturas, encarregado: a } })} />
          <SignaturePad label="Fiscal" value={form.assinaturas.fiscal} disabled={bloqueado}
            onChange={(a) => up({ assinaturas: { ...form.assinaturas, fiscal: a } })} />
          {form.assinaturas.supervisor !== undefined ? (
            <SignaturePad label="Supervisor" value={form.assinaturas.supervisor} disabled={bloqueado}
              onChange={(a) => up({ assinaturas: { ...form.assinaturas, supervisor: a } })} />
          ) : !bloqueado ? (
            <button type="button" onClick={() => up({ assinaturas: { ...form.assinaturas, supervisor: {} } })}
              className="rounded-lg bg-slate-700 px-3 py-2 text-sm self-start h-fit">+ Supervisor</button>
          ) : null}
        </div>
      </Secao>

      {/* Revisao solicitada pelo fiscal — a equipe corrige e reenvia */}
      {status === "RevisaoSolicitada" && motivoRevisao && (
        <div className="rounded-xl border border-amber-700 bg-amber-900/40 p-4 text-amber-200">
          <p className="font-semibold">Revisão solicitada pelo fiscal</p>
          <p className="text-sm mt-0.5 whitespace-pre-wrap">{motivoRevisao}</p>
        </div>
      )}

      {status === "Aprovado" && (
        <div className="rounded-xl border border-emerald-700 bg-emerald-900/40 p-4 text-emerald-200">
          <p className="font-semibold">RDO aprovado ✓</p>
          {aprovadoPor && <p className="text-sm mt-0.5">Aprovado por {aprovadoPor}.</p>}
        </div>
      )}

      {/* Link de aprovacao para enviar ao fiscal (status Enviado, com token vigente) */}
      {status === "Enviado" && linkAprovacao && (
        <div className="rounded-xl bg-slate-800 p-4 space-y-2">
          <p className="text-sm font-semibold">Link de aprovação do fiscal</p>
          <p className="text-xs text-slate-400">Envie este link ao fiscal do cliente. Ele aprova ou pede revisão sem precisar de login.</p>
          <div className="flex gap-2">
            <input readOnly value={linkAprovacao} onFocus={(e) => e.currentTarget.select()} className="flex-1 rounded-lg bg-slate-900 px-3 py-2 text-xs" />
            <button onClick={copiarLink} className="rounded-lg bg-sky-600 px-3 py-2 text-sm font-semibold hover:bg-sky-500">
              {linkCopiado ? "Copiado ✓" : "Copiar"}
            </button>
          </div>
        </div>
      )}

      {!bloqueado && (status === "Rascunho" || status === "RevisaoSolicitada") && (
        <button onClick={finalizar} className="w-full rounded-lg bg-emerald-600 py-3 font-semibold hover:bg-emerald-500">
          {status === "RevisaoSolicitada" ? "Reenviar para aprovação" : "Finalizar e enviar"}
        </button>
      )}
      {status === "Enviado" && <p className="text-center text-sm text-slate-400">Aguardando aprovação do fiscal.</p>}
    </div>
  );
}

function SecaoFotos({ rdoId, disabled }: { rdoId: string; disabled?: boolean }) {
  const qc = useQueryClient();
  const fileRef = useRef<HTMLInputElement>(null);
  const { data: fotos } = useQuery({ queryKey: ["midia", rdoId], queryFn: () => api<Midia[]>(`/api/v1/rdos/${rdoId}/midia`) });

  const enviar = useMutation({
    mutationFn: (file: File) => { const f = new FormData(); f.append("file", file); return apiUpload(`/api/v1/rdos/${rdoId}/midia`, f); },
    onSuccess: () => qc.invalidateQueries({ queryKey: ["midia", rdoId] }),
  });
  const apagar = useMutation({
    mutationFn: (id: string) => api(`/api/v1/midia/${id}`, { method: "DELETE" }),
    onSuccess: () => qc.invalidateQueries({ queryKey: ["midia", rdoId] }),
  });

  return (
    <Secao titulo="Fotos">
      {!disabled && (
        <div className="flex items-center gap-2">
          <input
            ref={fileRef}
            type="file"
            accept="image/*,video/*"
            capture="environment"
            className="hidden"
            onChange={(e) => { const f = e.target.files?.[0]; if (f) enviar.mutate(f); e.target.value = ""; }}
          />
          <button onClick={() => fileRef.current?.click()} disabled={enviar.isPending}
            className="rounded-lg bg-sky-600 px-4 py-2 text-sm font-semibold hover:bg-sky-500 disabled:opacity-50">
            {enviar.isPending ? "Enviando…" : "📷 Tirar / anexar foto"}
          </button>
        </div>
      )}
      <div className="grid grid-cols-3 gap-2 sm:grid-cols-4">
        {fotos?.map((m) => (
          <div key={m.id} className="relative">
            {m.tipo === "video"
              ? <video src={m.url} className="h-24 w-full rounded-lg object-cover" controls />
              : <img src={m.url} alt="" className="h-24 w-full rounded-lg object-cover" />}
            {!disabled && (
              <button onClick={() => apagar.mutate(m.id)}
                className="absolute right-1 top-1 rounded-full bg-black/60 px-1.5 text-xs text-white">✕</button>
            )}
          </div>
        ))}
      </div>
      {fotos?.length === 0 && <p className="text-slate-500 text-sm">Nenhuma foto.</p>}
    </Secao>
  );
}

function SignaturePad({ label, value, onChange, disabled }: { label: string; value: Assinatura; onChange: (a: Assinatura) => void; disabled?: boolean }) {
  const canvasRef = useRef<HTMLCanvasElement>(null);
  const drawing = useRef(false);
  const loaded = useRef(false);

  useEffect(() => {
    const c = canvasRef.current; if (!c) return;
    const ctx = c.getContext("2d")!;
    ctx.lineWidth = 2; ctx.lineCap = "round"; ctx.strokeStyle = "#e2e8f0";
    if (value.img && !loaded.current) {
      loaded.current = true;
      const img = new Image();
      img.onload = () => ctx.drawImage(img, 0, 0, c.width, c.height);
      img.src = value.img;
    }
  }, [value.img]);

  const pos = (e: React.PointerEvent<HTMLCanvasElement>) => {
    const c = canvasRef.current!; const r = c.getBoundingClientRect();
    return { x: (e.clientX - r.left) * (c.width / r.width), y: (e.clientY - r.top) * (c.height / r.height) };
  };
  const start = (e: React.PointerEvent<HTMLCanvasElement>) => {
    if (disabled) return; drawing.current = true;
    const ctx = canvasRef.current!.getContext("2d")!; const p = pos(e); ctx.beginPath(); ctx.moveTo(p.x, p.y);
  };
  const move = (e: React.PointerEvent<HTMLCanvasElement>) => {
    if (!drawing.current) return; const ctx = canvasRef.current!.getContext("2d")!; const p = pos(e); ctx.lineTo(p.x, p.y); ctx.stroke();
  };
  const end = () => {
    if (!drawing.current) return; drawing.current = false;
    onChange({ ...value, img: canvasRef.current!.toDataURL("image/png") });
  };
  const limpar = () => {
    const c = canvasRef.current!; c.getContext("2d")!.clearRect(0, 0, c.width, c.height);
    onChange({ ...value, img: undefined });
  };

  return (
    <div className="space-y-1">
      <input placeholder={`Nome — ${label}`} value={value.nome ?? ""} disabled={disabled}
        onChange={(e) => onChange({ ...value, nome: e.target.value })} className={inp} />
      <canvas ref={canvasRef} width={300} height={110}
        onPointerDown={start} onPointerMove={move} onPointerUp={end} onPointerLeave={end}
        className="w-full rounded-lg bg-slate-900 border border-slate-700 touch-none" style={{ height: 110 }} />
      {!disabled && <button type="button" onClick={limpar} className="text-xs text-slate-400">Limpar {label.toLowerCase()}</button>}
    </div>
  );
}

const inp = "w-full rounded-lg bg-slate-900 px-3 py-2 text-sm";
const ta = "w-full rounded-lg bg-slate-900 px-3 py-2 text-sm min-h-[60px]";

function Secao({ titulo, children }: { titulo: string; children: React.ReactNode }) {
  return <section className="rounded-xl bg-slate-800 p-4 space-y-3"><h3 className="font-semibold text-sm text-slate-300">{titulo}</h3>{children}</section>;
}
function Campo({ label, children }: { label: string; children: React.ReactNode }) {
  return <label className="block text-xs text-slate-400 space-y-1"><span>{label}</span>{children}</label>;
}

function ListaSecao<T>({ titulo, itens, novo, onChange, render, disabled }: {
  titulo: string; itens: T[]; novo: () => T; onChange: (v: T[]) => void; disabled?: boolean;
  render: (item: T, set: (v: T) => void) => React.ReactNode;
}) {
  return (
    <Secao titulo={titulo}>
      <div className="space-y-3">
        {itens.map((it, i) => (
          <div key={i} className="flex gap-2 items-start">
            <div className="flex-1">{render(it, (v) => onChange(itens.map((x, j) => (j === i ? v : x))))}</div>
            {!disabled && <button onClick={() => onChange(itens.filter((_, j) => j !== i))} className="text-red-400 text-sm px-1">✕</button>}
          </div>
        ))}
        {itens.length === 0 && <p className="text-slate-500 text-sm">Nenhum item.</p>}
      </div>
      {!disabled && <button onClick={() => onChange([...itens, novo()])} className="mt-3 rounded-lg bg-slate-700 px-3 py-1.5 text-sm hover:bg-slate-600">+ Adicionar</button>}
    </Secao>
  );
}
