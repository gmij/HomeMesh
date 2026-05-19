<template>
  <section :id="sectionId" class="prototype-section">
    <div class="section-heading">
      <div class="heading-badge">4</div>
      <div class="heading-copy">
        <h2>协议 Provider</h2>
        <p>当前启用中的 Provider 与后续规划统一放在这里。</p>
      </div>
      <div class="section-tools">
        <a-button :icon="h(ReloadOutlined)" :loading="refreshing" @click="emit('refresh')">
          刷新 Provider
        </a-button>
      </div>
    </div>

    <div class="workspace-frame" :class="{ 'workspace-frame--solo': !showRail }">
      <WorkspaceRail
        v-if="showRail"
        :active-key="prototypeSections.providers"
        :items="navItems"
        @navigate="emit('navigate', $event)"
      />

      <div class="workspace-body">
        <div class="provider-intro">
          当前已接入与规划中的协议能力，都在这里统一查看。
        </div>

        <div class="provider-grid">
          <article v-for="card in providerCards" :key="card.key" class="provider-card">
            <div class="provider-head">
              <div class="provider-logo" :class="`provider-logo--${card.tone}`">
                {{ card.badge }}
              </div>
              <div>
                <h3>{{ card.title }}</h3>
                <a-tag :color="card.tagColor">{{ card.stage }}</a-tag>
              </div>
            </div>

            <p>{{ card.description }}</p>

            <div class="definition-list">
              <div>
                <span>状态</span>
                <strong>{{ card.status }}</strong>
              </div>
              <div>
                <span>控制平面</span>
                <strong>{{ card.controlPlane }}</strong>
              </div>
              <div>
                <span>网络 ID</span>
                <strong>{{ card.networkId }}</strong>
              </div>
            </div>

            <a-button :type="card.actionType" block :disabled="card.disabled">
              {{ card.actionLabel }}
            </a-button>
          </article>
        </div>
      </div>
    </div>
  </section>
</template>

<script setup lang="ts">
import { h } from 'vue';
import { ReloadOutlined } from '@ant-design/icons-vue';

import type { ProviderCardModel, PrototypeSectionKey, SectionNavItem } from '../../types';
import { prototypeSections } from '../../constants';
import WorkspaceRail from '../WorkspaceRail.vue';

defineProps<{
  sectionId: string;
  navItems: SectionNavItem[];
  providerCards: ProviderCardModel[];
  refreshing: boolean;
  showRail?: boolean;
}>();

const emit = defineEmits<{
  navigate: [key: PrototypeSectionKey];
  refresh: [];
}>();
</script>
