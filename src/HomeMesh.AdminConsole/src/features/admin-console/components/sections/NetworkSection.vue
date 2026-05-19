<template>
  <section :id="sectionId" class="prototype-section">
    <div class="section-heading">
      <div class="heading-badge">2</div>
      <div class="heading-copy">
        <h2>家庭网络</h2>
        <p>成员接入、网络配置、同步状态都收在这个工作区。</p>
      </div>
      <div class="section-tools">
        <a-select
          :value="selectedNetworkId"
          class="network-switcher"
          :options="networkOptions"
          placeholder="选择网络"
          @update:value="emit('update:selected-network-id', $event)"
        />
        <a-button
          type="primary"
          :icon="h(ApiOutlined)"
          :disabled="!selectedNetworkId"
          @click="emit('open-easy-setup')"
        >
          Easy Setup
        </a-button>
        <a-button :icon="h(SyncOutlined)" :disabled="!selectedNetworkId" @click="emit('sync-members')">
          同步成员
        </a-button>
        <a-button
          :icon="h(DeploymentUnitOutlined)"
          :disabled="!selectedNetworkId"
          @click="emit('sync-config')"
        >
          同步设置
        </a-button>
      </div>
    </div>

    <div class="workspace-frame" :class="{ 'workspace-frame--solo': !showRail }">
      <WorkspaceRail
        v-if="showRail"
        :active-key="prototypeSections.network"
        :items="navItems"
        @navigate="emit('navigate', $event)"
      />

      <div class="workspace-body">
        <template v-if="selectedNetwork">
          <div class="network-topbar">
            <div>
              <div class="crumb-line">家庭网络 &gt; {{ selectedNetwork.name }}</div>
              <h3>{{ selectedNetwork.name }}</h3>
            </div>
            <div class="toolbar-note">最近审计 {{ formattedLastAudit }}</div>
          </div>

          <a-tabs :active-key="networkTab" class="network-tabs" @update:activeKey="emit('update:network-tab', $event as NetworkTabKey)">
            <a-tab-pane key="overview" tab="概览" />
            <a-tab-pane key="members" tab="成员" />
            <a-tab-pane key="routes" tab="路由" />
            <a-tab-pane key="dns" tab="DNS" />
            <a-tab-pane key="sync" tab="同步" />
            <a-tab-pane key="gateway" tab="网关" />
            <a-tab-pane key="acl" tab="ACL" />
            <a-tab-pane key="protocol" tab="协议配置" />
          </a-tabs>

          <div class="info-strip">
            <div class="info-item">
              <span>HomeMesh ID</span>
              <strong>{{ selectedNetwork.id }}</strong>
            </div>
            <div class="info-item">
              <span>Provider</span>
              <strong>{{ selectedBinding?.provider ?? '-' }}</strong>
            </div>
            <div class="info-item">
              <span>Provider Network ID</span>
              <strong>{{ selectedBinding?.providerNetworkId ?? '-' }}</strong>
            </div>
            <div class="info-item">
              <span>CIDR</span>
              <strong>{{ selectedNetwork.cidr ?? '自动分配' }}</strong>
            </div>
          </div>

          <div v-if="networkTab === 'overview'" class="network-pane">
            <div class="detail-metrics">
              <article v-for="metric in detailMetrics" :key="metric.label" class="detail-tile">
                <span>{{ metric.label }}</span>
                <strong>{{ metric.value }}</strong>
                <small>{{ metric.meta }}</small>
              </article>
            </div>

            <div class="content-split">
              <article class="panel-card">
                <div class="panel-card__header">
                  <h3>Provider 绑定</h3>
                </div>
                <div class="binding-list">
                  <div
                    v-for="binding in selectedNetwork.providerBindings"
                    :key="binding.providerNetworkId"
                    class="binding-item"
                  >
                    <div>
                      <strong>{{ binding.provider }}</strong>
                      <span>{{ binding.providerNetworkId }}</span>
                    </div>
                    <a-tag :color="binding.isPrimary ? 'blue' : 'default'">
                      {{ binding.isPrimary ? '主绑定' : '附加' }}
                    </a-tag>
                  </div>
                </div>
              </article>

              <article class="panel-card">
                <div class="panel-card__header">
                  <h3>网络策略</h3>
                </div>
                <div class="policy-grid">
                  <div class="policy-item">
                    <span>自动批准成员</span>
                    <strong>{{ selectedNetwork.autoApproveMembers ? '开启' : '关闭' }}</strong>
                  </div>
                  <div class="policy-item">
                    <span>自动分配 IP</span>
                    <strong>{{ selectedNetwork.v4AssignMode ? '开启' : '关闭' }}</strong>
                  </div>
                  <div class="policy-item">
                    <span>网络模式</span>
                    <strong>{{ selectedNetwork.private ? 'Private' : 'Public' }}</strong>
                  </div>
                  <div class="policy-item">
                    <span>状态</span>
                    <strong>{{ selectedNetwork.status }}</strong>
                  </div>
                </div>
              </article>
            </div>
          </div>

          <div v-else-if="networkTab === 'members'" class="network-pane">
            <div v-if="members.length" class="table-wrap">
              <table class="prototype-table prototype-table--members">
                <thead>
                  <tr>
                    <th>设备</th>
                    <th>角色</th>
                    <th>虚拟 IP</th>
                    <th>授权</th>
                    <th>在线</th>
                    <th>标签</th>
                    <th>操作</th>
                  </tr>
                </thead>
                <tbody>
                  <tr v-for="member in members" :key="member.id">
                    <td>
                      <div class="member-cell">
                        <strong>{{ member.name || member.providerMemberId }}</strong>
                        <span>{{ member.providerMemberId }}</span>
                      </div>
                    </td>
                    <td>{{ member.role }}</td>
                    <td class="ip-cell">
                      <a-input
                        :value="memberIpValues[member.id] ?? parseIpAssignments(member.ipAssignmentsJson).join(', ')"
                        size="small"
                        placeholder="10.10.0.20"
                        @update:value="emit('update:member-ip', member.id, $event)"
                        @pressEnter="emit('assign-ip', member)"
                      />
                    </td>
                    <td>
                      <a-tag :color="member.authorized ? 'green' : 'orange'">
                        {{ member.authorized ? '已授权' : '待授权' }}
                      </a-tag>
                    </td>
                    <td>
                      <span class="online-dot" :class="{ offline: !member.online }"></span>
                      {{ member.online ? '在线' : '离线' }}
                    </td>
                    <td>{{ parseTags(member.tagsJson).join(', ') || '-' }}</td>
                    <td>
                      <div class="row-actions">
                        <a-button size="small" type="link" @click="emit('toggle-auth', member)">
                          {{ member.authorized ? '拒绝' : '授权' }}
                        </a-button>
                        <a-button size="small" type="link" @click="emit('assign-ip', member)">
                          保存 IP
                        </a-button>
                      </div>
                    </td>
                  </tr>
                </tbody>
              </table>
            </div>
            <a-empty v-else :image="simpleEmptyImage" description="所选网络还没有成员" />
          </div>

          <div v-else-if="networkTab === 'routes'" class="network-pane">
            <div class="content-split">
              <article class="panel-card">
                <div class="panel-card__header">
                  <h3>路由规则</h3>
                </div>
                <div v-if="routes.length" class="stack-list">
                  <div v-for="route in routes" :key="route.id" class="stack-item">
                    <div>
                      <strong>{{ route.target }}</strong>
                      <span>{{ route.type }} / {{ route.via || 'provider managed' }}</span>
                    </div>
                    <a-button size="small" danger @click="emit('delete-route', route.id)">删除</a-button>
                  </div>
                </div>
                <a-empty v-else :image="simpleEmptyImage" description="暂无路由" />

                <div class="mini-form">
                  <a-input
                    :value="routeForm.target"
                    placeholder="192.168.50.0/24"
                    @update:value="emit('update:route-form', 'target', $event)"
                  />
                  <a-input
                    :value="routeForm.via"
                    placeholder="10.10.0.1"
                    @update:value="emit('update:route-form', 'via', $event)"
                  />
                  <a-button type="primary" @click="emit('create-route')">新增路由</a-button>
                </div>
              </article>

              <article class="panel-card">
                <div class="panel-card__header">
                  <h3>IP 池</h3>
                </div>
                <div v-if="pools.length" class="stack-list">
                  <div v-for="pool in pools" :key="pool.id" class="stack-item">
                    <div>
                      <strong>{{ pool.ipRangeStart }} - {{ pool.ipRangeEnd }}</strong>
                      <span>{{ pool.providerManaged ? 'Provider 管理' : '本地管理' }}</span>
                    </div>
                    <a-button size="small" danger @click="emit('delete-pool', pool.id)">删除</a-button>
                  </div>
                </div>
                <a-empty v-else :image="simpleEmptyImage" description="暂无 IP 池" />

                <div class="mini-form mini-form--triple">
                  <a-input
                    :value="poolForm.ipRangeStart"
                    placeholder="10.10.0.10"
                    @update:value="emit('update:pool-form', 'ipRangeStart', $event)"
                  />
                  <a-input
                    :value="poolForm.ipRangeEnd"
                    placeholder="10.10.0.200"
                    @update:value="emit('update:pool-form', 'ipRangeEnd', $event)"
                  />
                  <a-button type="primary" @click="emit('create-pool')">新增 IP 池</a-button>
                </div>
              </article>
            </div>
          </div>

          <div v-else-if="networkTab === 'dns'" class="network-pane">
            <div class="content-split">
              <article class="panel-card">
                <div class="panel-card__header">
                  <h3>DNS 配置</h3>
                </div>
                <div class="form-grid">
                  <a-input
                    :value="dnsForm.domain"
                    placeholder="home.arpa"
                    @update:value="emit('update:dns-form', 'domain', $event)"
                  />
                  <a-input
                    :value="dnsForm.servers"
                    placeholder="1.1.1.1, 8.8.8.8"
                    @update:value="emit('update:dns-form', 'servers', $event)"
                  />
                  <a-checkbox
                    :checked="dnsForm.providerManaged"
                    @update:checked="emit('update:dns-form', 'providerManaged', $event)"
                  >
                    Provider 管理
                  </a-checkbox>
                  <a-button type="primary" @click="emit('save-dns')">保存 DNS</a-button>
                </div>
              </article>

              <article class="panel-card">
                <div class="panel-card__header">
                  <h3>当前生效值</h3>
                </div>
                <div class="definition-list">
                  <div>
                    <span>域名</span>
                    <strong>{{ dnsConfig?.domain || '未设置' }}</strong>
                  </div>
                  <div>
                    <span>服务器</span>
                    <strong>{{ dnsConfig?.servers.join(', ') || '未设置' }}</strong>
                  </div>
                  <div>
                    <span>来源</span>
                    <strong>{{ dnsConfig?.providerManaged ? 'Provider' : 'Local' }}</strong>
                  </div>
                </div>
              </article>
            </div>
          </div>

          <div v-else-if="networkTab === 'sync'" class="network-pane">
            <div class="sync-grid">
              <article v-for="card in syncCards" :key="card.key" class="sync-card">
                <div class="sync-card__header">
                  <strong>{{ card.title }}</strong>
                  <a-tag :color="statusColor(card.status)">{{ card.status }}</a-tag>
                </div>
                <p>{{ card.message }}</p>
                <small>{{ card.time }}</small>
              </article>
            </div>
          </div>

          <div v-else class="network-pane">
            <article class="panel-card panel-card--placeholder">
              <div class="panel-card__header">
                <h3>{{ placeholderTitles[networkTab as PlaceholderTabKey] }}</h3>
              </div>
              <p>这一栏先把版式占位对齐到原型，后续再接实际网关、ACL 和协议策略接口。</p>
            </article>
          </div>
        </template>

        <a-empty
          v-else
          :image="simpleEmptyImage"
          description="先创建一个网络，或者从总览中选中已有网络"
        />
      </div>
    </div>
  </section>
