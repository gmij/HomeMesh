<template>
  <NetworkSection
    :section-id="`section-${prototypeSections.network}`"
    :nav-items="prototypeNavItems"
    :show-rail="false"
    :hide-section-heading="true"
    :selected-network-id="selectedNetworkId"
    :network-options="networkSelectOptions"
    :selected-network="selectedNetwork"
    :selected-binding="selectedBinding"
    :formatted-last-audit="formattedLastAudit"
    :network-tab="networkTab"
    :detail-metrics="detailMetrics"
    :members="members"
    :routes="routes"
    :pools="pools"
    :dns-config="dnsConfig"
    :delete-loading="deleteLoading"
    :route-form="{ target: routeForm.target, via: routeForm.via }"
    :pool-form="{ ipRangeStart: poolForm.ipRangeStart, ipRangeEnd: poolForm.ipRangeEnd }"
    :dns-form="dnsForm"
    :member-ip-values="memberIpValues"
    :member-tag-values="memberTagValues"
    @navigate="navigateToSection"
    @update:selected-network-id="onSelectedNetworkChange"
    @update:network-tab="networkTab = $event"
    @open-easy-setup="easySetupModalOpen = true"
    @sync-members="syncMembers"
    @sync-config="syncConfig"
    @delete-network="deleteNetwork"
    @update:member-ip="onMemberIpInput"
    @update:member-tag="onMemberTagInput"
    @toggle-auth="toggleMemberAuth"
    @assign-ip="assignIp"
    @update:route-form="updateRouteForm"
    @create-route="createRoute"
    @delete-route="deleteRoute"
    @update:pool-form="updatePoolForm"
    @create-pool="createPool"
    @delete-pool="deletePool"
    @update:dns-form="updateDnsForm"
    @save-dns="saveDns"
  />
</template>

<script setup lang="ts">
import { computed, watch } from 'vue';
import { useRoute, useRouter } from 'vue-router';

import NetworkSection from '../features/admin-console/components/sections/NetworkSection.vue';
import { prototypeNavItems, prototypeSections, sectionPathMap } from '../features/admin-console/constants';
import { useAdminConsole } from '../features/admin-console/composables/useAdminConsole';
import type { PrototypeSectionKey } from '../features/admin-console/types';

const route = useRoute();
const router = useRouter();

const {
  assignIp,
  createPool,
  createRoute,
  deleteLoading,
  deleteNetwork,
  deletePool,
  deleteRoute,
  detailMetrics,
  dnsConfig,
  dnsForm,
  easySetupModalOpen,
  formattedLastAudit,
  memberIpValues,
  memberTagValues,
  members,
  networkSelectOptions,
  networkTab,
  onMemberIpInput,
  onMemberTagInput,
  openNetwork,
  poolForm,
  pools,
  routeForm,
  routes,
  saveDns,
  selectedBinding,
  selectedNetwork,
  selectedNetworkId,
  syncConfig,
  syncMembers,
  toggleMemberAuth,
  updateDnsForm,
  updatePoolForm,
  updateRouteForm
} = useAdminConsole();

const routeNetworkId = computed(() => {
  const { networkId } = route.params;
  return typeof networkId === 'string' ? networkId : '';
});

watch(
  routeNetworkId,
  (networkId) => {
    if (!networkId) {
      void router.replace({ name: 'admin-network' });
      return;
    }

    if (selectedNetworkId.value !== networkId) {
      openNetwork(networkId);
    }
  },
  { immediate: true }
);

watch(selectedNetworkId, (networkId) => {
  if (!networkId) {
    void router.replace({ name: 'admin-network' });
    return;
  }

  if (networkId !== routeNetworkId.value) {
    void router.replace({ name: 'admin-network-detail', params: { networkId } });
  }
});

function onSelectedNetworkChange(networkId: string) {
  if (!networkId) {
    return;
  }

  openNetwork(networkId);
  void router.push({ name: 'admin-network-detail', params: { networkId } });
}

function navigateToSection(key: PrototypeSectionKey) {
  if (router.currentRoute.value.path !== sectionPathMap[key]) {
    void router.push(sectionPathMap[key]);
  }
}
</script>
