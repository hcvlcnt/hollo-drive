import { reactRouter } from "@react-router/dev/vite"
import tailwindcss from "@tailwindcss/vite"
import { defineConfig } from "vite"

const apiProxyTarget =
  process.env.VITE_API_PROXY_TARGET ?? "http://localhost:8080"

export default defineConfig({
  resolve: { tsconfigPaths: true },
  server: {
    proxy: {
      "/api": apiProxyTarget,
    },
  },
  plugins: [tailwindcss(), reactRouter()],
})
