<template>
  <section :id="sectionId" class="prototype-section">
    <div class="section-heading">
      <div class="heading-badge">1</div>
      <div class="heading-copy">
        <h2>控制台总览</h2>
        <p>网络、设备、网关与同步概况集中在一屏里。</p>
      </div>
      <div class="section-tools">
        <a-button :icon="h(BellOutlined)">通知</a-button>
        <a-button type="primary" :icon="h(PlusOutlined)" @click="emit('open-create')">
          新建网络
        </a-button>
      </div>
    </div>

    <div class="workspace-frame" :class="{ 'workspace-frame--solo': !showRail }">
      <WorkspaceRail
        v-if="showRail"
        :active-key="prototypeSections.dashboard"
        :items="navItems"
        @navigate="emit('navigate', $event)"
      />

      <div class="workspace-body">
        <div class="stat-grid">
          <article v-for="metric in metrics" :key="metric.label" class="stat-tile">
            <div class="stat-icon" :class="`tone-${metric.tone}`">
              <component :is="metric.icon" />
            </div>
            <div class="stat-copy">
              <div class="stat-label">{{ metric.label }}</div>
              <div class="stat-value">{{ metric.value }}</div>
              <div class="stat-meta">{{ metric.meta }}</div>
            </div>
          </article>
        </div>

        <div class="content-split">
          <article class="panel-card">
            <div class="panel-card__header">
              <h3>网络列表</h3>
              <a-button type="link" @click="emit('navigate', prototypeSections.network)">
                打开网络区
              </a-button>
            </div>

            <div v-if="networks.length" class="table-wrap">
              <table class="prototype-table">
                <thead>
                  <tr>
                    <th>网络名称</th>
                    <th>Provider</th>
                    <th>CIDR</th>
                    <th>状态</th>
                  </tr>
                </thead>
                <tbody>
                  <tr
                    v-for="network in networks"
                    :key="network.id"
                    class="table-row--clickable"
                    @click="emit('open-network', network.id)"
                  >
                    <td>
                      <button type="button" class="link-button">{{ network.name }}</button>
                    </td>
                    <td>{{ providerForNetwork(network.id) }}</td>
                    <td>{{ network.cidr ?? '自动分配' }}</td>
                    <td>
                      <a-tag :color="statusColor(network.status)">{{ network.status }}</a-tag>
                    </td>
                  </tr>
                </tbody>
              </table>
            </div>
            <a-empty v-else :image="simpleEmptyImage" description="还没有创建网络" />
          </article>

          <article class="panel-card panel-card--status">
            <div class="panel-card__header">
              <h3>控制台状态</h3>
              <a-button type="link" @click="emit('navigate', prototypeSections.providers)">查看 Provider</a-button>
            </div>

            <div class="status-list">
              <div v-for="item in statusItems" :key="item.key" class="status-item">
                <div class="stat-icon" :class="`tone-${item.tone}`">
                  <span class="status-pill">{{ item.label }}</span>
                </div>
                <div class="status-copy">
                  <strong>{{ item.value }}</strong>
                  <span>{{ item.note }}</span>
                </div>
              </div>
            </div>
          </article>
        </div>
      </div>
    </div>
  </section>
</template>

<script setup lang="ts">
import { h } from 'vue';
import { BellOutlined, PlusOutlined } from '@ant-design/icons-vue';
import { Empty } from 'ant-design-vue';

import { prototypeSections } from '../../constants';
import type { NetworkSummary } from '../../../../api/types';
import type { DashboardStatusItem, PrototypeSectionKey, SectionNavItem, SummaryMetric } from '../../types';
import { statusColor } from '../../utils';
import WorkspaceRail from '../WorkspaceRail.vue';

const simpleEmptyImage = Empty.PRESENTED_IMAGE_SIMPLE;

const props = defineProps<{
  sectionId: string;
  navItems: SectionNavItem[];
  metrics: SummaryMetric[];
  networks: NetworkSummary[];
  statusItems: DashboardStatusItem[];
  providerNames: Record<string, string>;
  showRail?: boolean;
}>();

const emit = defineEmits<{
  navigate: [key: PrototypeSectionKey];
  'open-create': [];
  'open-network': [networkId: string];
}>();

function providerForNetwork(networkId: string) {
  return props.providerNames[networkId] ?? '-';
}
</script>
