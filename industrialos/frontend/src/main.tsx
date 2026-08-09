import React from "react";
import ReactDOM from "react-dom/client";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import App from "./App";
import "./index.css";
import { iniciarSyncAutomatico } from "./lib/sync";

const qc = new QueryClient();

// Offline-first: tenta sincronizar rascunhos de RDO ao voltar a conexão e periodicamente.
iniciarSyncAutomatico();

ReactDOM.createRoot(document.getElementById("root")!).render(
  <React.StrictMode>
    <QueryClientProvider client={qc}>
      <App />
    </QueryClientProvider>
  </React.StrictMode>
);
