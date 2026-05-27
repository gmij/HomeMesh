<template>
  <section :id="sectionId" class="prototype-section">
    <div class="section-heading">
      <div class="heading-badge">1</div>
      <div class="heading-copy">
        <h2>{{ $t('dashboard.title') }}</h2>
        <p>{{ $t('dashboard.description') }}</p>
      </div>
      <div class="section-tools">
        <a-button :icon="h(BellOutlined)">{{ $t('buttons.notification') }}</a-button>
        <a-button type="primary" :icon="h(PlusOutlined)" @click="emit('open-create')">
          {{ $t('buttons.create_network') }}
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
              <div class="stat-head">
                <div class="stat-text">
                  <div class="stat-label">{{ metric.label }}</div>
                  <div class="stat-meta">{{ metric.meta }}</div>
                </div>
                <div class="stat-value">{{ metric.value }}</div>
              </div>
            </div>
          </article>
        </div>

        <div class="content-split">
          <article class="panel-card">
            <div class="panel-card__header">
              <h3>{{ $t('dashboard.networks_section') }}</h3>
              <a-button type="link" @click="emit('navigate', prototypeSections.network)">
                {{ $t('dashboard.networks_link') }}
              </a-button>
            </div>

            <div v-if="networks.length" class="table-wrap">
              <table class="prototype-table">
                <thead>
                  <tr>
                    <th>{{ $t('dashboard.table_headers.name') }}</th>
                    <th>{{ $t('dashboard.table_headers.provider') }}</th>
                    <th>{{ $t('dashboard.table_headers.cidr') }}</th>
                    <th>{{ $t('dashboard.table_headers.status') }}</th>
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
                    <td>{{ network.cidr ?? $t('network.auto_assigned') }}</td>
                    <td>
                      <a-tag :color="statusColor(network.status)">{{ localizedStatus(network.status) }}</a-tag>
                    </td>
                  </tr>
                </tbody>
              </table>
            </div>
            <a-empty v-else :image="simpleEmptyImage" :description="$t('dashboard.no_networks')" />
          </article>

          <article class="panel-card">
            <div class="panel-card__header">
              <h3>{{ $t('dashboard.recent_events') }}</h3>
              <a-button type="link" @click="emit('refresh-audits')">{{ $t('dashboard.refresh_events') }}</a-button>
            </div>

            <div v-if="recentAudits.length" class="event-list">
              <div v-for="audit in recentAudits" :key="audit.id" class="event-item">
                <div class="event-time">{{ formatTime(audit.createdAt, 'time') }}</div>
                <div class="event-copy">
                  <strong>{{ audit.message }}</strong>
                  <span>{{ audit.actor || $t('audit.system_actor') }}</span>
                </div>
              </div>
            </div>
            <a-empty v-else :image="simpleEmptyImage" :description="$t('dashboard.no_events')" />
          </article>
        </div>
      </div>
    </div>
  </section>
</template>

<script setup lang="ts">
import { h } from 'vue';
import { useI18n } from 'vue-i18n';
import { BellOutlined, PlusOutlined } from '@ant-design/icons-vue';
import { Empty } from 'ant-design-vue';

import { prototypeSections } from '../../constants';
import type { AuditLog, NetworkSummary } from '../../../../api/types';
import type { PrototypeSectionKey, SectionNavItem, SummaryMetric } from '../../types';
import { formatTime, statusColor } from '../../utils';
import WorkspaceRail from '../WorkspaceRail.vue';

const simpleEmptyImage = Empty.PRESENTED_IMAGE_SIMPLE;
const { t, te } = useI18n();

const props = defineProps<{
  sectionId: string;
  navItems: SectionNavItem[];
  metrics: SummaryMetric[];
  networks: NetworkSummary[];
  recentAudits: AuditLog[];
  providerNames: Record<string, string>;
  showRail?: boolean;
}>();

const emit = defineEmits<{
  navigate: [key: PrototypeSectionKey];
  'open-create': [];
  'open-network': [networkId: string];
  'refresh-audits': [];
}>();

function providerForNetwork(networkId: string) {
  return props.providerNames[networkId] ?? '-';
}

function localizedStatus(status: string) {
  const key = `network.status_values.${status.toLowerCase()}`;
  return te(key) ? t(key) : status;
}
</script>
