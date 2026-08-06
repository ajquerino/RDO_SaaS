import React from "react";
import ReactDOM from "react-dom/client";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import App from "./App";
import { iniciarOffline } from "./lib/offline";
import "./index.css";

const qc = new QueryClient();

// Liga o motor offline: sincroniza a fila ao carregar, ao voltar a rede e a cada 30s.
iniciarOffline();

ReactDOM.createRoot(document.getElementById("root")!).render(
  <React.StrictMode>
    <QueryClientProvider client={qc}>
      <App />
    </QueryClientProvider>
  </React.StrictMode>
);
