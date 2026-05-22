<template>
  <div class="auth-shell">
    <a-card class="auth-card" :bordered="false">
      <div class="hero-brand">
        <div class="hero-mark">
          <span></span>
          <span></span>
        </div>
        <div>
          <div class="hero-title">{{ $t('hero.title') }}</div>
          <div class="hero-subtitle">{{ $t('hero.subtitle') }}</div>
        </div>
      </div>

      <div class="auth-copy">
        <h1>{{ mode === 'setup' ? $t('auth.setup_title') : $t('auth.login_title') }}</h1>
        <p>
          {{
            mode === 'setup'
              ? $t('auth.setup_description')
              : $t('auth.login_description')
          }}
        </p>
      </div>

      <a-form layout="vertical" class="auth-form" @submit.prevent="emit('submit')">
        <a-form-item :label="$t('auth.username_label')">
          <a-input
            :value="username"
            size="large"
            :placeholder="$t('auth.username_placeholder')"
            @update:value="emit('update:username', $event)"
            @pressEnter="emit('submit')"
          />
        </a-form-item>
        <a-form-item :label="$t('auth.password_label')">
          <a-input-password
            :value="password"
            size="large"
            :placeholder="$t('auth.password_placeholder')"
            @update:value="emit('update:password', $event)"
            @pressEnter="emit('submit')"
          />
        </a-form-item>
        <a-button type="primary" size="large" block :loading="loading" @click="emit('submit')">
          {{ mode === 'setup' ? $t('auth.setup_button') : $t('auth.login_button') }}
        </a-button>
      </a-form>

      <div class="auth-footnote">
        <span>{{ $t('auth.health_status_label') }}</span>
        <a-tag :color="healthTagColor">
          {{ localizedHealthStatus }}
        </a-tag>
      </div>
    </a-card>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import { useI18n } from 'vue-i18n';
import type { AuthMode } from '../types';

const props = defineProps<{
  mode: AuthMode;
  username: string;
  password: string;
  loading: boolean;
  healthStatus?: string | null;
}>();

const emit = defineEmits<{
  'update:username': [value: string];
  'update:password': [value: string];
  submit: [];
}>();

const { t } = useI18n();

const statusKeyMap: Record<string, string> = {
  healthy: 'auth.health_status_healthy',
  degraded: 'auth.health_status_degraded',
  unhealthy: 'auth.health_status_unhealthy',
  unknown: 'auth.health_status_unknown'
};

const localizedHealthStatus = computed(() => {
  const raw = props.healthStatus;
  if (!raw) {
    return t('auth.health_status_waiting');
  }

  const key = statusKeyMap[raw.trim().toLowerCase()];
  return key ? t(key) : raw;
});

const healthTagColor = computed(() => {
  return props.healthStatus?.trim().toLowerCase() === 'healthy' ? 'green' : 'default';
});
</script>
