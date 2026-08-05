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
