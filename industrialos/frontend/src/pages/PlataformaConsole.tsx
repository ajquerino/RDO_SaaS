import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { api, type MetricasPlataforma, type Plano, type TenantResumo } from "../lib/api";
import { useAuth } from "../store/auth";

const brl = (v?: number | null) => (v == null ? "—" : v.toLocaleString("pt-BR", { style: "currency", currency: "BRL" }));

// uso vs limite do plano da empresa (amarelo quando atinge). Sem limite = só a contagem.
function UsoLimite({ n, limite }: { n: number; limite?: number | null }) {
  return <span className={limite != null && n >= limite ? "text-amber-400 font-medium" : ""}>{n}{limite != null ? `/${limite}` : ""}</span>;
}

/** Console do dono da plataforma (SuperAdmin). NÃO mostra dados de nenhuma empresa por padrão —
 * só o que a API de plataforma expõe (contagens/uso), sempre via endpoints [Authorize(SuperAdmin)]. */
export default function PlataformaConsole() {
  const { usuario, logout } = useAuth();
  return (
    <main className="min-h-screen bg-slate-900 text-slate-100">
      <header className="flex items-center justify-between border-b border-slate-800 px-4 py-3">
        <div>
          <h1 className="text-lg font-bold">IndustrialOS · <span className="text-sky-400">Plataforma</span></h1>
          <p className="text-slate-400 text-xs">{usuario?.nome} · Console do SaaS</p>
        </div>
        <button onClick={logout} className="rounded-lg bg-slate-800 px-3 py-1.5 text-sm">Sair</button>
      </header>
      <section className="mx-auto max-w-4xl space-y-6 p-4">
        <Metricas />
        <Empresas />
        <Planos />
      </section>
    </main>
  );
}

function Metricas() {
  const { data } = useQuery({ queryKey: ["plataforma-metricas"], queryFn: () => api<MetricasPlataforma>("/api/v1/plataforma/metricas") });
  const cards = [
    { r: "Empresas", v: data ? `${data.tenantsAtivos}/${data.totalTenants}` : "—", s: "ativas / total" },
    { r: "Obras", v: data?.totalObras ?? "—", s: "no SaaS" },
    { r: "RDOs", v: data?.totalRdos ?? "—", s: "no SaaS" },
    { r: "Usuários", v: data?.totalUsuarios ?? "—", s: "de empresas" },
  ];
  return (
    <div className="grid grid-cols-2 gap-3 sm:grid-cols-4">
      {cards.map((c) => (
        <div key={c.r} className="rounded-xl bg-slate-800 p-3">
          <p className="text-xs text-slate-400">{c.r}</p>
          <p className="text-2xl font-bold leading-tight">{c.v}</p>
          <p className="text-xs text-slate-500">{c.s}</p>
        </div>
      ))}
    </div>
  );
}

