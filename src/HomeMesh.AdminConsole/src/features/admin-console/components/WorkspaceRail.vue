<template>
  <aside class="workspace-rail">
    <div class="workspace-brand">
      <div class="hero-mark">
        <span></span>
        <span></span>
      </div>
      <div>
        <strong>HomeMesh</strong>
        <small>{{ $t('hero.subtitle') }}</small>
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
        <span>{{ t(item.label) }}</span>
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
import { computed } from 'vue';
import { useI18n } from 'vue-i18n';
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

const { t } = useI18n();

const iconMap = {
  dashboard: DashboardOutlined,
  network: ApartmentOutlined,
  access: DeploymentUnitOutlined,
  providers: ApiOutlined
};

const secondaryItems = computed(() => [
  { key: 'devices', label: t('secondary_nav.devices'), icon: AppstoreOutlined },
  { key: 'policy', label: t('secondary_nav.policy'), icon: SafetyCertificateOutlined },
  { key: 'settings', label: t('secondary_nav.settings'), icon: SettingOutlined }
]);
</script>
