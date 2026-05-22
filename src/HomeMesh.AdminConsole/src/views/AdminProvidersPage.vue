<template>
  <ProvidersSection
    :section-id="`section-${prototypeSections.providers}`"
    :nav-items="prototypeNavItems"
    :show-rail="false"
    :provider-cards="providerCards"
    :refreshing="refreshing"
    @navigate="navigateToSection"
    @refresh="refreshAll"
    @card-action="onCardAction"
  />

  <a-modal
    v-model:open="configModalOpen"
    :title="$t('providers.config_modal.title')"
    :footer="null"
    width="600"
  >
    <div style="display: flex; flex-direction: column; gap: 14px; padding: 4px 0 8px">
      <div style="display: flex; align-items: center; gap: 8px">
        <a-tag :color="activeCardStatus === 'Error' ? 'red' : 'green'">
          {{ activeCardStatus }}
        </a-tag>
        <span style="color: #64748b; font-size: 13px">{{ activeCardMessage }}</span>
      </div>

      <a-form layout="vertical">
        <a-form-item :label="$t('providers.config_modal.auth_token_path_label')">
          <a-input v-model:value="configForm.authTokenPath" :placeholder="$t('providers.config_modal.auth_token_path_placeholder')" />
        </a-form-item>

        <a-form-item :label="$t('providers.config_modal.port_label')">
          <a-input-number v-model:value="configForm.port" :min="1" :max="65535" style="width: 160px" />
        </a-form-item>

        <a-form-item :label="$t('providers.config_modal.enabled_label')">
          <a-switch v-model:checked="configForm.enabled" />
        </a-form-item>
      </a-form>

      <a-alert
        v-if="activeCardStatus === 'Error'"
        :message="$t('providers.config_modal.how_to_fix_title')"
        :description="$t('providers.config_modal.zerotier_fix_desc')"
        type="warning"
        show-icon
      />

      <a-alert
        v-if="testResult"
        :message="testResult.status"
        :description="testResult.detail ? `${testResult.message}\n${testResult.detail}` : testResult.message"
        :type="testResult.status === 'Healthy' ? 'success' : testResult.status === 'Warning' ? 'warning' : 'error'"
        show-icon
      />

      <a-alert
        v-if="saveResultMessage"
        :message="$t('providers.config_modal.saved_title')"
        :description="saveResultMessage"
        type="info"
        show-icon
      />

      <p style="color: #64748b; font-size: 12px; margin: 0">
        {{ $t('providers.config_modal.zerotier_restart_hint') }}
      </p>

      <div style="display: flex; justify-content: flex-end; gap: 8px">
        <a-button :loading="testingConfig" @click="testCurrentConfig">
          {{ $t('providers.config_modal.test_button') }}
        </a-button>
        <a-button type="primary" :loading="savingConfig" @click="saveCurrentConfig">
          {{ $t('providers.config_modal.save_button') }}
        </a-button>
      </div>
    </div>
  </a-modal>
</template>

<script setup lang="ts">
import { reactive, ref } from 'vue';
import { message } from 'ant-design-vue';
import { useI18n } from 'vue-i18n';
import { useRouter } from 'vue-router';

import { request } from '../api/client';
import type { ZeroTierConfig, ZeroTierConfigSaveResult, ZeroTierTestResult } from '../api/types';
import ProvidersSection from '../features/admin-console/components/sections/ProvidersSection.vue';
import { prototypeNavItems, prototypeSections, sectionPathMap } from '../features/admin-console/constants';
import { useAdminConsole } from '../features/admin-console/composables/useAdminConsole';
import type { PrototypeSectionKey } from '../features/admin-console/types';

const router = useRouter();
const { t } = useI18n();

const { providerCards, refreshAll, refreshing } = useAdminConsole();

const configModalOpen = ref(false);
const activeCardStatus = ref('');
const activeCardMessage = ref('');
const testingConfig = ref(false);
const savingConfig = ref(false);
const testResult = ref<ZeroTierTestResult | null>(null);
const saveResultMessage = ref('');

const configForm = reactive<ZeroTierConfig>({
  enabled: true,
  port: 9993,
  authTokenPath: '/var/lib/zerotier-one/authtoken.secret'
});

async function loadConfig() {
  try {
    const config = await request<ZeroTierConfig>('/api/providers/zerotier/config');
    configForm.enabled = config.enabled;
    configForm.port = config.port;
    configForm.authTokenPath = config.authTokenPath;
  } catch {
    message.warning(t('providers.config_modal.load_config_failed'));
  }
}

async function testCurrentConfig() {
  testingConfig.value = true;
  try {
    testResult.value = await request<ZeroTierTestResult>('/api/providers/zerotier/test-config', {
      method: 'POST',
      body: JSON.stringify(configForm)
    });
  } catch (error) {
    testResult.value = {
      status: 'Error',
      message: t('providers.config_modal.test_failed'),
      detail: error instanceof Error ? error.message : String(error)
    };
  } finally {
    testingConfig.value = false;
  }
}

async function saveCurrentConfig() {
  savingConfig.value = true;
  try {
    const result = await request<ZeroTierConfigSaveResult>('/api/providers/zerotier/config', {
      method: 'PUT',
      body: JSON.stringify(configForm)
    });

    saveResultMessage.value = result.message;
    message.success(t('providers.config_modal.save_success'));
  } catch (error) {
    message.error(error instanceof Error ? error.message : t('providers.config_modal.save_failed'));
  } finally {
    savingConfig.value = false;
  }
}

function onCardAction(key: string) {
  if (key === 'zerotier') {
    const card = providerCards.value.find(c => c.key === 'zerotier');
    activeCardStatus.value = card?.status ?? '';
    activeCardMessage.value = card?.controlPlane ?? '';
    testResult.value = null;
    saveResultMessage.value = '';
    configModalOpen.value = true;
    void loadConfig();
  }
}

function navigateToSection(key: PrototypeSectionKey) {
  if (router.currentRoute.value.path !== sectionPathMap[key]) {
    void router.push(sectionPathMap[key]);
  }
}
</script>
