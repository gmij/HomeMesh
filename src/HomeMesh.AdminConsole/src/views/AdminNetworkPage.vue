<template>
  <NetworkSection
    :section-id="`section-${prototypeSections.network}`"
    :nav-items="prototypeNavItems"
    :show-rail="false"
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
    :sync-cards="syncCards"
    :route-form="{ target: routeForm.target, via: routeForm.via }"
    :pool-form="{ ipRangeStart: poolForm.ipRangeStart, ipRangeEnd: poolForm.ipRangeEnd }"
    :dns-form="dnsForm"
    :member-ip-values="memberIpValues"
    @navigate="navigateToSection"
    @update:selected-network-id="selectedNetworkId = $event"
    @update:network-tab="networkTab = $event"
    @open-easy-setup="easySetupModalOpen = true"
    @sync-members="syncMembers"
    @sync-config="syncConfig"
    @update:member-ip="onMemberIpInput"
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
import { useRouter } from 'vue-router';

import NetworkSection from '../features/admin-console/components/sections/NetworkSection.vue';
import { prototypeNavItems, prototypeSections, sectionPathMap } from '../features/admin-console/constants';
import { useAdminConsole } from '../features/admin-console/composables/useAdminConsole';
import type { PrototypeSectionKey } from '../features/admin-console/types';

const router = useRouter();

const {
  assignIp,
  createPool,
  createRoute,
  deletePool,
  deleteRoute,
  detailMetrics,
  dnsConfig,
  dnsForm,
  easySetupModalOpen,
  formattedLastAudit,
  memberIpValues,
  members,
  networkSelectOptions,
  networkTab,
  onMemberIpInput,
  poolForm,
  pools,
  routeForm,
  routes,
  saveDns,
  selectedBinding,
  selectedNetwork,
  selectedNetworkId,
  syncCards,
  syncConfig,
  syncMembers,
  toggleMemberAuth,
  updateDnsForm,
  updatePoolForm,
  updateRouteForm
} = useAdminConsole();

function navigateToSection(key: PrototypeSectionKey) {
  if (router.currentRoute.value.path !== sectionPathMap[key]) {
    void router.push(sectionPathMap[key]);
  }
}
</script>
