/// <reference types="vitest/config" />
import { fileURLToPath, URL } from 'node:url'
import { defineConfig, loadEnv } from 'vite'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'

// https://vite.dev/config/
export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), '')
  const proxyTarget = env.VITE_API_PROXY_TARGET || 'http://localhost:8080'

  return {
    plugins: [react(), tailwindcss()],
    resolve: {
      alias: {
        '@': fileURLToPath(new URL('./src', import.meta.url)),
      },
    },
    server: {
      // host:true escuta em 0.0.0.0 (necessário dentro do container Docker).
      host: true,
      port: Number(env.VITE_DEV_PORT) || 5173,
      // Polling de arquivos: bind mounts do Docker nem sempre propagam eventos
      // inotify; ligue via VITE_USE_POLLING=true no modo dev containerizado.
      watch: env.VITE_USE_POLLING === 'true' ? { usePolling: true } : undefined,
      // O backend não expõe CORS; em dev encaminhamos /api -> API .NET.
      proxy: {
        '/api': {
          target: proxyTarget,
          changeOrigin: true,
        },
      },
    },
    preview: {
      port: 4173,
    },
    test: {
      environment: 'jsdom',
      setupFiles: ['./src/setupTests.ts'],
      css: false,
    },
  }
})
