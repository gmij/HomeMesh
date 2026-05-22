<template>
  <a-modal
    :open="open"
    :title="$t('modals.easy_setup.title')"
    :ok-text="$t('modals.easy_setup.ok_text')"
    :cancel-text="$t('modals.easy_setup.cancel_text')"
    :confirm-loading="loading"
    @ok="emit('submit')"
    @cancel="emit('update:open', false)"
  >
    <a-form layout="vertical">
      <a-form-item :label="$t('modals.easy_setup.cidr_label')">
        <a-input
          :value="form.cidr"
          :placeholder="$t('modals.easy_setup.cidr_placeholder')"
          @update:value="emit('update:form', 'cidr', $event)"
        />
      </a-form-item>
      <a-form-item :label="$t('modals.easy_setup.pool_start_label')">
        <a-input
          :value="form.ipPoolStart"
          :placeholder="$t('modals.easy_setup.pool_start_placeholder')"
          @update:value="emit('update:form', 'ipPoolStart', $event)"
        />
      </a-form-item>
      <a-form-item :label="$t('modals.easy_setup.pool_end_label')">
        <a-input
          :value="form.ipPoolEnd"
          :placeholder="$t('modals.easy_setup.pool_end_placeholder')"
          @update:value="emit('update:form', 'ipPoolEnd', $event)"
        />
      </a-form-item>
      <a-form-item :label="$t('modals.easy_setup.dns_domain_label')">
        <a-input
          :value="form.dnsDomain"
          :placeholder="$t('modals.easy_setup.dns_domain_placeholder')"
          @update:value="emit('update:form', 'dnsDomain', $event)"
        />
      </a-form-item>
      <a-form-item :label="$t('modals.easy_setup.dns_servers_label')">
        <a-input
          :value="form.dnsServers"
          :placeholder="$t('modals.easy_setup.dns_servers_placeholder')"
          @update:value="emit('update:form', 'dnsServers', $event)"
        />
      </a-form-item>
      <a-form-item>
        <a-checkbox
          :checked="form.enableAutoAssign"
          @update:checked="emit('update:form', 'enableAutoAssign', $event)"
        >
          {{ $t('modals.easy_setup.auto_assign_label') }}
        </a-checkbox>
      </a-form-item>
      <a-form-item>
        <a-checkbox
          :checked="form.autoApproveMembers"
          @update:checked="emit('update:form', 'autoApproveMembers', $event)"
        >
          {{ $t('modals.easy_setup.auto_approve_label') }}
        </a-checkbox>
      </a-form-item>
    </a-form>
  </a-modal>
</template>

<script setup lang="ts">
defineProps<{
  open: boolean;
  loading: boolean;
  form: {
    cidr: string;
    ipPoolStart: string;
    ipPoolEnd: string;
    enableAutoAssign: boolean;
    autoApproveMembers: boolean;
    dnsDomain: string;
    dnsServers: string;
  };
}>();

const emit = defineEmits<{
  'update:open': [value: boolean];
  'update:form': [
    field:
      | 'cidr'
      | 'ipPoolStart'
      | 'ipPoolEnd'
      | 'enableAutoAssign'
      | 'autoApproveMembers'
      | 'dnsDomain'
      | 'dnsServers',
    value: string | boolean
  ];
  submit: [];
}>();
</script>
