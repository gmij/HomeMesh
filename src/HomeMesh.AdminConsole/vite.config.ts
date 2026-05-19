import { defineConfig } from 'vite';
import vue from '@vitejs/plugin-vue';

const proxyTarget = process.env.VITE_API_PROXY_TARGET ?? 'http://localhost:5000';

export default defineConfig({
  base: '/admin/',
  plugins: [vue()],
  build: {
    outDir: '../HomeMesh.WebApi/dist',
    emptyOutDir: true
  },
  server: {
    host: '127.0.0.1',
    port: 5173,
    proxy: {
      '/api': proxyTarget,
      '/health': proxyTarget,
      '/swagger': proxyTarget
    }
  }
});
