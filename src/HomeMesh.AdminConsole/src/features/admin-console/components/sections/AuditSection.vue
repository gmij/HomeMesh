<template>
  <section class="audit-section">
    <div class="section-heading section-heading--audit">
      <div class="heading-copy">
        <h2>审计日志</h2>
        <p>补一条底部时间线，方便看最近操作有没有落库。</p>
      </div>
      <a-button :icon="h(FileSearchOutlined)" @click="emit('refresh')">刷新日志</a-button>
    </div>

    <article class="panel-card">
      <div v-if="audits.length" class="audit-list">
        <div v-for="audit in audits.slice(0, 12)" :key="audit.id" class="audit-item">
          <div class="audit-pill">{{ audit.type }}</div>
          <div class="audit-copy">
            <strong>{{ audit.message }}</strong>
            <span>{{ audit.actor || 'system' }} · {{ formatTime(audit.createdAt) }}</span>
          </div>
        </div>
      </div>
      <a-empty v-else :image="simpleEmptyImage" description="暂无审计日志" />
    </article>
  </section>
</template>

<script setup lang="ts">
import { h } from 'vue';
import { FileSearchOutlined } from '@ant-design/icons-vue';
import { Empty } from 'ant-design-vue';

import type { AuditLog } from '../../../../api/types';
import { formatTime } from '../../utils';

const simpleEmptyImage = Empty.PRESENTED_IMAGE_SIMPLE;

defineProps<{
  audits: AuditLog[];
}>();

const emit = defineEmits<{
  refresh: [];
}>();
</script>
