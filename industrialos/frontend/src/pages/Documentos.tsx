import { useRef, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { api, apiUpload, type Documento } from "../lib/api";
import { useAuth, podeGerirObras } from "../store/auth";

const TIPOS = ["projeto", "desenho", "procedimento", "arquivo"];
const tamanho = (b: number) => (b > 1e6 ? `${(b / 1e6).toFixed(1)} MB` : `${Math.max(1, Math.round(b / 1e3))} KB`);

/** Aba Documentos da obra (Sprint 8): upload no R2 + lista com link assinado + excluir. */
export default function Documentos({ obraId }: { obraId: string }) {
  const qc = useQueryClient();
  const gere = podeGerirObras(useAuth((s) => s.usuario)?.funcao);
  const fileRef = useRef<HTMLInputElement>(null);
  const [tipo, setTipo] = useState("arquivo");
  const [versao, setVersao] = useState("");
  const [msg, setMsg] = useState<string | null>(null);

  const { data: docs } = useQuery({ queryKey: ["documentos", obraId], queryFn: () => api<Documento[]>(`/api/v1/obras/${obraId}/documentos`) });

  const enviar = useMutation({
    mutationFn: (file: File) => {
      const f = new FormData();
      f.append("file", file); f.append("tipo", tipo); if (versao) f.append("versao", versao);
      return apiUpload(`/api/v1/obras/${obraId}/documentos`, f);
    },
    onSuccess: () => { setMsg(null); setVersao(""); if (fileRef.current) fileRef.current.value = ""; qc.invalidateQueries({ queryKey: ["documentos", obraId] }); },
    onError: (e) => setMsg((e as Error).message),
  });
  const apagar = useMutation({
    mutationFn: (id: string) => api(`/api/v1/documentos/${id}`, { method: "DELETE" }),
    onSuccess: () => qc.invalidateQueries({ queryKey: ["documentos", obraId] }),
  });

  return (
    <section className="space-y-4">
      {gere && (
        <div className="rounded-xl bg-slate-800 p-4 space-y-3">
          <h3 className="font-semibold">Enviar documento</h3>
          <div className="flex flex-wrap items-center gap-2">
            <select value={tipo} onChange={(e) => setTipo(e.target.value)} className="rounded-lg bg-slate-900 px-3 py-2 text-sm">
              {TIPOS.map((t) => <option key={t} value={t}>{t}</option>)}
            </select>
            <input value={versao} onChange={(e) => setVersao(e.target.value)} placeholder="Versão (ex.: rev.0)" className="rounded-lg bg-slate-900 px-3 py-2 text-sm" />
            <input ref={fileRef} type="file" className="text-sm text-slate-300 file:mr-3 file:rounded-lg file:border-0 file:bg-sky-600 file:px-3 file:py-1.5 file:text-white" />
            <button
              onClick={() => { const f = fileRef.current?.files?.[0]; if (f) enviar.mutate(f); }}
              disabled={enviar.isPending}
              className="rounded-lg bg-sky-600 px-3 py-2 text-sm font-semibold hover:bg-sky-500 disabled:opacity-50"
            >
              {enviar.isPending ? "Enviando…" : "Enviar"}
            </button>
          </div>
          {msg && <p className="text-sm text-red-400">{msg}</p>}
        </div>
      )}

      <ul className="space-y-1">
        {docs?.map((d) => (
          <li key={d.id} className="flex items-center justify-between rounded-lg bg-slate-800 px-3 py-2 text-sm">
            <span className="min-w-0">
              <a href={d.url} target="_blank" rel="noreferrer" className="font-medium text-sky-400 hover:underline break-all">{d.nome}</a>
              <span className="block text-xs text-slate-400">{d.tipo}{d.versao ? ` · ${d.versao}` : ""} · {tamanho(d.tamanhoBytes)} · {new Date(d.criadoEm).toLocaleDateString("pt-BR")}</span>
            </span>
            {gere && (
              <button onClick={() => { if (confirm(`Excluir "${d.nome}"?`)) apagar.mutate(d.id); }} className="ml-2 shrink-0 rounded-lg bg-red-900/60 px-3 py-1 text-xs text-red-200 hover:bg-red-900">Excluir</button>
            )}
          </li>
        ))}
        {docs?.length === 0 && <li className="text-slate-500 text-sm">Nenhum documento ainda.</li>}
      </ul>
    </section>
  );
}