</template>

<script setup lang="ts">
import { h } from 'vue';
import { ApiOutlined, DeploymentUnitOutlined, SyncOutlined } from '@ant-design/icons-vue';
import { Empty } from 'ant-design-vue';

import type { DetailMetric, NetworkTabKey, PlaceholderTabKey, PrototypeSectionKey, SectionNavItem, SyncCardModel } from '../../types';
import { placeholderTitles, prototypeSections } from '../../constants';
import { parseIpAssignments, parseTags, statusColor } from '../../utils';
import type { DnsConfig, IpPoolItem, Member, NetworkDetail, NetworkProviderBinding, RouteItem } from '../../../../api/types';
import WorkspaceRail from '../WorkspaceRail.vue';

const simpleEmptyImage = Empty.PRESENTED_IMAGE_SIMPLE;

defineProps<{
  sectionId: string;
  navItems: SectionNavItem[];
  selectedNetworkId: string;
  networkOptions: Array<{ label: string; value: string }>;
  selectedNetwork: NetworkDetail | null;
  selectedBinding: NetworkProviderBinding | null;
  formattedLastAudit: string;
  networkTab: NetworkTabKey;
  detailMetrics: DetailMetric[];
  members: Member[];
  routes: RouteItem[];
  pools: IpPoolItem[];
  dnsConfig: DnsConfig | null;
  syncCards: SyncCardModel[];
  routeForm: {
    target: string;
    via: string;
  };
  poolForm: {
    ipRangeStart: string;
    ipRangeEnd: string;
  };
  dnsForm: {
    domain: string;
    servers: string;
    providerManaged: boolean;
  };
  memberIpValues: Record<string, string>;
  showRail?: boolean;
}>();

const emit = defineEmits<{
  navigate: [key: PrototypeSectionKey];
  'update:selected-network-id': [value: string];
  'update:network-tab': [value: NetworkTabKey];
  'open-easy-setup': [];
  'sync-members': [];
  'sync-config': [];
  'update:member-ip': [memberId: string, value: string];
  'toggle-auth': [member: Member];
  'assign-ip': [member: Member];
  'update:route-form': [field: 'target' | 'via', value: string];
  'create-route': [];
  'delete-route': [routeId: string];
  'update:pool-form': [field: 'ipRangeStart' | 'ipRangeEnd', value: string];
  'create-pool': [];
  'delete-pool': [poolId: string];
  'update:dns-form': [field: 'domain' | 'servers' | 'providerManaged', value: string | boolean];
  'save-dns': [];
}>();
</script>
