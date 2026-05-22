<template>
  <NetworkListSection
    :section-id="`section-${prototypeSections.network}`"
    :nav-items="prototypeNavItems"
    :show-rail="false"
    :networks="networks"
    :provider-names="networkProviderNames"
    @navigate="navigateToSection"
    @open-create="createModalOpen = true"
    @open-network="openNetworkDetail"
  />
</template>

<script setup lang="ts">
import { useRouter } from 'vue-router';

import NetworkListSection from '../features/admin-console/components/sections/NetworkListSection.vue';
import { prototypeNavItems, prototypeSections, sectionPathMap } from '../features/admin-console/constants';
import { useAdminConsole } from '../features/admin-console/composables/useAdminConsole';
import type { PrototypeSectionKey } from '../features/admin-console/types';

const router = useRouter();

const {
  createModalOpen,
  networkProviderNames,
  networks,
  openNetwork
} = useAdminConsole();

function openNetworkDetail(networkId: string) {
  openNetwork(networkId);
  void router.push({ name: 'admin-network-detail', params: { networkId } });
}

function navigateToSection(key: PrototypeSectionKey) {
  if (router.currentRoute.value.path !== sectionPathMap[key]) {
    void router.push(sectionPathMap[key]);
  }
}
</script>
