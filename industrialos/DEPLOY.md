# Deploy em produção (VPS com Docker) — IndustrialOS

Sobe o sistema inteiro (banco + API + frontend) numa VPS com **um comando**. O banco é
criado, **migrado e semeado sozinho** no primeiro boot. Nenhum segredo fica no Git — tudo vem
do arquivo `.env`.

> Requisitos: uma VPS Linux (Ubuntu 22.04+ recomendado), acesso `ssh` e um domínio ou IP público.
> As portas **80** (frontend) e **8080** (API) precisam estar liberadas no firewall.

---

## 1. Instalar Docker na VPS

```bash
curl -fsSL https://get.docker.com | sh
sudo usermod -aG docker $USER    # opcional: usar docker sem sudo (reentrar no ssh depois)
```

## 2. Clonar o projeto

```bash
git clone https://github.com/ajquerino/RDO_SaaS.git
cd RDO_SaaS/industrialos
```

## 3. Criar e preencher o `.env`

```bash
cp .env.example .env
nano .env
```

Preencha (veja os comentários no próprio arquivo):

- **POSTGRES_USER / POSTGRES_PASSWORD / POSTGRES_DB** — credenciais do banco no container.
- **ConnectionStrings__Postgres** — use `Host=postgres` (nome do serviço) e as MESMAS credenciais acima.
- **Jwt__Key** — chave forte de 32+ caracteres. Gere com: `openssl rand -base64 48`
- **R2__AccountId / R2__Bucket / R2__AccessKeyId / R2__SecretAccessKey** — suas chaves do Cloudflare R2.
- **Seed__SuperAdminEmail / Seed__SuperAdminSenha** — login do super-admin criado no 1º boot.
- **VITE_API_URL** — URL pública onde a API responde. Ex.: `http://SEU_IP:8080`
  (ou `https://api.seudominio.com` se você colocar um proxy/HTTPS na frente).

> ⚠️ **VITE_API_URL é lido em build-time.** Se mudar depois, rode de novo com `--build` (passo 4)
> para reconstruir o frontend.

## 4. Subir tudo

```bash
docker compose -f docker-compose.prod.yml up -d --build
```

O que acontece automaticamente:
1. O **postgres** sobe e o healthcheck espera ele ficar pronto.
2. A **api** só inicia depois do banco saudável; no boot ela **aplica as migrations**
   (cria/atualiza o schema) e roda o **seed de sistema** (tenant "Plataforma" + super-admin
   com o e-mail/senha do `.env`). **Nenhum dado fictício é criado em produção.**
3. O **frontend** é servido pelo nginx na porta 80.

Ver os logs / status:
```bash
docker compose -f docker-compose.prod.yml logs -f api
docker compose -f docker-compose.prod.yml ps
```

Saúde da API: `curl http://localhost:8080/health` → deve responder `Healthy`.

## 5. Criar a primeira empresa

1. Acesse o frontend: `http://SEU_IP` (porta 80).
2. Faça login com o **super-admin** (o `Seed__SuperAdminEmail` / `Seed__SuperAdminSenha` do `.env`).
   O super cai direto no **Console de Plataforma**.
3. Clique em **“+ Nova empresa”**, informe empresa + nome/e-mail/senha do admin dela e crie.
   A empresa já nasce com o **catálogo de 61 funções de mão de obra**.
4. Saia e entre com o admin da empresa recém-criada para começar a usar (obras, RDOs, etc.).

---

## Operação do dia a dia

| Ação | Comando |
|---|---|
| Atualizar o app (após `git pull`) | `docker compose -f docker-compose.prod.yml up -d --build` |
| Parar tudo | `docker compose -f docker-compose.prod.yml down` |
| Parar apagando o banco (CUIDADO) | `docker compose -f docker-compose.prod.yml down -v` |
| Backup do banco | `docker compose -f docker-compose.prod.yml exec postgres pg_dump -U industrialos industrialos > backup.sql` |

Os dados do Postgres ficam no volume nomeado **`pgdata`** — sobrevivem a `up`/`down` (mas
**não** a `down -v`). As fotos/PDFs ficam no **Cloudflare R2**, fora da VPS.

---

## Notas

- **Migrations são idempotentes e rodam em todo boot** — atualizações de schema entram sozinhas
  ao subir uma versão nova. Não precisa rodar `dotnet ef` na mão.
- **HTTPS:** este compose serve HTTP puro (portas 80/8080). Para HTTPS com domínio, coloque um
  reverse proxy (Caddy/Traefik/Nginx) na frente — aí ajuste `VITE_API_URL` para `https://...`
  e reconstrua o frontend.
- **Dev local não muda:** em `Development` continua criando a Empresa Demo + `admin@demo.com`
  + funções, exatamente como antes.
