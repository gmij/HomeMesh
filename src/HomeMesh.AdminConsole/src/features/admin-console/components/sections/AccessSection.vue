<template>
  <section :id="sectionId" class="prototype-section">
    <div class="section-heading">
      <div class="heading-badge">3</div>
      <div class="heading-copy">
        <h2>{{ $t('access.title') }}</h2>
        <p>{{ $t('access.description') }}</p>
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
            </div>
          </article>

          <article class="panel-card panel-card--qr">
            <div class="panel-card__header">
              <h3>{{ $t('access.qr_section') }}</h3>
              <div class="panel-card__meta panel-card__meta--right">
                <span>{{ $t('access.qr_meta') }}</span>
                <span v-if="artifactExpiresAt" class="panel-card__expiry">
                  {{ $t('access.expires_at') }} {{ artifactExpiresAt }}
                </span>
              </div>
            </div>

            <div class="qr-shell">
              <div class="qr-artifact-grid">
                <div class="qr-artifact-card">
                  <div class="qr-artifact-card__header">
                    <strong>planet</strong>
                    <div class="qr-artifact-actions">
                      <a-dropdown-button
                        size="small"
                        :icon="h(DownOutlined)"
                        :disabled="!selectedNetworkId"
                        @click="emit('download-plant-file')"
                      >
                        <DownloadOutlined />
                        {{ $t('access.download_planet_button') }}
                        <template #overlay>
                          <a-menu>
                            <a-menu-item
                              key="copy-planet-url"
                              :disabled="!planetUrl"
                              @click="emit('copy-url', planetUrl)"
                            >
                              <CopyOutlined />
                              <span>{{ $t('access.copy_url_button') }}</span>
                            </a-menu-item>
                          </a-menu>
                        </template>
                      </a-dropdown-button>
                    </div>
                  </div>

                  <QrCodeImage
                    :value="planetUrl"
                    :alt="$t('access.download_planet_button')"
                    :empty-text="$t('access.qr_link_hint')"
                  />

                  <p class="qr-link-hint">{{ $t('access.qr_link_hint') }}</p>
                </div>

                <div class="qr-artifact-card">
                  <div class="qr-artifact-card__header">
                    <strong>moon</strong>
                    <div class="qr-artifact-actions">
                      <a-dropdown-button
                        size="small"
                        :icon="h(DownOutlined)"
                        :disabled="!selectedNetworkId"
                        @click="emit('download-moon-file')"
                      >
                        <DownloadOutlined />
                        {{ $t('access.download_moon_button') }}
                        <template #overlay>
                          <a-menu>
                            <a-menu-item
                              key="copy-moon-url"
                              :disabled="!moonUrl"
                              @click="emit('copy-url', moonUrl)"
                            >
                              <CopyOutlined />
                              <span>{{ $t('access.copy_url_button') }}</span>
                            </a-menu-item>
                          </a-menu>
                        </template>
                      </a-dropdown-button>
                    </div>
                  </div>

                  <QrCodeImage
                    :value="moonUrl"
                    :alt="$t('access.download_moon_button')"
                    :empty-text="$t('access.qr_link_hint')"
                  />

                  <p class="qr-link-hint">{{ $t('access.qr_link_hint') }}</p>
                </div>
              </div>

              <div class="qr-copy">
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
import { CopyOutlined, DownloadOutlined, DownOutlined } from '@ant-design/icons-vue';

import { prototypeSections } from '../../constants';
import type { PrototypeSectionKey, SectionNavItem } from '../../types';
import QrCodeImage from '../QrCodeImage.vue';
import WorkspaceRail from '../WorkspaceRail.vue';

defineProps<{
  sectionId: string;
  navItems: SectionNavItem[];
  selectedNetworkId: string;
  networkOptions: Array<{ label: string; value: string }>;
  expiryOptions: Array<{ label: string; value: string }>;
  accessForm: {
    expiryDays: string;
  };
  artifactExpiresAt: string | null;
  planetUrl: string;
  moonUrl: string;
  showRail?: boolean;
}>();

const emit = defineEmits<{
  navigate: [key: PrototypeSectionKey];
  'update:selected-network-id': [value: string];
  'update:access-form': [field: 'expiryDays', value: string | boolean];
  'download-plant-file': [];
  'download-moon-file': [];
  'copy-url': [value: string];
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
