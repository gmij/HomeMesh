<template>
  <div class="admin-console">
    <AdminAuthScreen
      v-if="authMode !== 'ready'"
      :mode="authMode"
      :username="authForm.username"
      :password="authForm.password"
      :loading="authLoading"
      :health-status="healthStatus?.status"
      @update:username="authForm.username = $event"
      @update:password="authForm.password = $event"
      @submit="submitAuth"
    />

    <div v-else class="console-page">
      <AdminHero :user-label="userLabel" :refreshing="refreshing" @refresh="refreshAll" @logout="logout" />

      <div class="page-shell">
        <WorkspaceRail
          :active-key="currentSection"
          :items="prototypeNavItems"
          @navigate="navigateToSection"
        />

        <div class="page-main">
          <div class="page-stage">
            <router-view />
          </div>
        </div>
      </div>

      <footer class="page-footer">
        <span>{{ $t('common.footer') }}</span>
      </footer>
    </div>

    <CreateNetworkModal
      :open="createModalOpen"
      :loading="createLoading"
      :provider-options="providerOptions"
      :form="createForm"
      @update:open="createModalOpen = $event"
      @update:form="updateCreateForm"
      @submit="createNetwork"
    />

    <EasySetupModal
      :open="easySetupModalOpen"
      :loading="easySetupLoading"
      :form="easySetupForm"
      @update:open="easySetupModalOpen = $event"
      @update:form="updateEasySetupForm"
      @submit="applyEasySetup"
    />
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import { useRoute, useRouter } from 'vue-router';

import '../features/admin-console/styles/index.css';
import AdminAuthScreen from '../features/admin-console/components/AdminAuthScreen.vue';
import AdminHero from '../features/admin-console/components/AdminHero.vue';
import CreateNetworkModal from '../features/admin-console/components/CreateNetworkModal.vue';
import EasySetupModal from '../features/admin-console/components/EasySetupModal.vue';
import WorkspaceRail from '../features/admin-console/components/WorkspaceRail.vue';
import { prototypeNavItems, sectionPathMap } from '../features/admin-console/constants';
import { useAdminConsole } from '../features/admin-console/composables/useAdminConsole';
import type { PrototypeSectionKey } from '../features/admin-console/types';

const route = useRoute();
const router = useRouter();

const {
  applyEasySetup,
  authForm,
  authLoading,
  authMode,
  createForm,
  createLoading,
  createModalOpen,
  createNetwork,
  easySetupForm,
  easySetupLoading,
  easySetupModalOpen,
  healthStatus,
  logout,
  providerOptions,
  refreshAll,
  refreshing,
  submitAuth,
  updateCreateForm,
  updateEasySetupForm,
  userLabel
} = useAdminConsole();

const currentSection = computed<PrototypeSectionKey>(() => {
  const section = route.meta.section;
  return section === 'network' || section === 'access' || section === 'providers' ? section : 'dashboard';
});

function navigateToSection(key: PrototypeSectionKey) {
  if (router.currentRoute.value.path !== sectionPathMap[key]) {
    void router.push(sectionPathMap[key]);
  }
}
</script>
