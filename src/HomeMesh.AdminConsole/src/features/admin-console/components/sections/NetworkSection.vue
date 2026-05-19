<template>
  <section :id="sectionId" class="prototype-section">
    <div class="section-heading">
      <div class="heading-badge">2</div>
      <div class="heading-copy">
        <h2>Networks</h2>
        <p>Manage member access, network settings, and sync activity from one place.</p>
      </div>
      <div class="section-tools">
        <a-select
          :value="selectedNetworkId"
          class="network-switcher"
          :options="networkOptions"
          placeholder="Select network"
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
          Sync Members
        </a-button>
        <a-button
          :icon="h(DeploymentUnitOutlined)"
          :disabled="!selectedNetworkId"
          @click="emit('sync-config')"
        >
          Sync Config
        </a-button>
        <a-popconfirm
          title="Delete this network?"
          description="This removes the HomeMesh network, provider binding, and related config. This cannot be undone."
          ok-text="Delete"
          cancel-text="Cancel"
          :disabled="!selectedNetworkId"
          @confirm="emit('delete-network')"
        >
          <a-button :disabled="!selectedNetworkId" :loading="deleteLoading" danger>
            Delete Network
          </a-button>
        </a-popconfirm>
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
              <div class="crumb-line">Networks &gt; {{ selectedNetwork.name }}</div>
              <h3>{{ selectedNetwork.name }}</h3>
            </div>
            <div class="toolbar-note">Last audit: {{ formattedLastAudit }}</div>
          </div>

          <a-tabs
            :active-key="networkTab"
            class="network-tabs"
            @update:activeKey="emit('update:network-tab', $event as NetworkTabKey)"
          >
            <a-tab-pane key="overview" tab="Overview" />
            <a-tab-pane key="members" tab="Members" />
            <a-tab-pane key="routes" tab="Routes" />
            <a-tab-pane key="dns" tab="DNS" />
            <a-tab-pane key="sync" tab="Sync" />
            <a-tab-pane key="gateway" tab="Gateway" />
            <a-tab-pane key="acl" tab="ACL" />
            <a-tab-pane key="protocol" tab="Protocol" />
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
              <strong>{{ selectedNetwork.cidr ?? 'Auto assigned' }}</strong>
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
                  <h3>Provider Bindings</h3>
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
                      {{ binding.isPrimary ? 'Primary' : 'Secondary' }}
                    </a-tag>
                  </div>
                </div>
              </article>

              <article class="panel-card">
                <div class="panel-card__header">
                  <h3>Policy</h3>
                </div>
                <div class="policy-grid">
                  <div class="policy-item">
                    <span>Auto approve members</span>
                    <strong>{{ selectedNetwork.autoApproveMembers ? 'Enabled' : 'Disabled' }}</strong>
                  </div>
                  <div class="policy-item">
                    <span>Auto assign IP</span>
                    <strong>{{ selectedNetwork.v4AssignMode ? 'Enabled' : 'Disabled' }}</strong>
                  </div>
                  <div class="policy-item">
                    <span>Network mode</span>
                    <strong>{{ selectedNetwork.private ? 'Private' : 'Public' }}</strong>
                  </div>
                  <div class="policy-item">
                    <span>Status</span>
                    <strong>{{ selectedNetwork.status }}</strong>
                  </div>
                </div>
              </article>
            </div>
          </div>

          <div v-else-if="networkTab === 'members'" class="network-pane">
            <article class="panel-card panel-card--table">
              <div class="panel-card__header panel-card__header--table">
                <div>
                  <h3>Members</h3>
                  <span>{{ members.length ? `${members.length} devices` : 'Waiting for members to join' }}</span>
                </div>
                <div class="inline-actions">
                  <a-button size="small" @click="emit('sync-members')">Sync Members</a-button>
                  <a-button size="small" type="primary" ghost @click="emit('navigate', prototypeSections.access)">
                    Device Access
                  </a-button>
                </div>
              </div>

              <div class="table-wrap">
                <table class="prototype-table prototype-table--members">
                  <thead>
                    <tr>
                      <th>Device</th>
                      <th>Role</th>
                      <th>Virtual IP</th>
                      <th>Auth</th>
                      <th>Online</th>
                      <th>Tags</th>
                      <th>Actions</th>
                    </tr>
                  </thead>
                  <tbody v-if="members.length">
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
                          {{ member.authorized ? 'Authorized' : 'Pending' }}
                        </a-tag>
                      </td>
                      <td>
                        <span class="online-dot" :class="{ offline: !member.online }"></span>
                        {{ member.online ? 'Online' : 'Offline' }}
                      </td>
                      <td>{{ parseTags(member.tagsJson).join(', ') || '-' }}</td>
                      <td>
                        <div class="row-actions">
                          <a-button size="small" type="link" @click="emit('toggle-auth', member)">
                            {{ member.authorized ? 'Revoke' : 'Authorize' }}
                          </a-button>
                          <a-button size="small" type="link" @click="emit('assign-ip', member)">
                            Save IP
                          </a-button>
                        </div>
                      </td>
                    </tr>
                  </tbody>
                  <tbody v-else>
                    <tr>
                      <td colspan="7" class="table-empty-cell">
                        <div class="table-empty-state">
                          <a-empty :image="simpleEmptyImage" description="No member devices yet" />
                        </div>
                      </td>
                    </tr>
                  </tbody>
                </table>
              </div>
            </article>
          </div>

          <div v-else-if="networkTab === 'routes'" class="network-pane">
            <div class="content-split">
              <article class="panel-card">
                <div class="panel-card__header">
                  <h3>Routes</h3>
                </div>
                <div v-if="routes.length" class="stack-list">
                  <div v-for="route in routes" :key="route.id" class="stack-item">
                    <div>
                      <strong>{{ route.target }}</strong>
                      <span>{{ route.type }} / {{ route.via || 'provider managed' }}</span>
                    </div>
                    <a-button size="small" danger @click="emit('delete-route', route.id)">Delete</a-button>
                  </div>
                </div>
                <a-empty v-else :image="simpleEmptyImage" description="No routes" />

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
                  <a-button type="primary" @click="emit('create-route')">Add Route</a-button>
                </div>
              </article>

              <article class="panel-card">
                <div class="panel-card__header">
                  <h3>IP Pools</h3>
                </div>
                <div v-if="pools.length" class="stack-list">
                  <div v-for="pool in pools" :key="pool.id" class="stack-item">
                    <div>
                      <strong>{{ pool.ipRangeStart }} - {{ pool.ipRangeEnd }}</strong>
                      <span>{{ pool.providerManaged ? 'Provider managed' : 'Local managed' }}</span>
                    </div>
                    <a-button size="small" danger @click="emit('delete-pool', pool.id)">Delete</a-button>
                  </div>
                </div>
                <a-empty v-else :image="simpleEmptyImage" description="No IP pools" />

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
                  <a-button type="primary" @click="emit('create-pool')">Add IP Pool</a-button>
                </div>
              </article>
            </div>
          </div>

          <div v-else-if="networkTab === 'dns'" class="network-pane">
            <div class="content-split">
              <article class="panel-card">
                <div class="panel-card__header">
                  <h3>DNS Config</h3>
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
                    Provider managed
                  </a-checkbox>
                  <a-button type="primary" @click="emit('save-dns')">Save DNS</a-button>
                </div>
              </article>

              <article class="panel-card">
                <div class="panel-card__header">
                  <h3>Current Values</h3>
                </div>
                <div class="definition-list">
                  <div>
                    <span>Domain</span>
                    <strong>{{ dnsConfig?.domain || 'Not set' }}</strong>
                  </div>
                  <div>
                    <span>Servers</span>
                    <strong>{{ dnsConfig?.servers.join(', ') || 'Not set' }}</strong>
                  </div>
                  <div>
                    <span>Source</span>
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
              <p>This area is reserved for future gateway, ACL, and protocol controls.</p>
            </article>
          </div>
        </template>

        <a-empty
          v-else
          :image="simpleEmptyImage"
          description="Create a network first, or select one from the dashboard"
        />
      </div>
    </div>
  </section>
</template>

<script setup lang="ts">
import { h } from 'vue';
import { ApiOutlined, DeploymentUnitOutlined, SyncOutlined } from '@ant-design/icons-vue';
import { Empty } from 'ant-design-vue';

import type {
  DetailMetric,
  NetworkTabKey,
  PlaceholderTabKey,
  PrototypeSectionKey,
  SectionNavItem,
  SyncCardModel
} from '../../types';
import { placeholderTitles, prototypeSections } from '../../constants';
import { parseIpAssignments, parseTags, statusColor } from '../../utils';
import type {
  DnsConfig,
  IpPoolItem,
  Member,
  NetworkDetail,
  NetworkProviderBinding,
  RouteItem
} from '../../../../api/types';
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
  deleteLoading: boolean;
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
  'delete-network': [];
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
