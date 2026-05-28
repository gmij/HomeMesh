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
    :width="520"
    centered
    wrap-class-name="providers-config-modal"
  >
    <div class="providers-config-panel">
      <div class="providers-config-panel__status">
        <a-tag :color="activeCardStatusColor">
          {{ activeCardStatus }}
        </a-tag>
        <span>{{ activeCardMessage }}</span>
      </div>

      <a-form layout="vertical" class="providers-config-panel__form">
        <a-form-item :label="$t('providers.config_modal.auth_token_path_label')">
          <a-input
            v-model:value="configForm.authTokenPath"
            :placeholder="$t('providers.config_modal.auth_token_path_placeholder')"
          />
        </a-form-item>

        <div class="providers-config-panel__row">
          <a-form-item
            class="providers-config-panel__field providers-config-panel__field--port"
            :label="$t('providers.config_modal.port_label')"
          >
            <a-input-number v-model:value="configForm.port" :min="1" :max="65535" />
          </a-form-item>

          <a-form-item
            class="providers-config-panel__field providers-config-panel__field--toggle"
            :label="$t('providers.config_modal.enabled_label')"
          >
            <a-switch v-model:checked="configForm.enabled" />
          </a-form-item>
        </div>
      </a-form>

      <div
        v-if="activeCardStatusTone === 'danger' || testResult || saveResultMessage"
        class="providers-config-feedback"
      >
        <div
          v-if="activeCardStatusTone === 'danger'"
          class="providers-config-feedback__item providers-config-feedback__item--warning"
        >
          <strong>{{ $t('providers.config_modal.how_to_fix_title') }}</strong>
          <span>{{ $t('providers.config_modal.zerotier_fix_desc') }}</span>
        </div>

        <div
          v-if="testResult && testResult.status !== 'Healthy'"
          class="providers-config-feedback__item"
          :class="{
            'providers-config-feedback__item--success': testResult.status === 'Healthy',
            'providers-config-feedback__item--warning': testResult.status === 'Warning',
            'providers-config-feedback__item--danger': testResult.status === 'Error'
          }"
        >
          <strong>{{ localizeStatus(testResult.status) }}</strong>
          <span>{{ localizeZeroTierMessage(testResult.message) }}</span>
        </div>

        <div
          v-if="saveResultMessage"
          class="providers-config-feedback__item providers-config-feedback__item--info"
        >
          <strong>{{ $t('providers.config_modal.saved_title') }}</strong>
          <span>{{ saveResultMessage }}</span>
        </div>
      </div>

      <div class="providers-config-panel__actions">
        <div class="providers-config-panel__actions-group">
          <a-button :loading="testingConfig" @click="testCurrentConfig">
            {{ testingConfig ? $t('providers.config_modal.testing_button') : $t('providers.config_modal.test_button') }}
          </a-button>
          <a-button type="primary" :loading="savingConfig" @click="saveCurrentConfig">
            {{ $t('providers.config_modal.save_button') }}
          </a-button>
        </div>
      </div>
    </div>
  </a-modal>
</template>

<script setup lang="ts">
import { reactive, ref } from 'vue';
import { message } from 'ant-design-vue';
import { useI18n } from 'vue-i18n';
import { useRouter } from 'vue-router';

import { logClientError, request } from '../api/client';
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
const activeCardStatusColor = ref<'green' | 'orange' | 'red'>('green');
const activeCardStatusTone = ref<'success' | 'warning' | 'danger'>('success');
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
  } catch (error) {
    logClientError('Failed to load ZeroTier provider configuration.', error);
    message.warning(t('providers.config_modal.load_config_failed'));
  }
}

async function testCurrentConfig() {
  const startedAt = Date.now();
  testingConfig.value = true;
  try {
    const result = await request<ZeroTierTestResult>('/api/providers/zerotier/test-config', {
      method: 'POST',
      body: JSON.stringify(configForm)
    });

    applyTestResultToStatus(result);

    if (result.status === 'Healthy') {
      testResult.value = null;
    } else {
      testResult.value = result;
    }
  } catch (error) {
    logClientError('Failed to test ZeroTier provider configuration.', error);
    const failedResult = {
      status: 'Error',
      message: t('providers.config_modal.test_failed')
    };
    applyTestResultToStatus(failedResult);
    testResult.value = failedResult;
  } finally {
    const elapsed = Date.now() - startedAt;
    const minimumLoadingMs = 900;
    if (elapsed < minimumLoadingMs) {
      await new Promise((resolve) => window.setTimeout(resolve, minimumLoadingMs - elapsed));
    }
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

    saveResultMessage.value = result.restartRequired
      ? t('providers.config_modal.saved_message_restart_required')
      : t('providers.config_modal.saved_message');
  } catch (error) {
    logClientError('Failed to save ZeroTier provider configuration.', error);
    message.error(t('providers.config_modal.save_failed'));
  } finally {
    savingConfig.value = false;
  }
}

function applyTestResultToStatus(result: Pick<ZeroTierTestResult, 'status' | 'message' | 'online' | 'nodeAddress' | 'version'>) {
  activeCardStatusTone.value = getStatusTone(result.status);
  activeCardStatusColor.value = getStatusColor(result.status);
  activeCardStatus.value = localizeStatus(result.status);

  if (typeof result.online === 'boolean' || result.nodeAddress || result.version) {
    const address = result.nodeAddress || t('providers.config_modal.node_address_unknown');
    const version = result.version || t('providers.config_modal.node_version_unknown');
    activeCardMessage.value = t(
      result.online === false
        ? 'providers.config_modal.node_summary_offline'
        : 'providers.config_modal.node_summary_online',
      { address, version }
    );
    return;
  }

  activeCardMessage.value = localizeZeroTierMessage(result.message);
}

function localizeStatus(status: string) {
  switch (status.toLowerCase()) {
    case 'healthy':
      return t('auth.health_status_healthy');
    case 'warning':
      return t('providers.config_modal.status_warning');
    case 'disabled':
      return t('providers.config_modal.status_disabled');
    case 'error':
      return t('providers.config_modal.status_error');
    default:
      return status;
  }
}

function localizeZeroTierMessage(messageText: string) {
  const nodeMatch = /^ZeroTier node (.+) is (online|offline)\. Version: (.+)\.$/i.exec(messageText);
  if (nodeMatch) {
    const [, address, onlineState, version] = nodeMatch;
    return t(
      onlineState.toLowerCase() === 'offline'
        ? 'providers.config_modal.node_summary_offline'
        : 'providers.config_modal.node_summary_online',
      { address, version }
    );
  }

  return messageText;
}

function getStatusTone(status: string) {
  switch (status.toLowerCase()) {
    case 'warning':
      return 'warning';
    case 'error':
      return 'danger';
    default:
      return 'success';
  }
}

function getStatusColor(status: string) {
  switch (status.toLowerCase()) {
    case 'warning':
      return 'orange';
    case 'error':
      return 'red';
    default:
      return 'green';
  }
}

function onCardAction(key: string) {
  if (key === 'zerotier') {
    const card = providerCards.value.find(c => c.key === 'zerotier');
    activeCardStatus.value = card?.status ?? t('auth.health_status_unknown');
    activeCardMessage.value = card?.controlPlane ?? '';
    activeCardStatusColor.value = card?.tagColor === 'red' ? 'red' : card?.tagColor === 'orange' ? 'orange' : 'green';
    activeCardStatusTone.value = card?.tagColor === 'red' ? 'danger' : card?.tagColor === 'orange' ? 'warning' : 'success';
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
