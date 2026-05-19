<template>
  <a-modal
    :open="open"
    title="创建家庭网络"
    ok-text="创建"
    cancel-text="取消"
    :confirm-loading="loading"
    @ok="emit('submit')"
    @cancel="emit('update:open', false)"
  >
    <a-form layout="vertical">
      <a-form-item label="网络名称">
        <a-input :value="form.name" placeholder="主家庭网络" @update:value="emit('update:form', 'name', $event)" />
      </a-form-item>
      <a-form-item label="Provider">
        <a-select :value="form.provider" :options="providerOptions" @update:value="emit('update:form', 'provider', $event)" />
      </a-form-item>
      <a-form-item label="CIDR">
        <a-input :value="form.cidr" placeholder="10.10.0.0/16" @update:value="emit('update:form', 'cidr', $event)" />
      </a-form-item>
      <a-form-item>
        <a-checkbox :checked="form.private" @update:checked="emit('update:form', 'private', $event)">
          Private 网络
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
</script>
