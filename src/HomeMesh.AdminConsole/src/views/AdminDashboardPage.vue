<template>
  <DashboardSection
    :section-id="`section-${prototypeSections.dashboard}`"
    :nav-items="prototypeNavItems"
    :metrics="summaryMetrics"
    :networks="networks"
    :recent-audits="recentAudits"
    :provider-names="networkProviderNames"
    :show-rail="false"
    @navigate="navigateToSection"
    @open-create="createModalOpen = true"
    @open-network="openNetworkSection"
    @refresh-audits="loadAudits"
  />
</template>

<script setup lang="ts">
import { useRouter } from 'vue-router';

import DashboardSection from '../features/admin-console/components/sections/DashboardSection.vue';
import { prototypeNavItems, prototypeSections, sectionPathMap } from '../features/admin-console/constants';
import { useAdminConsole } from '../features/admin-console/composables/useAdminConsole';
import type { PrototypeSectionKey } from '../features/admin-console/types';

const router = useRouter();

const {
  createModalOpen,
  loadAudits,
  networkProviderNames,
  networks,
  openNetwork,
  recentAudits,
  summaryMetrics
} = useAdminConsole();

function openNetworkSection(networkId: string) {
  openNetwork(networkId);
  navigateToSection('network');
}

function navigateToSection(key: PrototypeSectionKey) {
  if (router.currentRoute.value.path !== sectionPathMap[key]) {
    void router.push(sectionPathMap[key]);
  }
}
</script>
