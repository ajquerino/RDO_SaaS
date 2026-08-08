import { useState } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { api, type ObraItem } from "../lib/api";

const brl = (v?: number | null) => (v == null ? "—" : v.toLocaleString("pt-BR", { style: "currency", currency: "BRL" }));
const num = (s: string) => (s.trim() === "" ? null : Number(s));

type Campos = { descricao: string; unidade: string; qtdPrevista: string; hhPrevisto: string; valor: string; disciplina: string; dataInicio: string; dataFim: string };
const VAZIO: Campos = { descricao: "", unidade: "", qtdPrevista: "", hhPrevisto: "", valor: "", disciplina: "", dataInicio: "", dataFim: "" };
const doItem = (i: ObraItem): Campos => ({
  descricao: i.descricao ?? "", unidade: i.unidade ?? "", qtdPrevista: i.qtdPrevista?.toString() ?? "",
  hhPrevisto: i.hhPrevisto?.toString() ?? "", valor: i.valor?.toString() ?? "", disciplina: i.disciplina ?? "",
  dataInicio: i.dataInicio ?? "", dataFim: i.dataFim ?? "",
});
// Monta o corpo do ItemRequest. Quem NÃO vê R$ não altera valor (preserva o atual — não zera sem querer).
const toBody = (c: Campos, verValores: boolean, valorAtual?: number | null) => ({
  descricao: c.descricao.trim(),
  unidade: c.unidade || null,
  qtdPrevista: num(c.qtdPrevista),
  hhPrevisto: num(c.hhPrevisto),
  valor: verValores ? num(c.valor) : valorAtual ?? null,
  disciplina: c.disciplina || null,
  dataInicio: c.dataInicio || null,
  dataFim: c.dataFim || null,
});

/** EAP editável: adicionar item manual, editar inline (Salvar/Cancelar) e excluir.
 *  Só p/ Planejador/Gestor/Admin (gereObras). Coluna Valor só p/ quem podeVerValores. */
