<template>
  <section :id="sectionId" class="prototype-section">
    <div class="section-heading">
      <div class="heading-badge">3</div>
      <div class="heading-copy">
        <h2>设备接入</h2>
        <p>生成邀请码、下载接入文件，并为终端准备扫码入口。</p>
      </div>
      <div class="section-tools">
        <a-button
          type="primary"
          :icon="h(DownloadOutlined)"
          :disabled="!selectedNetworkId"
          @click="emit('download-plant-file')"
        >
          下载接入文件
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
              <h3>生成设备邀请码</h3>
              <span>为 Windows、Android、Linux 终端准备接入凭证</span>
            </div>

            <div class="form-grid">
              <a-select
                :value="selectedNetworkId"
                :options="networkOptions"
                placeholder="加入网络"
                @update:value="emit('update:selected-network-id', $event)"
              />
              <a-select
                :value="accessForm.expiryDays"
                :options="expiryOptions"
                @update:value="emit('update:access-form', 'expiryDays', $event)"
              />
              <a-input
                :value="accessForm.label"
                placeholder="例如：手机、工作站、IoT"
                @update:value="emit('update:access-form', 'label', $event)"
              />
              <a-checkbox
                :checked="accessForm.autoApprove"
                @update:checked="emit('update:access-form', 'autoApprove', $event)"
              >
                自动授权
              </a-checkbox>
              <div class="inline-actions">
                <a-button type="primary" @click="emit('generate-access-artifact')">
                  生成邀请码
                </a-button>
                <a-button :disabled="!selectedNetworkId" @click="emit('download-plant-file')">
                  下载接入文件
                </a-button>
              </div>
            </div>
          </article>

          <article class="panel-card panel-card--qr">
            <div class="panel-card__header">
              <h3>扫码接入</h3>
              <span>邀请码与二维码同步刷新</span>
            </div>

            <div class="qr-shell">
              <div class="qr-matrix">
                <div
                  v-for="(cell, index) in qrCells"
                  :key="index"
                  class="qr-cell"
                  :class="{ active: cell }"
                ></div>
              </div>

              <div class="qr-copy">
                <strong>{{ accessNetworkName }}</strong>
                <div class="access-code-card">
                  <code>{{ accessCode }}</code>
                  <a-button size="small" :icon="h(CopyOutlined)" @click="emit('copy-access-code')" />
                </div>
                <p>支持 Windows / Android / Linux 客户端</p>
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
  accessNetworkName: string;
  accessCode: string;
  qrCells: boolean[];
  showRail?: boolean;
}>();

const emit = defineEmits<{
  navigate: [key: PrototypeSectionKey];
  'update:selected-network-id': [value: string];
  'update:access-form': [field: 'expiryDays' | 'label' | 'autoApprove', value: string | boolean];
  'generate-access-artifact': [];
  'download-plant-file': [];
  'copy-access-code': [];
}>();
</script>
