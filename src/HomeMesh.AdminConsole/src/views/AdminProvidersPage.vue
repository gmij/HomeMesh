<template>
  <ProvidersSection
    :section-id="`section-${prototypeSections.providers}`"
    :nav-items="prototypeNavItems"
    :show-rail="false"
    :provider-cards="providerCards"
    :refreshing="refreshing"
    @navigate="navigateToSection"
    @refresh="refreshAll"
  />
</template>

<script setup lang="ts">
import { useRouter } from 'vue-router';

import ProvidersSection from '../features/admin-console/components/sections/ProvidersSection.vue';
import { prototypeNavItems, prototypeSections, sectionPathMap } from '../features/admin-console/constants';
import { useAdminConsole } from '../features/admin-console/composables/useAdminConsole';
import type { PrototypeSectionKey } from '../features/admin-console/types';

const router = useRouter();

const { providerCards, refreshAll, refreshing } = useAdminConsole();

function navigateToSection(key: PrototypeSectionKey) {
  if (router.currentRoute.value.path !== sectionPathMap[key]) {
    void router.push(sectionPathMap[key]);
  }
}
</script>
