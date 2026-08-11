import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";
import { VitePWA } from "vite-plugin-pwa";

// PWA offline-first: cache do app shell; a fila de sync do RDO virá no Sprint 4.
export default defineConfig({
  plugins: [
    react(),
    VitePWA({
      registerType: "autoUpdate",
      manifest: {
        name: "Montaris",
        short_name: "Montaris",
        description: "Gestão de obras industriais",
        start_url: "/",
        display: "standalone",
        background_color: "#0f172a",
        theme_color: "#0f172a",
      },
    }),
  ],
  server: { port: 5173 },
});
