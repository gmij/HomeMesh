import { computed, onMounted, reactive, ref, watch } from 'vue';
import { message } from 'ant-design-vue';
import {
  ApiOutlined,
  ApartmentOutlined,
  DashboardOutlined,
  DeploymentUnitOutlined
} from '@ant-design/icons-vue';

import type {
  AuditLog,
  AuthStatusResponse,
  AuthUser,
  ConfigSyncResult,
  DashboardSummary,
  DnsConfig,
  IpPoolItem,
  Member,
  MemberSyncResult,
  NetworkDetail,
  NetworkProviderBinding,
  NetworkSummary,
  ProviderStatus,
  RouteItem,
  SyncState
} from '../../../api/types';
import { request, requestMaybe } from '../../../api/client';
import type {
  AccessFormState,
  AuthFormState,
  AuthMode,
  CreateNetworkFormState,
  DashboardStatusItem,
  DetailMetric,
  DnsFormState,
  EasySetupFormState,
  HealthStatus,
  NetworkTabKey,
  PoolFormState,
  ProviderCardModel,
  RouteFormState,
  SummaryMetric,
  SyncCardModel
} from '../types';
import {
  buildPseudoQrMatrix,
  friendlyError,
  formatTime,
  isHealthyStatus,
  parseCsv,
  parseIpAssignments
} from '../utils';

let sharedState: ReturnType<typeof createAdminConsoleState> | null = null;

