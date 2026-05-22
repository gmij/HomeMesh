<template>
  <section class="audit-section">
    <div class="section-heading section-heading--audit">
      <div class="heading-copy">
        <h2>{{ $t('audit.title') }}</h2>
        <p>{{ $t('audit.description') }}</p>
      </div>
      <a-button :icon="h(FileSearchOutlined)" @click="emit('refresh')">{{ $t('audit.refresh') }}</a-button>
    </div>

    <article class="panel-card">
      <div v-if="audits.length" class="audit-list">
        <div v-for="audit in audits.slice(0, 12)" :key="audit.id" class="audit-item">
          <div class="audit-pill">{{ audit.type }}</div>
          <div class="audit-copy">
            <strong>{{ audit.message }}</strong>
            <span>{{ audit.actor || $t('audit.system_actor') }} · {{ formatTime(audit.createdAt) }}</span>
          </div>
        </div>
      </div>
      <a-empty v-else :image="simpleEmptyImage" :description="$t('audit.empty')" />
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
