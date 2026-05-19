<template>
  <div class="auth-shell">
    <a-card class="auth-card" :bordered="false">
      <div class="hero-brand">
        <div class="hero-mark">
          <span></span>
          <span></span>
        </div>
        <div>
          <div class="hero-title">HomeMesh</div>
          <div class="hero-subtitle">Home SD-WAN Controller</div>
        </div>
      </div>

      <div class="auth-copy">
        <h1>{{ mode === 'setup' ? '初始化管理员账户' : '登录管理控制台' }}</h1>
        <p>
          {{
            mode === 'setup'
              ? '先创建第一位管理员，我们再把控制台接起来。'
              : '输入管理员账号，进入新的 Vue 控制台。'
          }}
        </p>
      </div>

      <a-form layout="vertical" class="auth-form" @submit.prevent="emit('submit')">
        <a-form-item label="用户名">
          <a-input
            :value="username"
            size="large"
            placeholder="admin"
            @update:value="emit('update:username', $event)"
            @pressEnter="emit('submit')"
          />
        </a-form-item>
        <a-form-item label="密码">
          <a-input-password
            :value="password"
            size="large"
            placeholder="请输入密码"
            @update:value="emit('update:password', $event)"
            @pressEnter="emit('submit')"
          />
        </a-form-item>
        <a-button type="primary" size="large" block :loading="loading" @click="emit('submit')">
          {{ mode === 'setup' ? '创建管理员' : '登录' }}
        </a-button>
      </a-form>

      <div class="auth-footnote">
        <span>当前健康状态</span>
        <a-tag :color="healthStatus === 'Healthy' ? 'green' : 'default'">
          {{ healthStatus ?? '等待检测' }}
        </a-tag>
      </div>
    </a-card>
  </div>
</template>

<script setup lang="ts">
import type { AuthMode } from '../types';

defineProps<{
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
</script>
