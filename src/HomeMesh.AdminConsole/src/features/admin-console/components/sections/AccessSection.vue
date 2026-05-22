<template>
  <section :id="sectionId" class="prototype-section">
    <div class="section-heading">
      <div class="heading-badge">3</div>
      <div class="heading-copy">
        <h2>{{ $t('access.title') }}</h2>
        <p>{{ $t('access.description') }}</p>
      </div>
      <div class="section-tools">
        <a-button
          type="primary"
          :icon="h(DownloadOutlined)"
          :disabled="!selectedNetworkId"
          @click="emit('download-plant-file')"
        >
          {{ $t('access.download_planet_button') }}
        </a-button>
        <a-button
          :icon="h(DownloadOutlined)"
          :disabled="!selectedNetworkId"
          @click="emit('download-moon-file')"
        >
          {{ $t('access.download_moon_button') }}
        </a-button>
      </div>
    </div>

    <div class="workspace-frame" :class="{ 'workspace-frame--solo': !showRail }">
      <WorkspaceRail
        v-if="showRail"
        :active-key="prototypeSections.access"
        :items="navItems"
        @navigate="emit('navigate', $event)"
      />

      <div class="workspace-body">
        <div class="content-split content-split--tight">
          <article class="panel-card panel-card--invite">
            <div class="panel-card__header">
              <h3>{{ $t('access.invite_section') }}</h3>
              <span>{{ $t('access.invite_meta') }}</span>
            </div>

            <div class="form-grid">
              <a-select
                :value="selectedNetworkId"
                :options="networkOptions"
                :placeholder="$t('access.network_placeholder')"
                @update:value="onNetworkChange"
              />
              <a-select
                :value="accessForm.expiryDays"
                :options="expiryOptions"
                @update:value="onExpiryChange"
              />
              <a-input
                :value="accessForm.label"
                :placeholder="$t('access.device_placeholder')"
                @update:value="emit('update:access-form', 'label', $event)"
              />
              <a-checkbox
                :checked="accessForm.autoApprove"
                @update:checked="emit('update:access-form', 'autoApprove', $event)"
              >
                {{ $t('access.auto_approve') }}
              </a-checkbox>
              <div class="inline-actions">
                <a-button type="primary" @click="emit('generate-access-artifact')">
                  {{ $t('access.generate_code') }}
                </a-button>
                <a-button :disabled="!selectedNetworkId" @click="emit('download-plant-file')">
                  {{ $t('access.download_planet_button') }}
                </a-button>
                <a-button :disabled="!selectedNetworkId" @click="emit('download-moon-file')">
                  {{ $t('access.download_moon_button') }}
                </a-button>
              </div>
            </div>
          </article>

          <article class="panel-card panel-card--qr">
            <div class="panel-card__header">
              <h3>{{ $t('access.qr_section') }}</h3>
              <span>
                {{ $t('access.qr_meta') }}
                <template v-if="artifactExpiresAt">
                  · {{ $t('access.expires_at') }} {{ artifactExpiresAt }}
                </template>
              </span>
            </div>

            <div class="qr-shell">
              <div>
                <strong>{{ $t('access.download_planet_button') }}</strong>
                <div class="qr-matrix" style="margin-top: 8px">
                  <div
                    v-for="(cell, index) in planetQrCells"
                    :key="`planet-${index}`"
                    class="qr-cell"
                    :class="{ active: cell }"
                  ></div>
                </div>
                <div class="access-code-card" style="margin-top: 10px">
                  <code>{{ planetUrl || '-' }}</code>
                  <a-button size="small" :icon="h(CopyOutlined)" :disabled="!planetUrl" @click="emit('copy-url', planetUrl)" />
                </div>
              </div>

              <div style="margin-top: 18px">
                <strong>{{ $t('access.download_moon_button') }}</strong>
                <div class="qr-matrix" style="margin-top: 8px">
                  <div
                    v-for="(cell, index) in moonQrCells"
                    :key="`moon-${index}`"
                    class="qr-cell"
                    :class="{ active: cell }"
                  ></div>
                </div>
                <div class="access-code-card" style="margin-top: 10px">
                  <code>{{ moonUrl || '-' }}</code>
                  <a-button size="small" :icon="h(CopyOutlined)" :disabled="!moonUrl" @click="emit('copy-url', moonUrl)" />
                </div>
              </div>

              <div class="qr-copy" style="margin-top: 18px">
                <strong>{{ accessNetworkName }}</strong>
                <div class="access-code-card">
                  <code>{{ accessCode }}</code>
                  <a-button size="small" :icon="h(CopyOutlined)" @click="emit('copy-access-code')" />
                </div>
                <p>{{ $t('access.qr_platforms') }}</p>
              </div>
            </div>
          </article>
        </div>
      </div>
    </div>
  </section>
</template>

<script setup lang="ts">
import { h } from 'vue';
import { CopyOutlined, DownloadOutlined } from '@ant-design/icons-vue';

import type { PrototypeSectionKey, SectionNavItem } from '../../types';
import { prototypeSections } from '../../constants';
import WorkspaceRail from '../WorkspaceRail.vue';

defineProps<{
  sectionId: string;
  navItems: SectionNavItem[];
  selectedNetworkId: string;
  networkOptions: Array<{ label: string; value: string }>;
  expiryOptions: Array<{ label: string; value: string }>;
  accessForm: {
    expiryDays: string;
    label: string;
    autoApprove: boolean;
  };
  artifactExpiresAt: string | null;
  accessNetworkName: string;
  accessCode: string;
  planetUrl: string;
  moonUrl: string;
  planetQrCells: boolean[];
  moonQrCells: boolean[];
  showRail?: boolean;
}>();

const emit = defineEmits<{
  navigate: [key: PrototypeSectionKey];
  'update:selected-network-id': [value: string];
  'update:access-form': [field: 'expiryDays' | 'label' | 'autoApprove', value: string | boolean];
  'generate-access-artifact': [];
  'download-plant-file': [];
  'download-moon-file': [];
  'copy-url': [value: string];
  'copy-access-code': [];
}>();

function onNetworkChange(value: unknown) {
  emit('update:selected-network-id', normalizeSelectValue(value));
}

function onExpiryChange(value: unknown) {
  emit('update:access-form', 'expiryDays', normalizeSelectValue(value));
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
