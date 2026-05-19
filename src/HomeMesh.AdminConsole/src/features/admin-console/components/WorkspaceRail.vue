<template>
  <aside class="workspace-rail">
    <div class="workspace-brand">
      <div class="hero-mark">
        <span></span>
        <span></span>
      </div>
      <div>
        <strong>HomeMesh</strong>
        <small>Home SD-WAN Controller</small>
      </div>
    </div>

    <nav class="workspace-nav">
      <button
        v-for="item in items"
        :key="item.key"
        type="button"
        class="workspace-link"
        :class="{ 'is-active': activeKey === item.key }"
        @click="emit('navigate', item.key)"
      >
        <component :is="iconMap[item.key]" />
        <span>{{ item.label }}</span>
      </button>

      <button
        v-for="item in secondaryItems"
        :key="item.key"
        type="button"
        class="workspace-link workspace-link--muted workspace-link--secondary"
      >
        <component :is="item.icon" />
        <span>{{ item.label }}</span>
      </button>
    </nav>
  </aside>
</template>

<script setup lang="ts">
import {
  ApiOutlined,
  AppstoreOutlined,
  ApartmentOutlined,
  DashboardOutlined,
  DeploymentUnitOutlined,
  SafetyCertificateOutlined,
  SettingOutlined
} from '@ant-design/icons-vue';

import type { PrototypeSectionKey, SectionNavItem } from '../types';

defineProps<{
  activeKey: PrototypeSectionKey;
  items: SectionNavItem[];
}>();

const emit = defineEmits<{
  navigate: [key: PrototypeSectionKey];
}>();

const iconMap = {
  dashboard: DashboardOutlined,
  network: ApartmentOutlined,
  access: DeploymentUnitOutlined,
  providers: ApiOutlined
};

const secondaryItems = [
  { key: 'devices', label: '设备管理', icon: AppstoreOutlined },
  { key: 'policy', label: '访问策略', icon: SafetyCertificateOutlined },
  { key: 'settings', label: '系统设置', icon: SettingOutlined }
];
</script>
