<template>
  <section :id="sectionId" class="prototype-section">
    <div class="section-heading">
      <div class="heading-badge">2</div>
      <div class="heading-copy">
        <h2>{{ $t('network.title') }}</h2>
        <p>{{ $t('network.list_description') }}</p>
      </div>
      <div class="section-tools">
        <a-button type="primary" :icon="h(PlusOutlined)" @click="emit('open-create')">
          {{ $t('buttons.create_network') }}
        </a-button>
      </div>
    </div>

    <div class="workspace-frame" :class="{ 'workspace-frame--solo': !showRail }">
      <WorkspaceRail
        v-if="showRail"
        :active-key="prototypeSections.network"
        :items="navItems"
        @navigate="emit('navigate', $event)"
      />

      <div class="workspace-body">
        <article class="panel-card">
          <div class="panel-card__header">
            <h3>{{ $t('network.networks_section') }}</h3>
            <span>{{ $t('network.list_hint') }}</span>
          </div>

          <div v-if="networks.length" class="table-wrap">
            <table class="prototype-table">
              <thead>
                <tr>
                  <th>{{ $t('network.list_table_headers.name') }}</th>
                  <th>{{ $t('network.list_table_headers.provider') }}</th>
                  <th>{{ $t('network.list_table_headers.cidr') }}</th>
                  <th>{{ $t('network.list_table_headers.status') }}</th>
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
          <a-empty v-else :image="simpleEmptyImage" :description="$t('network.no_networks')" />
        </article>
      </div>
    </div>
  </section>
</template>

<script setup lang="ts">
import { h } from 'vue';
import { useI18n } from 'vue-i18n';
import { PlusOutlined } from '@ant-design/icons-vue';
import { Empty } from 'ant-design-vue';

import { prototypeSections } from '../../constants';
import type { NetworkSummary } from '../../../../api/types';
import type { PrototypeSectionKey, SectionNavItem } from '../../types';
import { statusColor } from '../../utils';
import WorkspaceRail from '../WorkspaceRail.vue';

const simpleEmptyImage = Empty.PRESENTED_IMAGE_SIMPLE;
const { t, te } = useI18n();

const props = defineProps<{
  sectionId: string;
  navItems: SectionNavItem[];
  networks: NetworkSummary[];
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

function localizedStatus(status: string) {
  const key = `network.status_values.${status.toLowerCase()}`;
  return te(key) ? t(key) : status;
}
</script>