function Empresas() {
  const qc = useQueryClient();
  const [nova, setNova] = useState(false);
  const [editando, setEditando] = useState<string | null>(null);
  const { data: tenants } = useQuery({ queryKey: ["plataforma-tenants"], queryFn: () => api<TenantResumo[]>("/api/v1/plataforma/tenants") });
  const { data: planos } = useQuery({ queryKey: ["plataforma-planos"], queryFn: () => api<Plano[]>("/api/v1/plataforma/planos") });

  const invalidar = () => { qc.invalidateQueries({ queryKey: ["plataforma-tenants"] }); qc.invalidateQueries({ queryKey: ["plataforma-metricas"] }); };

  const status = useMutation({
    mutationFn: ({ id, status }: { id: string; status: string }) =>
      api(`/api/v1/plataforma/tenants/${id}/status`, { method: "PUT", body: JSON.stringify({ status }) }),
    onSuccess: invalidar,
  });

  const editar = useMutation({
    mutationFn: ({ id, nome, cnpj }: { id: string; nome: string; cnpj: string | null }) =>
      api(`/api/v1/plataforma/tenants/${id}`, { method: "PUT", body: JSON.stringify({ nome, cnpj }) }),
    onSuccess: () => { setEditando(null); invalidar(); },
  });

  const excluir = useMutation({
    mutationFn: (id: string) => api(`/api/v1/plataforma/tenants/${id}`, { method: "DELETE" }),
    onSuccess: invalidar,
  });

  const atribuirPlano = useMutation({
    mutationFn: ({ id, planoId }: { id: string; planoId: string | null }) =>
      api(`/api/v1/plataforma/tenants/${id}/plano`, { method: "PUT", body: JSON.stringify({ planoId }) }),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ["plataforma-tenants"] }); },
  });

  return (
    <section className="rounded-xl bg-slate-800 p-4">
      <div className="mb-3 flex items-center justify-between">
        <h2 className="font-semibold">Empresas</h2>
        <button onClick={() => setNova((v) => !v)} className="rounded-lg bg-sky-600 px-3 py-1.5 text-sm font-semibold hover:bg-sky-500">
          {nova ? "Fechar" : "+ Nova empresa"}
        </button>
      </div>

      {nova && <NovaEmpresa onDone={() => setNova(false)} />}

      <div className="overflow-x-auto">
        <table className="w-full text-sm">
          <thead className="text-left text-slate-400">
            <tr><th className="py-1 pr-3">Empresa</th><th className="pr-3">Plano</th><th className="pr-3">Obras</th><th className="pr-3">Usuários</th><th className="pr-3">Status</th><th></th></tr>
          </thead>
          <tbody>
            {tenants?.map((t) => (
              <tr key={t.id} className="border-t border-slate-700/50">
                <td className="py-1 pr-3">
                  {editando === t.id ? (
                    <LinhaEdicao t={t} onSalvar={(nome, cnpj) => editar.mutate({ id: t.id, nome, cnpj })} onCancelar={() => setEditando(null)} salvando={editar.isPending} />
                  ) : (
                    <>
                      <span className="font-medium">{t.nome}</span>
                      {t.cnpj && <span className="block text-xs text-slate-500">{t.cnpj}</span>}
                    </>
                  )}
                </td>
                <td className="pr-3">
                  <select
                    value={t.planoId ?? ""}
                    disabled={atribuirPlano.isPending}
                    onChange={(e) => atribuirPlano.mutate({ id: t.id, planoId: e.target.value || null })}
                    className="rounded-lg bg-slate-900 px-2 py-1 text-sm disabled:opacity-50"
                    title={t.planoNome ?? "sem plano"}
                  >
                    <option value="">— sem plano —</option>
                    {planos?.map((p) => <option key={p.id} value={p.id}>{p.nome}</option>)}
                  </select>
                </td>
                <td className="pr-3"><UsoLimite n={t.nObras} limite={t.limiteObras} /></td>
                <td className="pr-3"><UsoLimite n={t.nUsuarios} limite={t.limiteUsuarios} /></td>
                <td className="pr-3">
                  <span className={t.status === "suspenso" ? "text-red-400" : "text-emerald-400"}>{t.status}</span>
                </td>
                <td className="text-right">
                  <div className="flex justify-end gap-1">
                    {editando !== t.id && (
                      <button onClick={() => setEditando(t.id)} className="rounded-lg bg-slate-700 px-2 py-1 text-xs text-slate-200 hover:bg-slate-600">Editar</button>
                    )}
                    {t.status === "suspenso" ? (
                      <button onClick={() => status.mutate({ id: t.id, status: "ativo" })} className="rounded-lg bg-emerald-700/60 px-2 py-1 text-xs text-emerald-100 hover:bg-emerald-700">Ativar</button>
                    ) : (
                      <button onClick={() => { if (confirm(`Suspender "${t.nome}"? Os usuários dela não conseguirão entrar.`)) status.mutate({ id: t.id, status: "suspenso" }); }} className="rounded-lg bg-red-900/60 px-2 py-1 text-xs text-red-200 hover:bg-red-900">Suspender</button>
                    )}
                    <button
                      disabled={excluir.isPending}
                      onClick={() => { if (confirm(`Excluir "${t.nome}"?\n\nA empresa some do console e ninguém dela consegue entrar. Os dados ficam guardados no banco (exclusão reversível).`)) excluir.mutate(t.id); }}
                      className="rounded-lg bg-red-950 px-2 py-1 text-xs text-red-300 hover:bg-red-900 disabled:opacity-50"
                    >Excluir</button>
                  </div>
                </td>
              </tr>
            ))}
            {tenants?.length === 0 && <tr><td colSpan={6} className="py-2 text-slate-400">Nenhuma empresa cadastrada.</td></tr>}
          </tbody>
        </table>
      </div>
    </section>
  );
}

