import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";
import { VitePWA } from "vite-plugin-pwa";

// PWA offline-first: o service worker cacheia o app shell (navigateFallback garante
// que a SPA abra sem rede); os DADOS (RDO, catálogos) e a fila de sync ficam em
// IndexedDB via src/lib/offline.ts.
export default defineConfig({
  plugins: [
    react(),
    VitePWA({
      registerType: "autoUpdate",
      workbox: {
        navigateFallback: "index.html", // qualquer rota da SPA cai no index quando offline
        globPatterns: ["**/*.{js,css,html,svg,png,ico,webmanifest}"],
      },
      manifest: {
        name: "IndustrialOS",
        short_name: "IndustrialOS",
        start_url: "/",
        display: "standalone",
        background_color: "#0f172a",
        theme_color: "#0f172a",
      },
    }),
  ],
  server: { port: 5173 },
});
