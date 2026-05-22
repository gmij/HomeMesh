<template>
  <a-modal
    :open="open"
    :title="$t('modals.create_network.title')"
    :ok-text="$t('modals.create_network.ok_text')"
    :cancel-text="$t('modals.create_network.cancel_text')"
    :confirm-loading="loading"
    @ok="emit('submit')"
    @cancel="emit('update:open', false)"
  >
    <a-form layout="vertical">
      <a-form-item :label="$t('modals.create_network.name_label')">
        <a-input :value="form.name" :placeholder="$t('modals.create_network.name_placeholder')" @update:value="emit('update:form', 'name', $event)" />
      </a-form-item>
      <a-form-item :label="$t('modals.create_network.provider_label')">
        <a-select :value="form.provider" :options="providerOptions" @update:value="onProviderChange" />
      </a-form-item>
      <a-form-item :label="$t('modals.create_network.cidr_label')">
        <a-select
          :value="cidrMode"
          :options="cidrOptions"
          :placeholder="$t('modals.create_network.cidr_mode_placeholder')"
          @update:value="onCidrModeChange"
        />
        <a-input
          v-if="cidrMode === customCidrValue"
          :value="customCidr"
          style="margin-top: 8px"
          :placeholder="$t('modals.create_network.cidr_placeholder')"
          @update:value="onCustomCidrChange"
        />
        <div v-if="cidrMode === customCidrValue" style="margin-top: 6px; color: #64748b; font-size: 12px">
          {{ $t('modals.create_network.cidr_custom_hint') }}
        </div>
      </a-form-item>
      <a-form-item>
        <a-checkbox :checked="form.private" @update:checked="emit('update:form', 'private', $event)">
          {{ $t('modals.create_network.private_label') }}
        </a-checkbox>
      </a-form-item>
      <a-form-item>
        <a-checkbox
          :checked="form.autoApproveMembers"
          @update:checked="emit('update:form', 'autoApproveMembers', $event)"
        >
          {{ $t('modals.create_network.auto_approve_label') }}
        </a-checkbox>
      </a-form-item>
    </a-form>
  </a-modal>
</template>

<script setup lang="ts">
import { computed, ref, watch } from 'vue';
import { useI18n } from 'vue-i18n';

const props = defineProps<{
  open: boolean;
  loading: boolean;
  providerOptions: Array<{ label: string; value: string }>;
  form: {
    name: string;
    provider: string;
    cidr: string;
    private: boolean;
    autoApproveMembers: boolean;
  };
}>();

const emit = defineEmits<{
  'update:open': [value: boolean];
  'update:form': [
    field: 'name' | 'provider' | 'cidr' | 'private' | 'autoApproveMembers',
    value: string | boolean
  ];
  submit: [];
}>();

const { t } = useI18n();

const customCidrValue = '__custom__';
const presetCidrs = ['10.10.0.0/24', '10.20.0.0/24', '192.168.1.0/24', '192.168.100.0/24'];
const cidrMode = ref<string>(presetCidrs[0]);
const customCidr = ref('');

const cidrOptions = computed(() => [
  { value: '10.10.0.0/24', label: `${t('modals.create_network.cidr_option_home_default')} (10.10.0.0/24)` },
  { value: '10.20.0.0/24', label: `${t('modals.create_network.cidr_option_home_alt')} (10.20.0.0/24)` },
  { value: '192.168.1.0/24', label: `${t('modals.create_network.cidr_option_lab')} (192.168.1.0/24)` },
  { value: '192.168.100.0/24', label: `${t('modals.create_network.cidr_option_small_lan')} (192.168.100.0/24)` },
  { value: customCidrValue, label: t('modals.create_network.cidr_custom_option') }
]);

watch(
  () => props.form.cidr,
  (cidr) => {
    const value = (cidr ?? '').trim();
    if (!value) {
      cidrMode.value = presetCidrs[0];
      customCidr.value = '';
      return;
    }

    if (presetCidrs.includes(value)) {
      cidrMode.value = value;
      customCidr.value = '';
      return;
    }

    cidrMode.value = customCidrValue;
    customCidr.value = value;
  },
  { immediate: true }
);

function onProviderChange(value: unknown) {
  emit('update:form', 'provider', normalizeSelectValue(value));
}

function onCidrModeChange(value: unknown) {
  const normalized = normalizeSelectValue(value);
  if (!normalized) {
    return;
  }

  cidrMode.value = normalized;

  if (normalized === customCidrValue) {
    emit('update:form', 'cidr', customCidr.value.trim());
    return;
  }

  customCidr.value = '';
  emit('update:form', 'cidr', normalized);
}

function onCustomCidrChange(value: unknown) {
  const normalized = normalizeSelectValue(value);
  customCidr.value = normalized;
  emit('update:form', 'cidr', normalized.trim());
}

function normalizeSelectValue(value: unknown) {
  if (typeof value === 'string') {
    return value;
  }

  if (typeof value === 'number') {
    return String(value);
  }

  return '';
}
</script>