// Edição inline do nome + CNPJ de uma empresa (dentro da célula "Empresa").
function LinhaEdicao({ t, onSalvar, onCancelar, salvando }: { t: TenantResumo; onSalvar: (nome: string, cnpj: string | null) => void; onCancelar: () => void; salvando: boolean }) {
  const [nome, setNome] = useState(t.nome);
  const [cnpj, setCnpj] = useState(t.cnpj ?? "");
  return (
    <div className="space-y-1">
      <input autoFocus className="w-full rounded-lg bg-slate-900 px-2 py-1 text-sm" value={nome} onChange={(e) => setNome(e.target.value)} placeholder="Nome da empresa" />
      <input className="w-full rounded-lg bg-slate-900 px-2 py-1 text-xs" value={cnpj} onChange={(e) => setCnpj(e.target.value)} placeholder="CNPJ (opcional)" />
      <div className="flex gap-1 pt-1">
        <button disabled={salvando || !nome.trim()} onClick={() => onSalvar(nome.trim(), cnpj.trim() || null)} className="rounded-lg bg-emerald-600 px-2 py-1 text-xs font-semibold hover:bg-emerald-500 disabled:opacity-50">{salvando ? "Salvando…" : "Salvar"}</button>
        <button disabled={salvando} onClick={onCancelar} className="rounded-lg bg-slate-700 px-2 py-1 text-xs hover:bg-slate-600">Cancelar</button>
      </div>
    </div>
  );
}

function NovaEmpresa({ onDone }: { onDone: () => void }) {
  const qc = useQueryClient();
  const [nomeEmpresa, setNomeEmpresa] = useState("");
  const [cnpj, setCnpj] = useState("");
  const [adminNome, setAdminNome] = useState("");
  const [adminEmail, setAdminEmail] = useState("");
  const [adminSenha, setAdminSenha] = useState("");
  const [erro, setErro] = useState<string | null>(null);

  const criar = useMutation({
    mutationFn: () => api("/api/v1/plataforma/tenants", { method: "POST", body: JSON.stringify({ nomeEmpresa, cnpj: cnpj.trim() || null, adminNome, adminEmail, adminSenha }) }),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ["plataforma-tenants"] }); qc.invalidateQueries({ queryKey: ["plataforma-metricas"] }); onDone(); },
    onError: (e) => setErro((e as Error).message),
  });

  return (
    <form onSubmit={(e) => { e.preventDefault(); criar.mutate(); }} className="mb-4 rounded-lg bg-slate-900/60 p-3 space-y-3">
      <p className="text-sm text-slate-400">Cria a empresa + o primeiro usuário administrador dela.</p>
      <div className="grid gap-3 sm:grid-cols-2">
        <input className="rounded-lg bg-slate-900 px-3 py-2 text-sm" placeholder="Nome da empresa *" value={nomeEmpresa} onChange={(e) => setNomeEmpresa(e.target.value)} required />
        <input className="rounded-lg bg-slate-900 px-3 py-2 text-sm" placeholder="CNPJ (opcional)" value={cnpj} onChange={(e) => setCnpj(e.target.value)} />
        <input className="rounded-lg bg-slate-900 px-3 py-2 text-sm" placeholder="Nome do admin *" value={adminNome} onChange={(e) => setAdminNome(e.target.value)} required />
        <input className="rounded-lg bg-slate-900 px-3 py-2 text-sm" type="email" placeholder="E-mail do admin *" value={adminEmail} onChange={(e) => setAdminEmail(e.target.value)} required />
        <input className="rounded-lg bg-slate-900 px-3 py-2 text-sm" type="password" placeholder="Senha do admin *" value={adminSenha} onChange={(e) => setAdminSenha(e.target.value)} required />
      </div>
      {erro && <p className="text-sm text-red-400">{erro}</p>}
      <button disabled={criar.isPending} className="rounded-lg bg-emerald-600 px-4 py-2 text-sm font-semibold hover:bg-emerald-500 disabled:opacity-50">
        {criar.isPending ? "Criando…" : "Criar empresa"}
      </button>
    </form>
  );
}

