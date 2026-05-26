import { defineConfig } from 'vite';
import vue from '@vitejs/plugin-vue';
import VueI18nPlugin from '@intlify/unplugin-vue-i18n/vite';
import Components from 'unplugin-vue-components/vite';
import { AntDesignVueResolver } from 'unplugin-vue-components/resolvers';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const proxyTarget = process.env.VITE_API_PROXY_TARGET ?? 'http://localhost:5000';
const adminConsoleRoot = path.dirname(fileURLToPath(import.meta.url));

function cspSafeDependencies() {
  return {
    name: 'csp-safe-dependencies',
    enforce: 'pre' as const,
    transform(code: string, id: string) {
      const normalizedId = id.replace(/\\/g, '/');

      if (normalizedId.endsWith('/node_modules/lodash-es/_root.js')) {
        return code.replace(
          "var root = freeGlobal || freeSelf || Function('return this')();",
          'var root = freeGlobal || freeSelf || globalThis;'
        );
      }

      if (normalizedId.endsWith('/node_modules/resize-observer-polyfill/src/shims/global.js')) {
        return code.replace(
          "    // eslint-disable-next-line no-new-func\n    return Function('return this')();",
          '    return globalThis;'
        );
      }

      return null;
    }
  };
}

export default defineConfig({
  base: '/',
  plugins: [
    cspSafeDependencies(),
    vue(),
    VueI18nPlugin({
      include: path.resolve(adminConsoleRoot, './src/locales/**')
    }),
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
    outDir: '../HomeMesh.WebApi/wwwroot',
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
