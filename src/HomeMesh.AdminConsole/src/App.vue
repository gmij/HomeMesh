<template>
  <a-config-provider :theme="theme">
    <router-view />
  </a-config-provider>
</template>

<script setup lang="ts">
const theme = {
  token: {
    colorPrimary: '#2563eb',
    borderRadius: 12,
    fontFamily:
      'Inter, system-ui, -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif',
    colorBgLayout: '#f3f6fb'
  }
};
</script>
