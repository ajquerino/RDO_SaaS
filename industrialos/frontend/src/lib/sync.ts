import { api, apiUpload } from "./api";
import { pendentesSync, salvarRdoLocal, type RdoLocal } from "./db";
import { notificarSyncMudou } from "./useOnline";

// Motor de sincronização dos rascunhos de RDO. Ordem por rascunho:
// 1) se não tem serverId → cria no servidor (POST) e guarda o id/número;
// 2) manda o conteúdo (PUT full-replace);
// 3) sobe as fotos pendentes (uma a uma; remove da fila ao subir);
// 4) se pediu Finalizar offline → finaliza;
// e marca como sincronizado. Erro (ex.: 409 aprovado) fica registrado em erroSync e tenta de novo depois.

let rodando = false;

export async function sincronizar(): Promise<void> {
  if (rodando || !navigator.onLine) return;
  rodando = true;
  try {
    const pend = await pendentesSync();
    for (const r of pend) {
      try {
        await sincronizarUm(r);
      } catch (e) {
        r.erroSync = (e as Error).message;
        await salvarRdoLocal(r);
      }
    }
  } finally {
    rodando = false;
    notificarSyncMudou();
  }
}

async function sincronizarUm(r: RdoLocal) {
  const corpo = (r.data ?? {}) as Record<string, unknown>;

  // 1) cria no servidor se ainda não existe lá
  if (!r.serverId) {
    const criado = await api<{ id: string }>(`/api/v1/obras/${r.obraId}/rdos`, {
      method: "POST",
      body: JSON.stringify({ data: corpo.data ?? new Date().toISOString().slice(0, 10) }),
    });
    r.serverId = criado.id;
    await salvarRdoLocal(r);
  }

  // 2) conteúdo (full-replace)
  await api(`/api/v1/rdos/${r.serverId}`, { method: "PUT", body: JSON.stringify(corpo) });

  // 3) fotos pendentes (sobe e remove da fila conforme sobem)
  while (r.fotosPendentes.length) {
    const f = r.fotosPendentes[0];
    const fd = new FormData();
    fd.append("file", f.blob, f.nome);
    if (f.categoria) fd.append("categoria", f.categoria);
    if (f.descricao) fd.append("descricao", f.descricao);
    await apiUpload(`/api/v1/rdos/${r.serverId}/midia`, fd);
    r.fotosPendentes = r.fotosPendentes.slice(1);
    await salvarRdoLocal(r);
  }

  // 4) finalizar (se pedido offline)
  if (r.finalizar) {
    await api(`/api/v1/rdos/${r.serverId}/finalizar`, { method: "POST" });
    r.finalizar = false;
  }

  r.sincronizado = true;
  r.erroSync = null;
  await salvarRdoLocal(r);
}

/** Liga o sync automático: ao voltar a conexão e a cada 30s enquanto online. */
export function iniciarSyncAutomatico() {
  const tenta = () => { void sincronizar(); };
  window.addEventListener("online", tenta);
  setInterval(tenta, 30000);
  tenta(); // uma tentativa na carga
}
