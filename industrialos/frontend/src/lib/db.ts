import { openDB, type DBSchema, type IDBPDatabase } from "idb";

// Camada offline (IndexedDB): cache de leitura (obras/EAP/funções) + rascunhos de RDO com fila de sync.
// O RDO é criado no servidor (id + número sequencial); offline usamos um localId e conciliamos no sync.

export type FotoPendente = { id: string; nome: string; blob: Blob; categoria?: string; descricao?: string };

export type RdoLocal = {
  localId: string;              // uuid gerado no dispositivo
  serverId: string | null;      // id do RDO no servidor (null até sincronizar/criar)
  obraId: string;
  data: unknown;                // conteúdo do RDO (mesmo shape do RdoUpsert do PUT)
  atualizadoEm: number;         // epoch ms — pra ordenar/detectar "sujo"
  sincronizado: boolean;        // true quando o servidor já tem esta versão
  finalizar: boolean;           // usuário pediu "Finalizar" offline → finalizar no sync
  erroSync: string | null;      // último erro de sync (ex.: conflito 409)
  fotosPendentes: FotoPendente[];
};

interface IndustrialDB extends DBSchema {
  cache: { key: string; value: { atualizadoEm: number; dados: unknown } };
  rdosLocais: { key: string; value: RdoLocal; indexes: { "por-obra": string } };
}

let _db: Promise<IDBPDatabase<IndustrialDB>> | null = null;
function db() {
  return (_db ??= openDB<IndustrialDB>("industrialos", 1, {
    upgrade(d) {
      d.createObjectStore("cache");
      const r = d.createObjectStore("rdosLocais", { keyPath: "localId" });
      r.createIndex("por-obra", "obraId");
    },
  }));
}

// ---- Cache de leitura (chave livre, ex.: "obras", "eap:<obraId>", "funcoes") ----
export async function setCache(chave: string, dados: unknown) {
  (await db()).put("cache", { atualizadoEm: Date.now(), dados }, chave);
}
export async function getCache<T>(chave: string): Promise<T | undefined> {
  const v = await (await db()).get("cache", chave);
  return v?.dados as T | undefined;
}

// ---- Rascunhos de RDO ----
// SEMPRE prefixado "loc-" → o editor detecta rascunho local pelo id (sem mudar props).
export function novoLocalId() {
  const uuid = (crypto as Crypto & { randomUUID?: () => string }).randomUUID?.() ??
    `${Date.now()}-${Math.random().toString(36).slice(2)}`;
  return `loc-${uuid}`;
}
export const ehLocalId = (id: string) => id.startsWith("loc-");
export async function salvarRdoLocal(r: RdoLocal) {
  (await db()).put("rdosLocais", r);
}
export async function obterRdoLocal(localId: string) {
  return (await db()).get("rdosLocais", localId);
}
export async function obterRdoLocalPorServer(serverId: string) {
  const todos = await (await db()).getAll("rdosLocais");
  return todos.find((r) => r.serverId === serverId);
}
export async function listarRdosLocaisDaObra(obraId: string) {
  return (await db()).getAllFromIndex("rdosLocais", "por-obra", obraId);
}
export async function removerRdoLocal(localId: string) {
  (await db()).delete("rdosLocais", localId);
}
export async function pendentesSync(): Promise<RdoLocal[]> {
  const todos = await (await db()).getAll("rdosLocais");
  return todos.filter((r) => !r.sincronizado);
}
export async function contarPendentes(): Promise<number> {
  return (await pendentesSync()).length;
}
