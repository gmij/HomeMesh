import { defineConfig } from 'vite';
import vue from '@vitejs/plugin-vue';
import Components from 'unplugin-vue-components/vite';
import { AntDesignVueResolver } from 'unplugin-vue-components/resolvers';

const proxyTarget = process.env.VITE_API_PROXY_TARGET ?? 'http://localhost:5000';

export default defineConfig({
  base: '/admin/',
  plugins: [
    vue(),
    Components({
      dts: 'src/components.d.ts',
      resolvers: [
        AntDesignVueResolver({
          importStyle: false
        })
      ]
    })
  ],
  build: {
    outDir: '../HomeMesh.WebApi/dist',
    emptyOutDir: true,
    rollupOptions: {
      output: {
        manualChunks(id) {
          if (!id.includes('node_modules')) {
            return;
          }

          if (id.includes('@ant-design/icons-vue')) {
            return 'antd-icons';
          }

          if (id.includes('vue-router')) {
            return 'router';
          }

          if (id.includes('vue-i18n')) {
            return 'i18n';
          }

          if (id.includes('/vue/') || id.includes('\\vue\\')) {
            return 'vue';
          }
        }
      }
    }
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