export default function EapEditor({ obraId, itens, gereObras, verValores }: { obraId: string; itens: ObraItem[]; gereObras: boolean; verValores: boolean }) {
  const qc = useQueryClient();
  const [novo, setNovo] = useState(false);
  const [add, setAdd] = useState<Campos>(VAZIO);
  const [editId, setEditId] = useState<string | null>(null);
  const [edit, setEdit] = useState<Campos>(VAZIO);

  const invalidar = () => { qc.invalidateQueries({ queryKey: ["obra", obraId] }); qc.invalidateQueries({ queryKey: ["obras"] }); };

  const adicionar = useMutation({
    mutationFn: (c: Campos) => api(`/api/v1/obras/${obraId}/itens`, { method: "POST", body: JSON.stringify(toBody(c, verValores)) }),
    onSuccess: () => { setAdd(VAZIO); setNovo(false); invalidar(); },
  });
  const editar = useMutation({
    mutationFn: (v: { id: string; c: Campos; valorAtual?: number | null }) =>
      api(`/api/v1/obras/${obraId}/itens/${v.id}`, { method: "PUT", body: JSON.stringify(toBody(v.c, verValores, v.valorAtual)) }),
    onSuccess: () => { setEditId(null); invalidar(); },
  });
  const excluir = useMutation({
    mutationFn: (id: string) => api(`/api/v1/obras/${obraId}/itens/${id}`, { method: "DELETE" }),
    onSuccess: invalidar,
  });

  const totalHh = itens.reduce((s, i) => s + (i.hhPrevisto ?? 0), 0);
  const totalValor = itens.reduce((s, i) => s + (i.valor ?? 0), 0);
  const cols = 5 + (verValores ? 1 : 0) + (gereObras ? 1 : 0);
  const inp = "w-full rounded bg-slate-900 px-2 py-1 text-sm";

  return (
    <div>
      <div className="mb-2 flex flex-wrap items-center justify-between gap-2 text-sm text-slate-400">
        <span>EAP — {itens.length} itens</span>
        <div className="flex items-center gap-3">
          <span>HH previsto: {totalHh.toLocaleString("pt-BR")}{verValores ? ` · ${brl(totalValor)}` : ""}</span>
          {gereObras && <button onClick={() => setNovo((v) => !v)} className="rounded-lg bg-sky-600 px-3 py-1 text-xs font-semibold text-white hover:bg-sky-500">{novo ? "Fechar" : "+ Adicionar item"}</button>}
        </div>
      </div>

      {gereObras && novo && (
        <form onSubmit={(e) => { e.preventDefault(); if (add.descricao.trim()) adicionar.mutate(add); }} className="mb-3 rounded-lg bg-slate-900/50 p-3 space-y-2">
          <div className="grid gap-2 sm:grid-cols-2">
            <input className={inp} placeholder="Descrição *" value={add.descricao} onChange={(e) => setAdd({ ...add, descricao: e.target.value })} required />
            <input className={inp} placeholder="Disciplina" value={add.disciplina} onChange={(e) => setAdd({ ...add, disciplina: e.target.value })} />
            <input className={inp} placeholder="Unidade" value={add.unidade} onChange={(e) => setAdd({ ...add, unidade: e.target.value })} />
            <input className={inp} type="number" step="0.01" placeholder="Qtd prevista" value={add.qtdPrevista} onChange={(e) => setAdd({ ...add, qtdPrevista: e.target.value })} />
            <input className={inp} type="number" step="0.01" placeholder="HH previsto" value={add.hhPrevisto} onChange={(e) => setAdd({ ...add, hhPrevisto: e.target.value })} />
            {verValores && <input className={inp} type="number" step="0.01" placeholder="Valor (R$)" value={add.valor} onChange={(e) => setAdd({ ...add, valor: e.target.value })} />}
            <input className={inp} type="date" title="Início" value={add.dataInicio} onChange={(e) => setAdd({ ...add, dataInicio: e.target.value })} />
            <input className={inp} type="date" title="Fim" value={add.dataFim} onChange={(e) => setAdd({ ...add, dataFim: e.target.value })} />
          </div>
          <button disabled={adicionar.isPending} className="rounded-lg bg-emerald-600 px-4 py-1.5 text-sm font-semibold hover:bg-emerald-500 disabled:opacity-50">{adicionar.isPending ? "Salvando…" : "Adicionar"}</button>
        </form>
      )}

      <div className="overflow-x-auto">
        <table className="w-full text-sm">
          <thead className="text-left text-slate-400">
            <tr>
              <th className="py-1 pr-3">Descrição</th><th className="pr-3">Un</th><th className="pr-3">Qtd</th><th className="pr-3">HH</th>
              {verValores && <th className="pr-3">Valor</th>}<th className="pr-3">Disciplina</th>{gereObras && <th></th>}
            </tr>
          </thead>
          <tbody>
            {itens.map((i) => editId === i.id ? (
              <tr key={i.id} className="border-t border-slate-700/50">
                <td className="py-1 pr-2"><input className={inp} value={edit.descricao} onChange={(e) => setEdit({ ...edit, descricao: e.target.value })} /></td>
                <td className="pr-2"><input className={inp} value={edit.unidade} onChange={(e) => setEdit({ ...edit, unidade: e.target.value })} /></td>
                <td className="pr-2"><input className={inp} type="number" step="0.01" value={edit.qtdPrevista} onChange={(e) => setEdit({ ...edit, qtdPrevista: e.target.value })} /></td>
                <td className="pr-2"><input className={inp} type="number" step="0.01" value={edit.hhPrevisto} onChange={(e) => setEdit({ ...edit, hhPrevisto: e.target.value })} /></td>
                {verValores && <td className="pr-2"><input className={inp} type="number" step="0.01" value={edit.valor} onChange={(e) => setEdit({ ...edit, valor: e.target.value })} /></td>}
                <td className="pr-2"><input className={inp} value={edit.disciplina} onChange={(e) => setEdit({ ...edit, disciplina: e.target.value })} /></td>
                <td className="whitespace-nowrap text-right">
                  <button onClick={() => edit.descricao.trim() && editar.mutate({ id: i.id, c: edit, valorAtual: i.valor })} disabled={editar.isPending} className="rounded bg-emerald-700/60 px-2 py-1 text-xs text-emerald-100 hover:bg-emerald-700">Salvar</button>
                  <button onClick={() => setEditId(null)} className="ml-1 rounded bg-slate-700 px-2 py-1 text-xs hover:bg-slate-600">Cancelar</button>
                </td>
              </tr>
            ) : (
              <tr key={i.id} className="border-t border-slate-700/50">
                <td className="py-1 pr-3">{i.descricao}</td>
                <td className="pr-3">{i.unidade ?? "—"}</td>
                <td className="pr-3">{i.qtdPrevista ?? "—"}</td>
                <td className="pr-3">{i.hhPrevisto ?? "—"}</td>
                {verValores && <td className="pr-3">{brl(i.valor)}</td>}
                <td className="pr-3">{i.disciplina ?? "—"}</td>
                {gereObras && (
                  <td className="whitespace-nowrap text-right">
                    <button onClick={() => { setEditId(i.id); setEdit(doItem(i)); }} className="rounded bg-slate-700 px-2 py-1 text-xs hover:bg-slate-600">Editar</button>
                    <button onClick={() => { if (confirm(`Excluir o item "${i.descricao}"?`)) excluir.mutate(i.id); }} disabled={excluir.isPending} className="ml-1 rounded bg-red-950 px-2 py-1 text-xs text-red-300 hover:bg-red-900 disabled:opacity-50">Excluir</button>
                  </td>
                )}
              </tr>
            ))}
            {itens.length === 0 && <tr><td colSpan={cols} className="py-2 text-slate-400">Sem itens{gereObras ? ". Adicione manualmente ou importe um cronograma acima." : "."}</td></tr>}
          </tbody>
        </table>
      </div>
    </div>
  );
}
