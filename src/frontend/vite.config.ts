import react from '@vitejs/plugin-react'
import { defineConfig } from 'vitest/config'

// ADR-0006: in production the Api serves the built SPA from its wwwroot (same origin); in
// development the Vite dev server proxies /api to the Api.
export default defineConfig({
  plugins: [react()],
  server: {
    proxy: { '/api': 'http://localhost:5083' },
  },
  build: {
    outDir: '../backend/KamraApp.Api/wwwroot',
    emptyOutDir: true,
  },
  test: {
    environment: 'jsdom',
  },
})
