<template>
  <div class="qr-matrix qr-matrix--compact qr-matrix--image">
    <img v-if="imageUrl" class="qr-image" :src="imageUrl" :alt="alt" />
    <div v-else class="qr-placeholder">{{ emptyText }}</div>
  </div>
</template>

<script setup lang="ts">
import { ref, watch } from 'vue';
import QRCode from 'qrcode';

const props = defineProps<{
  value: string;
  alt: string;
  emptyText: string;
}>();

const imageUrl = ref('');

watch(
  () => props.value,
  async (value) => {
    const normalizedValue = value.trim();

    if (!normalizedValue) {
      imageUrl.value = '';
      return;
    }

    imageUrl.value = await QRCode.toDataURL(normalizedValue, {
      errorCorrectionLevel: 'M',
      margin: 1,
      width: 168
    });
  },
  { immediate: true }
);
</script>
