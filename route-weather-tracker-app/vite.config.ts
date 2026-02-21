import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    host: true,
    port: parseInt(process.env['PORT'] ?? '5173'),
    proxy: {
      '/api': {
        // Aspire injects service discovery URLs via process.env at dev-server startup
        target:
          process.env['services__api__https__0'] ||
          process.env['services__api__http__0'],
        changeOrigin: true,
        secure: false,
      },
    },
  },
})
