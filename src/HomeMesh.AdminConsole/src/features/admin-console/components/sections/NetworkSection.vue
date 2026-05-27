<template>
  <section :id="sectionId" class="prototype-section">
    <div class="section-heading">
      <div class="heading-badge">2</div>
      <div class="heading-copy">
        <h2>{{ $t('network.title') }}</h2>
        <p>{{ $t('network.description') }}</p>
      </div>
      <div class="section-tools">
        <a-select
          :value="selectedNetworkId"
          class="network-switcher"
          :options="networkOptions"
          :placeholder="$t('network.select_placeholder')"
          @update:value="onNetworkChange"
        />
        <a-button
          type="primary"
          :icon="h(ApiOutlined)"
          :disabled="!selectedNetworkId"
          @click="emit('open-easy-setup')"
        >
          {{ $t('network.actions.easy_setup') }}
        </a-button>
        <a-button :icon="h(ReloadOutlined)" :disabled="!selectedNetworkId" @click="emit('sync-members')">
          {{ $t('network.actions.sync_members') }}
        </a-button>
        <a-button
          :icon="h(DeploymentUnitOutlined)"
          :disabled="!selectedNetworkId"
          @click="emit('sync-config')"
        >
          {{ $t('network.actions.sync_config') }}
        </a-button>
        <a-popconfirm
          :title="$t('network.actions.confirm_delete')"
          :description="$t('network.actions.confirm_delete_desc')"
          :ok-text="$t('network.actions.confirm_ok')"
          :cancel-text="$t('network.actions.confirm_cancel')"
          :disabled="!selectedNetworkId"
          @confirm="emit('delete-network')"
        >
          <a-button :disabled="!selectedNetworkId" :loading="deleteLoading" danger>
            {{ $t('network.actions.delete') }}
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
              <div class="crumb-line">{{ $t('network.breadcrumb') }} &gt; {{ selectedNetwork.name }}</div>
              <h3>{{ selectedNetwork.name }}</h3>
            </div>
            <div class="toolbar-note">{{ $t('network.last_audit', { time: formattedLastAudit }) }}</div>
          </div>

          <a-tabs
            :active-key="networkTab"
            class="network-tabs"
            @update:activeKey="emit('update:network-tab', $event as NetworkTabKey)"
          >
            <a-tab-pane key="overview" :tab="$t('network.tabs.overview')" />
            <a-tab-pane key="members" :tab="$t('network.tabs.members')" />
            <a-tab-pane key="routes" :tab="$t('network.tabs.routes')" />
            <a-tab-pane key="dns" :tab="$t('network.tabs.dns')" />
            <a-tab-pane key="gateway" :tab="$t('network.tabs.gateway')" />
            <a-tab-pane key="acl" :tab="$t('network.tabs.acl')" />
            <a-tab-pane key="protocol" :tab="$t('network.tabs.protocol')" />
          </a-tabs>

          <div class="info-strip">
            <div class="info-item">
              <span>{{ $t('network.info_labels.homemesh_id') }}</span>
              <strong>{{ selectedNetwork.id }}</strong>
            </div>
            <div class="info-item">
              <span>{{ $t('network.info_labels.provider') }}</span>
              <strong>{{ selectedBinding?.provider ?? '-' }}</strong>
            </div>
            <div class="info-item">
              <span>{{ $t('network.info_labels.provider_network_id') }}</span>
              <strong>{{ selectedBinding?.providerNetworkId ?? '-' }}</strong>
            </div>
            <div class="info-item">
              <span>{{ $t('network.info_labels.cidr') }}</span>
              <strong>{{ selectedNetwork.cidr ?? $t('network.auto_assigned') }}</strong>
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
                  <h3>{{ $t('network.provider_bindings') }}</h3>
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
                      {{ binding.isPrimary ? $t('network.binding_primary') : $t('network.binding_secondary') }}
                    </a-tag>
                  </div>
                </div>
              </article>

              <article class="panel-card">
                <div class="panel-card__header">
                  <h3>{{ $t('network.policy') }}</h3>
                </div>
                <div class="policy-grid">
                  <div class="policy-item">
                    <span>{{ $t('network.auto_approve_members') }}</span>
                    <strong>{{ selectedNetwork.autoApproveMembers ? $t('network.enabled') : $t('network.disabled') }}</strong>
                  </div>
                  <div class="policy-item">
                    <span>{{ $t('network.auto_assign_ip') }}</span>
                    <strong>{{ selectedNetwork.v4AssignMode ? $t('network.enabled') : $t('network.disabled') }}</strong>
                  </div>
                  <div class="policy-item">
                    <span>{{ $t('network.network_mode') }}</span>
                    <strong>{{ selectedNetwork.private ? $t('network.private_mode') : $t('network.public_mode') }}</strong>
                  </div>
                  <div class="policy-item">
                    <span>{{ $t('network.status_label') }}</span>
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
                  <h3>{{ $t('network.members_section') }}</h3>
                  <span>{{ members.length ? $t('network.members_count', { count: members.length }) : $t('network.waiting_for_members') }}</span>
                </div>
                <div class="inline-actions">
                  <a-button size="small" :icon="h(ReloadOutlined)" @click="emit('sync-members')">{{ $t('network.sync_members_button') }}</a-button>
                  <a-button size="small" type="primary" ghost @click="emit('navigate', prototypeSections.access)">
                    {{ $t('network.device_access_button') }}
                  </a-button>
                </div>
              </div>

              <div class="table-wrap">
                <table class="prototype-table prototype-table--members">
                  <thead>
                    <tr>
                      <th>{{ $t('network.table_headers.device') }}</th>
                      <th>{{ $t('network.table_headers.role') }}</th>
                      <th>{{ $t('network.table_headers.virtual_ip') }}</th>
                      <th>{{ $t('network.table_headers.auth') }}</th>
                      <th>{{ $t('network.table_headers.online') }}</th>
                      <th>{{ $t('network.table_headers.tags') }}</th>
                      <th>{{ $t('network.table_headers.actions') }}</th>
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
                      <td>{{ normalizeMemberRole(member.role, $t('network.role_device')) }}</td>
                      <td class="ip-cell">
                        <a-input
                          :value="memberIpValues[member.id] ?? parseIpAssignments(member.ipAssignmentsJson).join(', ')"
                          size="small"
                          :placeholder="$t('network.member_ip_placeholder')"
                          @update:value="emit('update:member-ip', member.id, $event)"
                          @pressEnter="emit('assign-ip', member)"
                        />
                      </td>
                      <td>
                        <a-tag :color="member.authorized ? 'green' : 'orange'">
                          {{ member.authorized ? $t('network.authorized') : $t('network.pending') }}
                        </a-tag>
                      </td>
                      <td>
                        <span class="online-dot" :class="{ offline: !member.online }"></span>
                        {{ member.online ? $t('network.online') : $t('network.offline') }}
                      </td>
                      <td class="ip-cell">
                        <a-input
                          :value="memberTagValues[member.id] ?? parseTags(member.tagsJson).join(', ')"
                          size="small"
                          :placeholder="$t('network.member_tags_placeholder')"
                          @update:value="emit('update:member-tag', member.id, $event)"
                          @pressEnter="emit('assign-ip', member)"
                        />
                      </td>
                      <td>
                        <div class="row-actions">
                          <a-button size="small" type="link" @click="emit('toggle-auth', member)">
                            {{ member.authorized ? $t('network.revoke') : $t('network.authorize') }}
                          </a-button>
                          <a-button size="small" type="link" @click="emit('assign-ip', member)">
                            {{ $t('network.save_member') }}
                          </a-button>
                        </div>
                      </td>
                    </tr>
                  </tbody>
                  <tbody v-else>
                    <tr>
                      <td colspan="7" class="table-empty-cell">
                        <div class="table-empty-state">
                          <a-empty :image="simpleEmptyImage" :description="$t('network.no_member_devices')" />
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
                  <h3>{{ $t('network.routes_section') }}</h3>
                </div>
                <div v-if="routes.length" class="stack-list">
                  <div v-for="route in routes" :key="route.id" class="stack-item">
                    <div>
                      <strong>{{ route.target }}</strong>
                      <span>{{ route.type }} / {{ route.via || $t('network.provider_managed') }}</span>
                    </div>
                    <a-button size="small" danger @click="emit('delete-route', route.id)">{{ $t('network.delete') }}</a-button>
                  </div>
                </div>
                <a-empty v-else :image="simpleEmptyImage" :description="$t('network.no_routes')" />

                <div class="mini-form">
                  <a-input
                    :value="routeForm.target"
                    :placeholder="$t('network.route_target_placeholder')"
                    @update:value="emit('update:route-form', 'target', $event)"
                  />
                  <a-input
                    :value="routeForm.via"
                    :placeholder="$t('network.route_via_placeholder')"
                    @update:value="emit('update:route-form', 'via', $event)"
                  />
                  <a-button type="primary" @click="emit('create-route')">{{ $t('network.add_route') }}</a-button>
                </div>
              </article>

              <article class="panel-card">
                <div class="panel-card__header">
                  <h3>{{ $t('network.ip_pools_section') }}</h3>
                </div>
                <div v-if="pools.length" class="stack-list">
                  <div v-for="pool in pools" :key="pool.id" class="stack-item">
                    <div>
                      <strong>{{ pool.ipRangeStart }} - {{ pool.ipRangeEnd }}</strong>
                      <span>{{ pool.providerManaged ? $t('network.provider_managed') : $t('network.local_managed') }}</span>
                    </div>
                    <a-button size="small" danger @click="emit('delete-pool', pool.id)">{{ $t('network.delete') }}</a-button>
                  </div>
                </div>
                <a-empty v-else :image="simpleEmptyImage" :description="$t('network.no_ip_pools')" />

                <div class="mini-form mini-form--triple">
                  <a-input
                    :value="poolForm.ipRangeStart"
                    :placeholder="$t('network.pool_start_placeholder')"
                    @update:value="emit('update:pool-form', 'ipRangeStart', $event)"
                  />
                  <a-input
                    :value="poolForm.ipRangeEnd"
                    :placeholder="$t('network.pool_end_placeholder')"
                    @update:value="emit('update:pool-form', 'ipRangeEnd', $event)"
                  />
                  <a-button type="primary" @click="emit('create-pool')">{{ $t('network.add_ip_pool') }}</a-button>
                </div>
              </article>
            </div>
          </div>

          <div v-else-if="networkTab === 'dns'" class="network-pane">
            <div class="content-split">
              <article class="panel-card">
                <div class="panel-card__header">
                  <h3>{{ $t('network.dns_config') }}</h3>
                </div>
                <div class="form-grid">
                  <a-input
                    :value="dnsForm.domain"
                    :placeholder="$t('network.dns_domain_placeholder')"
                    @update:value="emit('update:dns-form', 'domain', $event)"
                  />
                  <a-input
                    :value="dnsForm.servers"
                    :placeholder="$t('network.dns_servers_placeholder')"
                    @update:value="emit('update:dns-form', 'servers', $event)"
                  />
                  <a-checkbox
                    :checked="dnsForm.providerManaged"
                    @update:checked="emit('update:dns-form', 'providerManaged', $event)"
                  >
                    {{ $t('network.provider_managed') }}
                  </a-checkbox>
                  <a-button type="primary" @click="emit('save-dns')">{{ $t('network.save_dns') }}</a-button>
                </div>
              </article>

              <article class="panel-card">
                <div class="panel-card__header">
                  <h3>{{ $t('network.current_values') }}</h3>
                </div>
                <div class="definition-list">
                  <div>
                    <span>{{ $t('network.domain') }}</span>
                    <strong>{{ dnsConfig?.domain || $t('network.not_set') }}</strong>
                  </div>
                  <div>
                    <span>{{ $t('network.servers') }}</span>
                    <strong>{{ dnsConfig?.servers.join(', ') || $t('network.not_set') }}</strong>
                  </div>
                  <div>
                    <span>{{ $t('network.source') }}</span>
                    <strong>{{ dnsConfig?.providerManaged ? $t('network.provider') : $t('network.local') }}</strong>
                  </div>
                </div>
              </article>
            </div>
          </div>

          <div v-else class="network-pane">
            <article class="panel-card panel-card--placeholder">
              <div class="panel-card__header">
                <h3>{{ placeholderTitles[networkTab as PlaceholderTabKey] }}</h3>
              </div>
              <p>{{ $t('network.placeholder_reserved') }}</p>
            </article>
          </div>
        </template>

        <a-empty
          v-else
          :image="simpleEmptyImage"
          :description="$t('network.empty_select_network')"
        />
      </div>
    </div>
  </section>
</template>

<script setup lang="ts">
import { h } from 'vue';
import { ApiOutlined, DeploymentUnitOutlined, ReloadOutlined } from '@ant-design/icons-vue';
import { Empty } from 'ant-design-vue';

import type {
  DetailMetric,
  NetworkTabKey,
  PlaceholderTabKey,
  PrototypeSectionKey,
  SectionNavItem
} from '../../types';
import { placeholderTitles, prototypeSections } from '../../constants';
import { normalizeMemberRole, parseIpAssignments, parseTags } from '../../utils';
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
  memberTagValues: Record<string, string>;
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
  'update:member-tag': [memberId: string, value: string];
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

function onNetworkChange(value: unknown) {
  emit('update:selected-network-id', normalizeSelectValue(value));
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
