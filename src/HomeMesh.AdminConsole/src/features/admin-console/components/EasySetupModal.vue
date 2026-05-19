<template>
  <a-modal
    :open="open"
    title="Easy Setup"
    ok-text="应用"
    cancel-text="取消"
    :confirm-loading="loading"
    @ok="emit('submit')"
    @cancel="emit('update:open', false)"
  >
    <a-form layout="vertical">
      <a-form-item label="CIDR">
        <a-input :value="form.cidr" placeholder="10.10.0.0/16" @update:value="emit('update:form', 'cidr', $event)" />
      </a-form-item>
      <a-form-item label="IP Pool Start">
        <a-input
          :value="form.ipPoolStart"
          placeholder="10.10.0.10"
          @update:value="emit('update:form', 'ipPoolStart', $event)"
        />
      </a-form-item>
      <a-form-item label="IP Pool End">
        <a-input
          :value="form.ipPoolEnd"
          placeholder="10.10.0.200"
          @update:value="emit('update:form', 'ipPoolEnd', $event)"
        />
      </a-form-item>
      <a-form-item label="DNS Domain">
        <a-input
          :value="form.dnsDomain"
          placeholder="home.arpa"
          @update:value="emit('update:form', 'dnsDomain', $event)"
        />
      </a-form-item>
      <a-form-item label="DNS Servers">
        <a-input
          :value="form.dnsServers"
          placeholder="1.1.1.1, 8.8.8.8"
          @update:value="emit('update:form', 'dnsServers', $event)"
        />
      </a-form-item>
      <a-form-item>
        <a-checkbox
          :checked="form.enableAutoAssign"
          @update:checked="emit('update:form', 'enableAutoAssign', $event)"
        >
          自动分配 IP
        </a-checkbox>
      </a-form-item>
      <a-form-item>
        <a-checkbox
          :checked="form.autoApproveMembers"
          @update:checked="emit('update:form', 'autoApproveMembers', $event)"
        >
          自动批准成员
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