function Planos() {
  const qc = useQueryClient();
  const { data: planos } = useQuery({ queryKey: ["plataforma-planos"], queryFn: () => api<Plano[]>("/api/v1/plataforma/planos") });
  const [nome, setNome] = useState("");
  const [obras, setObras] = useState("");
  const [usuarios, setUsuarios] = useState("");
  const [preco, setPreco] = useState("");
  const [erro, setErro] = useState<string | null>(null);

  const criar = useMutation({
    mutationFn: () => api<Plano>("/api/v1/plataforma/planos", {
      method: "POST",
      body: JSON.stringify({ nome, limiteObras: obras ? Number(obras) : null, limiteUsuarios: usuarios ? Number(usuarios) : null, precoMensal: preco ? Number(preco) : null }),
    }),
    onSuccess: () => { setNome(""); setObras(""); setUsuarios(""); setPreco(""); setErro(null); qc.invalidateQueries({ queryKey: ["plataforma-planos"] }); },
    onError: (e) => setErro((e as Error).message),
  });

  return (
    <section className="rounded-xl bg-slate-800 p-4 space-y-3">
      <h2 className="font-semibold">Planos do SaaS</h2>
      <p className="rounded-lg bg-slate-900/40 px-3 py-2 text-xs text-slate-400">Só o modelo — cobrança/checkout/trial não implementados.</p>

      <form onSubmit={(e) => { e.preventDefault(); criar.mutate(); }} className="grid gap-2 sm:grid-cols-5">
        <input className="rounded-lg bg-slate-900 px-3 py-2 text-sm" placeholder="Nome *" value={nome} onChange={(e) => setNome(e.target.value)} required />
        <input className="rounded-lg bg-slate-900 px-3 py-2 text-sm" type="number" placeholder="Obras" value={obras} onChange={(e) => setObras(e.target.value)} />
        <input className="rounded-lg bg-slate-900 px-3 py-2 text-sm" type="number" placeholder="Usuários" value={usuarios} onChange={(e) => setUsuarios(e.target.value)} />
        <input className="rounded-lg bg-slate-900 px-3 py-2 text-sm" type="number" step="0.01" placeholder="R$/mês" value={preco} onChange={(e) => setPreco(e.target.value)} />
        <button disabled={criar.isPending} className="rounded-lg bg-sky-600 px-3 py-2 text-sm font-semibold hover:bg-sky-500 disabled:opacity-50">Adicionar</button>
      </form>
      {erro && <p className="text-sm text-red-400">{erro}</p>}

      <ul className="space-y-1">
        {planos?.map((p) => (
          <li key={p.id} className="flex items-center justify-between rounded-lg bg-slate-900 px-3 py-2 text-sm">
            <span className="font-medium">{p.nome}</span>
            <span className="text-slate-400">{p.limiteObras ?? "∞"} obras · {p.limiteUsuarios ?? "∞"} usuários · {brl(p.precoMensal)}/mês</span>
          </li>
        ))}
        {planos?.length === 0 && <li className="text-slate-500 text-sm">Nenhum plano.</li>}
      </ul>
    </section>
  );
}
