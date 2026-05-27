<template>
  <header class="page-hero">
    <div class="page-hero-main">
      <div class="hero-brand">
        <div class="hero-mark">
          <span></span>
          <span></span>
        </div>
        <div class="hero-brand-copy">
          <div class="hero-title">{{ $t('hero.title') }}</div>
          <div class="hero-subtitle">{{ $t('hero.subtitle') }}</div>
        </div>
      </div>

      <div class="hero-tagline">{{ $t('hero.tagline') }}</div>
    </div>

    <div class="hero-actions">
      <div class="hero-toolbar">
        <a-button :icon="h(ReloadOutlined)" :loading="refreshing" @click="emit('refresh')">
          {{ $t('buttons.refresh') }}
        </a-button>
        <a-dropdown>
          <a-button>
            <span class="user-chip">
              <span class="user-avatar">{{ userInitial }}</span>
              <span>{{ userLabel }}</span>
            </span>
          </a-button>
          <template #overlay>
            <a-menu>
              <a-menu-item key="logout" @click="emit('logout')">
                <LogoutOutlined />
                <span>{{ $t('buttons.logout') }}</span>
              </a-menu-item>
            </a-menu>
          </template>
        </a-dropdown>
      </div>
    </div>
  </header>
</template>

<script setup lang="ts">
import { computed, h } from 'vue';
import { LogoutOutlined, ReloadOutlined } from '@ant-design/icons-vue';

const props = defineProps<{
  userLabel: string;
  refreshing: boolean;
}>();

const emit = defineEmits<{
  refresh: [];
  logout: [];
}>();

const userInitial = computed(() => props.userLabel.slice(0, 1).toUpperCase());
</script>
