// Camada offline-first (sem dependência externa — IndexedDB nativo).
// - cache: guarda respostas de GET por caminho, para o app abrir sem sinal.
// - queue: fila de escritas (PUT/POST/DELETE) feitas offline, sincronizadas depois.
// O objetivo central: o encarregado preenche o RDO em campo sem internet e,
// ao voltar o sinal, tudo sobe sozinho (last-write-wins no autosave full-replace).

const API = import.meta.env.VITE_API_URL ?? "http://localhost:8080";
const DB_NAME = "industrialos-offline";
const DB_VERSION = 1;
const STORE_CACHE = "cache";
const STORE_QUEUE = "queue";

export type QueueItem = { id: string; method: string; path: string; body?: string; ts: number };

function abrirDb(): Promise<IDBDatabase> {
  return new Promise((resolve, reject) => {
    const req = indexedDB.open(DB_NAME, DB_VERSION);
    req.onupgradeneeded = () => {
      const db = req.result;
      if (!db.objectStoreNames.contains(STORE_CACHE)) db.createObjectStore(STORE_CACHE, { keyPath: "path" });
      if (!db.objectStoreNames.contains(STORE_QUEUE)) db.createObjectStore(STORE_QUEUE, { keyPath: "id" });
    };
    req.onsuccess = () => resolve(req.result);
    req.onerror = () => reject(req.error);
  });
}

function tx<T>(store: string, mode: IDBTransactionMode, fn: (s: IDBObjectStore) => IDBRequest): Promise<T> {
  return abrirDb().then(
    (db) =>
      new Promise<T>((resolve, reject) => {
        const t = db.transaction(store, mode);
        const req = fn(t.objectStore(store));
        let resultado: T;
        req.onsuccess = () => { resultado = req.result as T; };
        req.onerror = () => reject(req.error);
        // Resolve só no COMMIT (oncomplete), não no onsuccess do request: garante que
        // uma leitura seguinte (em nova conexão) já enxergue o que foi gravado.
        t.oncomplete = () => { db.close(); resolve(resultado); };
        t.onerror = () => { db.close(); reject(t.error); };
        t.onabort = () => { db.close(); reject(t.error); };
      })
  );
}

// ---- Cache de GET ----
export async function lerCache<T>(path: string): Promise<T | undefined> {
  try {
    const row = await tx<{ path: string; valor: T } | undefined>(STORE_CACHE, "readonly", (s) => s.get(path));
    return row?.valor;
  } catch {
    return undefined; // IndexedDB indisponível (aba privada etc.) — degrada sem quebrar
  }
}

export async function gravarCache<T>(path: string, valor: T): Promise<void> {
  try {
    await tx(STORE_CACHE, "readwrite", (s) => s.put({ path, valor }));
  } catch {
    /* sem cache é tolerável */
  }
}

// ---- Fila de escritas ----
export async function listarFila(): Promise<QueueItem[]> {
  try {
    const itens = await tx<QueueItem[]>(STORE_QUEUE, "readonly", (s) => s.getAll());
    return (itens ?? []).sort((a, b) => a.ts - b.ts);
  } catch {
    return [];
  }
}

async function removerDaFila(id: string): Promise<void> {
  await tx(STORE_QUEUE, "readwrite", (s) => s.delete(id));
}

/** Enfileira uma escrita feita offline. Para PUT full-replace no mesmo caminho,
 *  substitui a pendência anterior (só a última versão importa). */
export async function enfileirar(method: string, path: string, body?: string): Promise<void> {
  const atual = await listarFila();
  const antigo = atual.find((q) => q.method === method && q.path === path && method === "PUT");
  const id = antigo?.id ?? (crypto.randomUUID?.() ?? `${Date.now()}-${Math.random().toString(36).slice(2)}`);
  await tx(STORE_QUEUE, "readwrite", (s) => s.put({ id, method, path, body, ts: Date.now() }));
  notificar();
}

// ---- Estado observável (online / sincronizando / nº de pendências) ----
export type StatusOffline = { online: boolean; sincronizando: boolean; pendentes: number };
let estado: StatusOffline = { online: navigator.onLine, sincronizando: false, pendentes: 0 };
const ouvintes = new Set<(s: StatusOffline) => void>();

export function assinarStatus(fn: (s: StatusOffline) => void): () => void {
  ouvintes.add(fn);
  fn(estado);
  return () => ouvintes.delete(fn);
}

function setEstado(patch: Partial<StatusOffline>) {
  estado = { ...estado, ...patch };
  ouvintes.forEach((fn) => fn(estado));
}

async function notificar() {
  const fila = await listarFila();
  setEstado({ pendentes: fila.length });
}

// ---- Sincronização ----
let sincronizando = false;

/** Reenvia a fila em ordem. Para no primeiro erro de rede (tenta de novo depois).
 *  Erros do servidor (4xx) descartam o item para não travar a fila para sempre. */
export async function sincronizar(): Promise<void> {
  if (sincronizando || !navigator.onLine) return;
  sincronizando = true;
  setEstado({ sincronizando: true });
  try {
    const fila = await listarFila();
    const token = localStorage.getItem("accessToken");
    for (const item of fila) {
      try {
        const res = await fetch(`${API}${item.path}`, {
          method: item.method,
          headers: {
            "Content-Type": "application/json",
            ...(token ? { Authorization: `Bearer ${token}` } : {}),
          },
          body: item.body,
        });
        if (res.ok || res.status === 204) {
          await removerDaFila(item.id);
        } else if (res.status === 401 || res.status === 403) {
          break; // token expirou/sessão caiu: NÃO descarta — reenvia após novo login
        } else if (res.status >= 400 && res.status < 500) {
          // Requisição realmente inválida (400/404/409/422): reenviar não resolve — remove p/ não travar.
          await removerDaFila(item.id);
        } else {
          break; // 5xx: para e tenta na próxima rodada
        }
      } catch {
        break; // caiu a rede no meio — retoma quando voltar
      }
    }
  } finally {
    sincronizando = false;
    setEstado({ sincronizando: false });
    await notificar();
  }
}

let iniciado = false;
/** Liga os gatilhos de sync: ao carregar, ao voltar a ficar online e a cada 30s. */
export function iniciarOffline() {
  if (iniciado) return;
  iniciado = true;
  const aoOnline = () => { setEstado({ online: true }); sincronizar(); };
  const aoOffline = () => setEstado({ online: false });
  window.addEventListener("online", aoOnline);
  window.addEventListener("offline", aoOffline);
  setInterval(() => { if (navigator.onLine) sincronizar(); }, 30_000);
  notificar();
  sincronizar();
}

/** Erros que indicam falta de rede (para decidir cair no cache / enfileirar). */
export function ehErroDeRede(e: unknown): boolean {
  if (!navigator.onLine) return true;
  if (e instanceof TypeError) return true; // "Failed to fetch"
  if (e instanceof Error && /esgotado|Failed to fetch|NetworkError/i.test(e.message)) return true;
  return false;
}