function createAdminConsoleState() {
  const authMode = ref<AuthMode>('loading');
  const authLoading = ref(false);
  const refreshing = ref(false);
  const createLoading = ref(false);
  const deleteLoading = ref(false);
  const easySetupLoading = ref(false);
  const createModalOpen = ref(false);
  const easySetupModalOpen = ref(false);
  const networkTab = ref<NetworkTabKey>('members');
  const user = ref<AuthUser | null>(null);
  const healthStatus = ref<HealthStatus | null>(null);
  const providers = ref<ProviderStatus[]>([]);
  const summary = ref<DashboardSummary | null>(null);
  const networks = ref<NetworkSummary[]>([]);
  const selectedNetworkId = ref('');
  const selectedNetwork = ref<NetworkDetail | null>(null);
  const members = ref<Member[]>([]);
  const routes = ref<RouteItem[]>([]);
  const pools = ref<IpPoolItem[]>([]);
  const dnsConfig = ref<DnsConfig | null>(null);
  const memberSyncStates = ref<SyncState[]>([]);
  const configSyncStates = ref<SyncState[]>([]);
  const audits = ref<AuditLog[]>([]);
  const inviteVersion = ref(Date.now());
  const networkProviderNames = reactive<Record<string, string>>({});
  const memberIpValues = reactive<Record<string, string>>({});

  const authForm = reactive<AuthFormState>({
    username: 'admin',
    password: ''
  });

  const createForm = reactive<CreateNetworkFormState>({
    name: 'Home Network',
    provider: 'ZeroTier',
    cidr: '10.10.0.0/16',
    private: true,
    autoApproveMembers: false
  });

  const easySetupForm = reactive<EasySetupFormState>({
    cidr: '10.10.0.0/16',
    ipPoolStart: '10.10.0.10',
    ipPoolEnd: '10.10.0.200',
    enableAutoAssign: true,
    autoApproveMembers: false,
    dnsDomain: 'home.arpa',
    dnsServers: '1.1.1.1, 8.8.8.8'
  });

  const routeForm = reactive<RouteFormState>({
    target: '',
    via: '',
    type: 'VirtualSubnet',
    enabled: true,
    providerManaged: true
  });

  const poolForm = reactive<PoolFormState>({
    ipRangeStart: '',
    ipRangeEnd: '',
    providerManaged: true
  });

  const dnsForm = reactive<DnsFormState>({
    domain: '',
    servers: '',
    providerManaged: true
  });

  const accessForm = reactive<AccessFormState>({
    expiryDays: '7',
    label: '',
    autoApprove: true
  });

  const providerOptions = computed(() => {
    const names = providers.value.length
      ? providers.value.map((provider) => provider.providerName)
      : ['ZeroTier'];

    return names.map((provider) => ({
      label: provider,
      value: provider
    }));
  });

  const networkSelectOptions = computed(() =>
    networks.value.map((network) => ({
      label: network.name,
      value: network.id
    }))
  );

  const selectedBinding = computed<NetworkProviderBinding | null>(
    () => selectedNetwork.value?.providerBindings[0] ?? null
  );

  const recentAudits = computed(() => audits.value.slice(0, 5));
  const formattedLastAudit = computed(() =>
    summary.value?.lastAuditAt ? formatTime(summary.value.lastAuditAt) : 'No records yet'
  );
  const userLabel = computed(() => user.value?.username ?? 'admin');

  const summaryMetrics = computed<SummaryMetric[]>(() => {
    const data = summary.value;
    return [
      {
        label: 'Networks',
        value: data?.networkCount ?? 0,
        meta: 'Created networks',
        icon: ApartmentOutlined,
        tone: 'blue'
      },
      {
        label: 'Registered Devices',
        value: data?.memberCount ?? 0,
        meta: `Online ${data?.onlineMemberCount ?? 0}`,
        icon: DeploymentUnitOutlined,
        tone: 'cyan'
      },
      {
        label: 'Gateways',
        value: selectedNetwork.value?.gatewayCount ?? 0,
        meta: 'For the selected network',
        icon: ApiOutlined,
        tone: 'violet'
      },
      {
        label: 'Policies',
        value: data?.routeCount ?? 0,
        meta: `IP Pools ${data?.ipPoolCount ?? 0}`,
        icon: DashboardOutlined,
        tone: 'purple'
      }
    ];
  });

  const dashboardStatusItems = computed<DashboardStatusItem[]>(() => {
    const healthyProviders = providers.value.filter((provider) => isHealthyStatus(provider.status)).length;
    const totalProviders = providers.value.length || 1;
    const activeProvider = selectedBinding.value?.provider ?? providers.value[0]?.providerName ?? 'ZeroTier';
    const authorizedMembers = summary.value?.authorizedMemberCount ?? 0;
    const totalMembers = summary.value?.memberCount ?? 0;
    const syncIssues = summary.value?.errorSyncCount ?? 0;

    return [
      {
        key: 'health',
        label: 'Platform Health',
        value: healthStatus.value?.status ?? 'Unknown',
        tone: 'blue',
        note: healthStatus.value?.checkedAt ? `Last checked ${formatTime(healthStatus.value.checkedAt, 'time')}` : 'Waiting for health check'
      },
      {
        key: 'provider',
        label: 'Protocol Provider',
        value: activeProvider,
        tone: 'cyan',
        note: `${healthyProviders}/${totalProviders} 鍙敤`
      },
      {
        key: 'authorization',
        label: 'Member Authorization',
        value: totalMembers ? `${authorizedMembers}/${totalMembers}` : '0/0',
        tone: 'violet',
        note: totalMembers ? 'Authorized member count' : 'Waiting for device access'
      },
      {
        key: 'sync',
        label: 'Sync Status',
        value: syncIssues ? `${syncIssues} issues` : 'Normal',
        tone: 'purple',
        note: syncIssues ? 'Please review the network page for details' : 'No sync issues right now'
      }
    ];
  });

  const detailMetrics = computed<DetailMetric[]>(() => {
    const network = selectedNetwork.value;
    if (!network) {
      return [];
    }

    return [
      { label: 'Member Devices', value: network.memberCount, meta: `Online ${network.onlineMemberCount}` },
      { label: 'Gateways', value: network.gatewayCount, meta: 'Current network' },
      { label: 'Routes', value: network.routeCount, meta: `Provider ${selectedBinding.value?.provider ?? '-'}` },
      { label: 'Sync Tasks', value: memberSyncStates.value.length + configSyncStates.value.length, meta: 'Recent jobs' }
    ];
  });

  const syncCards = computed<SyncCardModel[]>(() => {
    const memberCards = memberSyncStates.value.map((state) => ({
      key: `member-${state.id}`,
      title: `${state.provider} member sync`,
      status: state.status,
      message: state.lastError || 'Member sync looks healthy.',
      time: state.lastSyncAt ? formatTime(state.lastSyncAt) : 'Not synced yet'
    }));

    const configCards = configSyncStates.value.map((state) => ({
      key: `config-${state.id}`,
      title: `${state.provider} config sync`,
      status: state.status,
      message: state.lastError || 'Config sync looks healthy.',
      time: state.lastSyncAt ? formatTime(state.lastSyncAt) : 'Not synced yet'
    }));

    return [...memberCards, ...configCards];
  });

  const providerWarning = computed(() => {
    const unhealthy = providers.value.find((provider) => !isHealthyStatus(provider.status));
    if (!unhealthy) {
      return null;
    }

    return {
      status: unhealthy.status,
      message: `${unhealthy.providerName} status is ${unhealthy.status}${unhealthy.message ? `: ${unhealthy.message}` : ''}`
    };
  });

  const accessNetworkName = computed(() => selectedNetwork.value?.name || 'No network selected');
  const accessCode = computed(() => {
    const networkId = selectedNetworkId.value || 'hmnet-preview';
    const tag = accessForm.label.trim() || 'client';
    const stamp = inviteVersion.value.toString(36).slice(-6).toUpperCase();
    return `HMM-${networkId.slice(-4).toUpperCase()}-${accessForm.expiryDays}D-${tag
      .replace(/\s+/g, '-')
      .slice(0, 6)
      .toUpperCase()}-${stamp}`;
  });
  const qrCells = computed(() => buildPseudoQrMatrix(`${selectedNetworkId.value}|${accessCode.value}`));

  const providerCards = computed<ProviderCardModel[]>(() => {
    const actual = new Map(providers.value.map((provider) => [provider.providerName.toLowerCase(), provider]));
    const activeBinding = selectedBinding.value;
    const zeroTier = actual.get('zerotier');
    const zeroTierHealthy = zeroTier ? isHealthyStatus(zeroTier.status) : false;

    return [
      {
        key: 'zerotier',
        badge: 'ZT',
        title: 'ZeroTier',
        stage: zeroTierHealthy ? 'Connected' : 'Issue',
        description: zeroTierHealthy
          ? 'The active control plane is connected to the real provider path for network creation and member sync.'
          : 'ZeroTier is configured, but the connection must be fixed before sync and config push can continue.',
        status: zeroTier?.status ?? 'Unknown',
        controlPlane: zeroTier?.message || 'global.zerotier.com',
        networkId: activeBinding?.provider === 'ZeroTier' ? activeBinding.providerNetworkId : '-',
        actionLabel: 'Manage Config',
        actionType: 'primary',
        disabled: false,
        tone: 'gold',
        tagColor: zeroTierHealthy ? 'green' : 'red'
      },
      {
        key: 'wireguard',
        badge: 'WG',
        title: 'WireGuard',
        stage: 'Reserved',
        description: 'Reserved layout for a future WireGuard provider integration.',
        status: 'Planned',
        controlPlane: 'Reserved',
        networkId: '-',
        actionLabel: 'Learn More',
        actionType: 'default',
        disabled: true,
        tone: 'purple',
        tagColor: 'orange'
      },
      {
        key: 'quic',
        badge: 'QC',
        title: 'Custom QUIC',
        stage: 'Planned',
        description: 'Reserved for a lower-latency custom QUIC protocol layer in a later phase.',
        status: 'Planned',
        controlPlane: 'Reserved',
        networkId: '-',
        actionLabel: 'Learn More',
        actionType: 'default',
        disabled: true,
        tone: 'blue',
        tagColor: 'purple'
      }
    ];
  });

  watch(selectedNetworkId, async (value, previous) => {
    if (!value || value === previous || authMode.value !== 'ready') {
      return;
    }
    await loadSelectedNetwork(value);
  });

  onMounted(async () => {
    await boot();
  });

  async function boot() {
    try {
      healthStatus.value = await request<HealthStatus>('/health');
    } catch {
      healthStatus.value = null;
    }

    try {
      const status = await request<AuthStatusResponse>('/api/auth/status');
      applyAuthStatus(status);
      if (status.initialized && status.authenticated) {
        await refreshAll();
      }
    } catch (error) {
      authMode.value = 'login';
      handleError(error, 'Failed to read auth status');
    }
  }

  function applyAuthStatus(status: AuthStatusResponse) {
    user.value = status.user ?? null;
    if (!status.initialized) {
      authMode.value = 'setup';
      return;
    }
    authMode.value = status.authenticated ? 'ready' : 'login';
  }

  async function submitAuth() {
    if (!authForm.username.trim() || !authForm.password.trim()) {
      message.warning('Please enter a username and password.');
      return;
    }

    authLoading.value = true;
    try {
      if (authMode.value === 'setup') {
        await request('/api/setup/admin', {
          method: 'POST',
          body: JSON.stringify({
            username: authForm.username.trim(),
            password: authForm.password
          })
        });

        message.success('Administrator created. Please sign in.');
        authMode.value = 'login';
        authForm.password = '';
        return;
      }

      await request('/api/auth/login', {
        method: 'POST',
        body: JSON.stringify({
          username: authForm.username.trim(),
          password: authForm.password
        })
      });

      const status = await request<AuthStatusResponse>('/api/auth/status');
      applyAuthStatus(status);
      authForm.password = '';
      await refreshAll();
    } catch (error) {
      handleError(error, authMode.value === 'setup' ? 'Initialization failed' : 'Login failed');
    } finally {
      authLoading.value = false;
    }
  }

  async function logout() {
    try {
      await request('/api/auth/logout', { method: 'POST' });
      user.value = null;
      authMode.value = 'login';
      message.success('Signed out.');
    } catch (error) {
      handleError(error, 'Logout failed');
    }
  }

  async function refreshAll() {
    if (authMode.value !== 'ready') {
      return;
    }

    refreshing.value = true;
    try {
      const [health, providerData, summaryData, networkData, auditData] = await Promise.all([
        request<HealthStatus>('/health'),
        request<ProviderStatus[]>('/api/providers'),
        request<DashboardSummary>('/api/dashboard/summary'),
        request<NetworkSummary[]>('/api/networks'),
        request<AuditLog[]>('/api/audit-logs')
      ]);

      healthStatus.value = health;
      providers.value = providerData;
      summary.value = summaryData;
      networks.value = networkData;
      audits.value = auditData;

      if (!selectedNetworkId.value || !networkData.some((network) => network.id === selectedNetworkId.value)) {
        selectedNetworkId.value = networkData[0]?.id ?? '';
      }

      if (selectedNetworkId.value) {
        await loadSelectedNetwork(selectedNetworkId.value);
      } else {
        clearSelectedNetwork();
      }
    } catch (error) {
      handleError(error, 'Failed to refresh console data');
    } finally {
      refreshing.value = false;
    }
  }

  async function loadSelectedNetwork(networkId: string) {
    try {
      const [detail, memberData, routeData, poolData, dnsData, memberStates, configStates] = await Promise.all([
        request<NetworkDetail>(`/api/networks/${networkId}`),
        request<Member[]>(`/api/networks/${networkId}/members`),
        request<RouteItem[]>(`/api/networks/${networkId}/routes`),
        request<IpPoolItem[]>(`/api/networks/${networkId}/ip-pools`),
        requestMaybe<DnsConfig>(`/api/networks/${networkId}/dns`),
        request<SyncState[]>(`/api/networks/${networkId}/members/sync-state`),
        request<SyncState[]>(`/api/networks/${networkId}/config/sync-state`)
      ]);

      selectedNetwork.value = detail;
      members.value = memberData;
      routes.value = routeData;
      pools.value = poolData;
      dnsConfig.value = dnsData;
      memberSyncStates.value = memberStates;
      configSyncStates.value = configStates;
      networkProviderNames[networkId] = detail.providerBindings[0]?.provider ?? '-';
      hydrateNetworkForms();
    } catch (error) {
      handleError(error, 'Failed to load network details');
    }
  }

  function hydrateNetworkForms() {
    const network = selectedNetwork.value;
    if (!network) {
      return;
    }

    easySetupForm.cidr = network.cidr || easySetupForm.cidr;
    easySetupForm.enableAutoAssign = network.v4AssignMode;
    easySetupForm.autoApproveMembers = network.autoApproveMembers;

    if (pools.value[0]) {
      easySetupForm.ipPoolStart = pools.value[0].ipRangeStart;
      easySetupForm.ipPoolEnd = pools.value[0].ipRangeEnd;
      poolForm.ipRangeStart = pools.value[0].ipRangeStart;
      poolForm.ipRangeEnd = pools.value[0].ipRangeEnd;
    } else {
      poolForm.ipRangeStart = '';
      poolForm.ipRangeEnd = '';
    }

    dnsForm.domain = dnsConfig.value?.domain ?? '';
    dnsForm.servers = dnsConfig.value?.servers.join(', ') ?? '';
    dnsForm.providerManaged = dnsConfig.value?.providerManaged ?? true;
    easySetupForm.dnsDomain = dnsForm.domain;
    easySetupForm.dnsServers = dnsForm.servers;

    for (const member of members.value) {
      memberIpValues[member.id] = parseIpAssignments(member.ipAssignmentsJson).join(', ');
    }
  }

  function clearSelectedNetwork() {
    selectedNetwork.value = null;
    members.value = [];
    routes.value = [];
    pools.value = [];
    dnsConfig.value = null;
    memberSyncStates.value = [];
    configSyncStates.value = [];
  }

  async function createNetwork() {
    if (!createForm.name.trim()) {
      message.warning('Please enter a network name.');
      return;
    }

    createLoading.value = true;
    try {
      const created = await request<NetworkSummary>('/api/networks', {
        method: 'POST',
        body: JSON.stringify({
          name: createForm.name.trim(),
          provider: createForm.provider,
          cidr: createForm.cidr.trim() || null,
          private: createForm.private,
          autoApproveMembers: createForm.autoApproveMembers
        })
      });

      createModalOpen.value = false;
      selectedNetworkId.value = created.id;
      await refreshAll();
      message.success(`Network ${created.name} created.`);
    } catch (error) {
      handleError(error, 'Failed to create network');
    } finally {
      createLoading.value = false;
    }
  }

  async function deleteNetwork() {
    if (!selectedNetworkId.value || !selectedNetwork.value) {
      message.warning('Please select a network to delete.');
      return;
    }

    deleteLoading.value = true;
    try {
      const networkName = selectedNetwork.value.name;
      await request(`/api/networks/${selectedNetworkId.value}`, {
        method: 'DELETE'
      });

      await refreshAll();
      message.success(`Network ${networkName} deleted.`);
    } catch (error) {
      handleError(error, 'Failed to delete network');
    } finally {
      deleteLoading.value = false;
    }
  }

  function buildPlantFileUrl(networkId: string) {
    return `/api/networks/${networkId}/plant-file`;
  }

  function downloadPlantFile() {
    if (!selectedNetworkId.value) {
      message.warning('Please select a network first.');
      return;
    }
    window.open(buildPlantFileUrl(selectedNetworkId.value), '_blank', 'noopener');
  }

  async function applyEasySetup() {
    if (!selectedNetworkId.value) {
      message.warning('Please select a network first.');
      return;
    }

    easySetupLoading.value = true;
    try {
      await request(`/api/networks/${selectedNetworkId.value}/easy-setup`, {
        method: 'POST',
        body: JSON.stringify({
          cidr: easySetupForm.cidr.trim(),
          ipPoolStart: easySetupForm.ipPoolStart.trim(),
          ipPoolEnd: easySetupForm.ipPoolEnd.trim(),
          enableAutoAssign: easySetupForm.enableAutoAssign,
          autoApproveMembers: easySetupForm.autoApproveMembers,
          dnsDomain: easySetupForm.dnsDomain.trim() || null,
          dnsServers: parseCsv(easySetupForm.dnsServers)
        })
      });

      easySetupModalOpen.value = false;
      await loadSelectedNetwork(selectedNetworkId.value);
      await loadAudits();
      message.success('Easy Setup applied.');
    } catch (error) {
      handleError(error, 'Easy Setup failed');
    } finally {
      easySetupLoading.value = false;
    }
  }

  async function createRoute() {
    if (!selectedNetworkId.value) {
      message.warning('Please select a network first.');
      return;
    }
    if (!routeForm.target.trim()) {
      message.warning('Please enter a target CIDR.');
      return;
    }

    try {
      await request(`/api/networks/${selectedNetworkId.value}/routes`, {
        method: 'POST',
        body: JSON.stringify({
          target: routeForm.target.trim(),
          via: routeForm.via.trim() || null,
          type: routeForm.type,
          enabled: routeForm.enabled,
          providerManaged: routeForm.providerManaged
        })
      });

      routeForm.target = '';
      routeForm.via = '';
      await loadSelectedNetwork(selectedNetworkId.value);
      await loadAudits();
      message.success('Route added.');
    } catch (error) {
      handleError(error, 'Failed to add route');
    }
  }

  async function deleteRoute(routeId: string) {
    if (!selectedNetworkId.value) {
      return;
    }
    try {
      await request(`/api/networks/${selectedNetworkId.value}/routes/${routeId}`, { method: 'DELETE' });
      await loadSelectedNetwork(selectedNetworkId.value);
      await loadAudits();
      message.success('Route deleted.');
    } catch (error) {
      handleError(error, 'Failed to delete route');
    }
  }

  async function createPool() {
    if (!selectedNetworkId.value) {
      message.warning('Please select a network first.');
      return;
    }
    if (!poolForm.ipRangeStart.trim() || !poolForm.ipRangeEnd.trim()) {
      message.warning('Please enter both start and end IP addresses.');
      return;
    }

    try {
      await request(`/api/networks/${selectedNetworkId.value}/ip-pools`, {
        method: 'POST',
        body: JSON.stringify({
          ipRangeStart: poolForm.ipRangeStart.trim(),
          ipRangeEnd: poolForm.ipRangeEnd.trim(),
          providerManaged: poolForm.providerManaged
        })
      });

      await loadSelectedNetwork(selectedNetworkId.value);
      await loadAudits();
      message.success('IP pool added.');
    } catch (error) {
      handleError(error, 'Failed to add IP pool');
    }
  }

  async function deletePool(poolId: string) {
    if (!selectedNetworkId.value) {
      return;
    }
    try {
      await request(`/api/networks/${selectedNetworkId.value}/ip-pools/${poolId}`, { method: 'DELETE' });
      await loadSelectedNetwork(selectedNetworkId.value);
      await loadAudits();
      message.success('IP pool deleted.');
    } catch (error) {
      handleError(error, 'Failed to delete IP pool');
    }
  }

  async function saveDns() {
    if (!selectedNetworkId.value) {
      message.warning('Please select a network first.');
      return;
    }
    try {
      await request(`/api/networks/${selectedNetworkId.value}/dns`, {
        method: 'PUT',
        body: JSON.stringify({
          domain: dnsForm.domain.trim() || null,
          servers: parseCsv(dnsForm.servers),
          providerManaged: dnsForm.providerManaged
        })
      });

      await loadSelectedNetwork(selectedNetworkId.value);
      await loadAudits();
      message.success('DNS saved.');
    } catch (error) {
      handleError(error, 'Failed to save DNS');
    }
  }

  async function syncMembers() {
    if (!selectedNetworkId.value) {
      message.warning('Please select a network first.');
      return;
    }
    try {
      const result = await request<MemberSyncResult>(`/api/networks/${selectedNetworkId.value}/members/sync`, {
        method: 'POST'
      });
      await loadSelectedNetwork(selectedNetworkId.value);
      await loadAudits();
      networkTab.value = 'sync';
      message.success(`${result.provider} member sync completed.`);
    } catch (error) {
      handleError(error, 'Failed to sync members');
    }
  }

  async function syncConfig() {
    if (!selectedNetworkId.value) {
      message.warning('Please select a network first.');
      return;
    }
    try {
      const result = await request<ConfigSyncResult>(`/api/networks/${selectedNetworkId.value}/config/sync`, {
        method: 'POST'
      });
      await loadSelectedNetwork(selectedNetworkId.value);
      await loadAudits();
      networkTab.value = 'sync';
      message.success(`${result.provider} config sync completed.`);
    } catch (error) {
      handleError(error, 'Failed to sync config');
    }
  }

  async function toggleMemberAuth(member: Member) {
    if (!selectedNetworkId.value) {
      return;
    }
    try {
      await request(
        `/api/networks/${selectedNetworkId.value}/members/${member.id}/${member.authorized ? 'deauthorize' : 'authorize'}`,
        { method: 'POST' }
      );
      await loadSelectedNetwork(selectedNetworkId.value);
      await loadAudits();
      message.success(`${member.name || member.providerMemberId} authorization updated.`);
    } catch (error) {
      handleError(error, 'Failed to update member authorization');
    }
  }

  async function assignIp(member: Member) {
    if (!selectedNetworkId.value) {
      return;
    }
    try {
      await request(`/api/networks/${selectedNetworkId.value}/members/${member.id}`, {
        method: 'PATCH',
        body: JSON.stringify({
          ipAssignments: parseCsv(memberIpValues[member.id] || '')
        })
      });

      await loadSelectedNetwork(selectedNetworkId.value);
      await loadAudits();
      message.success(`${member.name || member.providerMemberId} IP assignments saved.`);
    } catch (error) {
      handleError(error, 'Failed to save member IP assignments');
    }
  }

  async function loadAudits() {
    try {
      audits.value = await request<AuditLog[]>('/api/audit-logs');
    } catch (error) {
      handleError(error, 'Failed to load audit logs');
    }
  }

  function generateAccessArtifact() {
    inviteVersion.value = Date.now();
    message.info('Access code and QR preview refreshed. Plant file download still uses the live backend endpoint.');
  }

  async function copyAccessCode() {
    try {
      await navigator.clipboard.writeText(accessCode.value);
      message.success('Access code copied.');
    } catch {
      message.warning('Clipboard is not available in this environment. Please copy the access code manually.');
    }
  }

  function onMemberIpInput(memberId: string, value: string) {
    memberIpValues[memberId] = value;
  }

  function openNetwork(networkId: string) {
    selectedNetworkId.value = networkId;
    networkTab.value = 'members';
  }

  function updateRouteForm(field: 'target' | 'via', value: string) {
    routeForm[field] = value;
  }

  function updatePoolForm(field: 'ipRangeStart' | 'ipRangeEnd', value: string) {
    poolForm[field] = value;
  }

  function updateDnsForm(field: 'domain' | 'servers' | 'providerManaged', value: string | boolean) {
    if (field === 'providerManaged') {
      dnsForm.providerManaged = Boolean(value);
      return;
    }
    dnsForm[field] = String(value);
  }

  function updateAccessForm(field: 'expiryDays' | 'label' | 'autoApprove', value: string | boolean) {
    if (field === 'autoApprove') {
      accessForm.autoApprove = Boolean(value);
      return;
    }
    accessForm[field] = String(value);
  }

  function updateCreateForm(
    field: 'name' | 'provider' | 'cidr' | 'private' | 'autoApproveMembers',
    value: string | boolean
  ) {
    if (field === 'private' || field === 'autoApproveMembers') {
      createForm[field] = Boolean(value);
      return;
    }
    createForm[field] = String(value);
  }

  function updateEasySetupForm(
    field:
      | 'cidr'
      | 'ipPoolStart'
      | 'ipPoolEnd'
      | 'enableAutoAssign'
      | 'autoApproveMembers'
      | 'dnsDomain'
      | 'dnsServers',
    value: string | boolean
  ) {
    if (field === 'enableAutoAssign' || field === 'autoApproveMembers') {
      easySetupForm[field] = Boolean(value);
      return;
    }
    easySetupForm[field] = String(value);
  }

  function handleError(error: unknown, fallback: string) {
    message.error(`${fallback}: ${friendlyError(error)}`);
  }

  return {
    accessCode,
    accessForm,
    accessNetworkName,
    audits,
    authForm,
    authLoading,
    authMode,
    configSyncStates,
    createForm,
    createLoading,
    createModalOpen,
    deleteLoading,
    deleteNetwork,
    deletePool,
    deleteRoute,
    detailMetrics,
    dashboardStatusItems,
    dnsConfig,
    dnsForm,
    downloadPlantFile,
    easySetupForm,
    easySetupLoading,
    easySetupModalOpen,
    formattedLastAudit,
    generateAccessArtifact,
    healthStatus,
    loadAudits,
    logout,
    memberIpValues,
    members,
    networkProviderNames,
    networkSelectOptions,
    networkTab,
    networks,
    onMemberIpInput,
    openNetwork,
    pools,
    providerCards,
    providerOptions,
    providerWarning,
    poolForm,
    providers,
    qrCells,
    recentAudits,
    refreshAll,
    routeForm,
    routes,
    saveDns,
    selectedBinding,
    selectedNetwork,
    selectedNetworkId,
    submitAuth,
    summary,
    summaryMetrics,
    syncCards,
    syncConfig,
    syncMembers,
    toggleMemberAuth,
    updateAccessForm,
    updateCreateForm,
    updateDnsForm,
    updateEasySetupForm,
    updatePoolForm,
    updateRouteForm,
    user,
    userLabel,
    assignIp,
    applyEasySetup,
    copyAccessCode,
    createNetwork,
    createPool,
    createRoute,
    refreshing
  };
}

export function useAdminConsole() {
  if (!sharedState) {
    sharedState = createAdminConsoleState();
  }

  return sharedState;
}
