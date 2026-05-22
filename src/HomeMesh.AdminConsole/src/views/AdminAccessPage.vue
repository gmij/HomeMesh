<template>
  <AccessSection
    :section-id="`section-${prototypeSections.access}`"
    :nav-items="prototypeNavItems"
    :show-rail="false"
    :selected-network-id="selectedNetworkId"
    :network-options="networkSelectOptions"
    :expiry-options="translatedExpiryOptions"
    :access-form="accessForm"
    :access-network-name="accessNetworkName"
    :access-code="accessCode"
    :qr-cells="qrCells"
    @navigate="navigateToSection"
    @update:selected-network-id="selectedNetworkId = $event"
    @update:access-form="updateAccessForm"
    @generate-access-artifact="generateAccessArtifact"
    @download-plant-file="downloadPlantFile"
    @download-moon-file="downloadMoonFile"
    @copy-access-code="copyAccessCode"
  />
</template>

<script setup lang="ts">
import { computed } from 'vue';
import { useRouter } from 'vue-router';
import { useI18n } from 'vue-i18n';

import AccessSection from '../features/admin-console/components/sections/AccessSection.vue';
import {
  accessExpiryOptions,
  prototypeNavItems,
  prototypeSections,
  sectionPathMap
} from '../features/admin-console/constants';
import { useAdminConsole } from '../features/admin-console/composables/useAdminConsole';
import type { PrototypeSectionKey } from '../features/admin-console/types';

const router = useRouter();
const { t } = useI18n();

const translatedExpiryOptions = computed(() =>
  accessExpiryOptions.map(opt => ({ ...opt, label: t(opt.label) }))
);

const {
  accessCode,
  accessForm,
  accessNetworkName,
  copyAccessCode,
  downloadMoonFile,
  downloadPlantFile,
  generateAccessArtifact,
  networkSelectOptions,
  qrCells,
  selectedNetworkId,
  updateAccessForm
} = useAdminConsole();

function navigateToSection(key: PrototypeSectionKey) {
  if (router.currentRoute.value.path !== sectionPathMap[key]) {
    void router.push(sectionPathMap[key]);
  }
}
</script>
