import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";

// Izstrādes režīmā pieprasījumi uz /api tiek pārsūtīti uz ASP.NET Core aizmugursistēmu.
export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    proxy: {
      "/api": process.env.API_URL ?? "http://localhost:5080",
    },
  },
});
