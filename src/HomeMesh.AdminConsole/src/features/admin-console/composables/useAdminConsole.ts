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
    name: '主家庭网络',
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
    summary.value?.lastAuditAt ? formatTime(summary.value.lastAuditAt) : '暂无记录'
  );
  const userLabel = computed(() => user.value?.username ?? 'admin');

  const summaryMetrics = computed<SummaryMetric[]>(() => {
    const data = summary.value;
    return [
      {
        label: '家庭网络',
        value: data?.networkCount ?? 0,
        meta: '已创建网络数',
        icon: ApartmentOutlined,
        tone: 'blue'
      },
      {
        label: '已注册设备',
        value: data?.memberCount ?? 0,
        meta: `在线 ${data?.onlineMemberCount ?? 0}`,
        icon: DeploymentUnitOutlined,
        tone: 'cyan'
      },
      {
        label: '网关节点',
        value: selectedNetwork.value?.gatewayCount ?? 0,
        meta: '来自当前主网络',
        icon: ApiOutlined,
        tone: 'violet'
      },
      {
        label: '策略规则',
        value: data?.routeCount ?? 0,
        meta: `IP 池 ${data?.ipPoolCount ?? 0}`,
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
        label: '平台健康',
        value: healthStatus.value?.status ?? 'Unknown',
        tone: 'blue',
        note: healthStatus.value?.checkedAt ? `最近检查 ${formatTime(healthStatus.value.checkedAt, 'time')}` : '等待健康检查'
      },
      {
        key: 'provider',
        label: '协议 Provider',
        value: activeProvider,
        tone: 'cyan',
        note: `${healthyProviders}/${totalProviders} 可用`
      },
      {
        key: 'authorization',
        label: '成员授权',
        value: totalMembers ? `${authorizedMembers}/${totalMembers}` : '0/0',
        tone: 'violet',
        note: totalMembers ? '已授权成员数' : '等待设备接入'
      },
      {
        key: 'sync',
        label: '同步状态',
        value: syncIssues ? `${syncIssues} 项异常` : '运行正常',
        tone: 'purple',
        note: syncIssues ? '建议前往家庭网络页处理' : '当前没有待处理异常'
      }
    ];
  });

  const detailMetrics = computed<DetailMetric[]>(() => {
    const network = selectedNetwork.value;
    if (!network) {
      return [];
    }

    return [
      { label: '成员设备', value: network.memberCount, meta: `在线 ${network.onlineMemberCount}` },
      { label: '网关节点', value: network.gatewayCount, meta: '当前网络' },
      { label: '路由规则', value: network.routeCount, meta: `Provider ${selectedBinding.value?.provider ?? '-'}` },
      { label: '同步状态', value: memberSyncStates.value.length + configSyncStates.value.length, meta: '最近任务记录' }
    ];
  });

  const syncCards = computed<SyncCardModel[]>(() => {
    const memberCards = memberSyncStates.value.map((state) => ({
      key: `member-${state.id}`,
      title: `${state.provider} 成员同步`,
      status: state.status,
      message: state.lastError || '成员同步状态正常。',
      time: state.lastSyncAt ? formatTime(state.lastSyncAt) : '尚未同步'
    }));

    const configCards = configSyncStates.value.map((state) => ({
      key: `config-${state.id}`,
      title: `${state.provider} 配置同步`,
      status: state.status,
      message: state.lastError || '配置同步状态正常。',
      time: state.lastSyncAt ? formatTime(state.lastSyncAt) : '尚未同步'
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
      message: `${unhealthy.providerName} 当前状态为 ${unhealthy.status}${unhealthy.message ? `：${unhealthy.message}` : ''}`
    };
  });

  const accessNetworkName = computed(() => selectedNetwork.value?.name || '未选择网络');
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
        stage: zeroTierHealthy ? '已连接' : '异常',
        description: zeroTierHealthy
          ? '当前控制台已接入的协议 Provider，网络创建和成员同步都走这一条链路。'
          : '当前控制台已接入 ZeroTier，但需要先修复连接状态后再继续网络同步与配置下发。',
        status: zeroTier?.status ?? 'Unknown',
        controlPlane: zeroTier?.message || 'global.zerotier.com',
        networkId: activeBinding?.provider === 'ZeroTier' ? activeBinding.providerNetworkId : '-',
        actionLabel: '管理配置',
        actionType: 'primary',
        disabled: false,
        tone: 'gold',
        tagColor: zeroTierHealthy ? 'green' : 'red'
      },
      {
        key: 'wireguard',
        badge: 'WG',
        title: 'WireGuard',
        stage: '预留',
        description: '这里先把版式和演进位准备好，等后端 Provider 接上之后直接落进去。',
        status: 'Planned',
        controlPlane: 'Reserved',
        networkId: '-',
        actionLabel: '了解更多',
        actionType: 'default',
        disabled: true,
        tone: 'purple',
        tagColor: 'orange'
      },
      {
        key: 'quic',
        badge: 'QC',
        title: 'Custom QUIC',
        stage: '规划',
        description: '预留给更低时延和更细粒度控制的后续协议层实现。',
        status: 'Planned',
        controlPlane: 'Reserved',
        networkId: '-',
        actionLabel: '了解更多',
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
      handleError(error, '读取登录状态失败');
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
      message.warning('请输入用户名和密码。');
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

        message.success('管理员已创建，请直接登录。');
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
      handleError(error, authMode.value === 'setup' ? '初始化失败' : '登录失败');
    } finally {
      authLoading.value = false;
    }
  }

  async function logout() {
    try {
      await request('/api/auth/logout', { method: 'POST' });
      user.value = null;
      authMode.value = 'login';
      message.success('已退出登录。');
    } catch (error) {
      handleError(error, '退出失败');
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
      handleError(error, '刷新控制台失败');
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
      handleError(error, '读取网络详情失败');
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
      message.warning('请输入网络名称。');
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
      message.success(`网络 ${created.name} 已创建。`);
    } catch (error) {
      handleError(error, '创建网络失败');
    } finally {
      createLoading.value = false;
    }
  }

  function buildPlantFileUrl(networkId: string) {
    return `/api/networks/${networkId}/plant-file`;
  }

  function downloadPlantFile() {
    if (!selectedNetworkId.value) {
      message.warning('请先选择网络。');
      return;
    }
    window.open(buildPlantFileUrl(selectedNetworkId.value), '_blank', 'noopener');
  }

  async function applyEasySetup() {
    if (!selectedNetworkId.value) {
      message.warning('请先选择网络。');
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
      message.success('Easy Setup 已应用。');
    } catch (error) {
      handleError(error, 'Easy Setup 失败');
    } finally {
      easySetupLoading.value = false;
    }
  }

  async function createRoute() {
    if (!selectedNetworkId.value) {
      message.warning('请先选择网络。');
      return;
    }
    if (!routeForm.target.trim()) {
      message.warning('请输入路由目标 CIDR。');
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
      message.success('路由已新增。');
    } catch (error) {
      handleError(error, '新增路由失败');
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
      message.success('路由已删除。');
    } catch (error) {
      handleError(error, '删除路由失败');
    }
  }

  async function createPool() {
    if (!selectedNetworkId.value) {
      message.warning('请先选择网络。');
      return;
    }
    if (!poolForm.ipRangeStart.trim() || !poolForm.ipRangeEnd.trim()) {
      message.warning('请填写起始和结束 IP。');
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
      message.success('IP 池已新增。');
    } catch (error) {
      handleError(error, '新增 IP 池失败');
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
      message.success('IP 池已删除。');
    } catch (error) {
      handleError(error, '删除 IP 池失败');
    }
  }

  async function saveDns() {
    if (!selectedNetworkId.value) {
      message.warning('请先选择网络。');
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
      message.success('DNS 已保存。');
    } catch (error) {
      handleError(error, '保存 DNS 失败');
    }
  }

  async function syncMembers() {
    if (!selectedNetworkId.value) {
      message.warning('请先选择网络。');
      return;
    }
    try {
      const result = await request<MemberSyncResult>(`/api/networks/${selectedNetworkId.value}/members/sync`, {
        method: 'POST'
      });
      await loadSelectedNetwork(selectedNetworkId.value);
      await loadAudits();
      networkTab.value = 'sync';
      message.success(`${result.provider} 成员同步完成。`);
    } catch (error) {
      handleError(error, '同步成员失败');
    }
  }

  async function syncConfig() {
    if (!selectedNetworkId.value) {
      message.warning('请先选择网络。');
      return;
    }
    try {
      const result = await request<ConfigSyncResult>(`/api/networks/${selectedNetworkId.value}/config/sync`, {
        method: 'POST'
      });
      await loadSelectedNetwork(selectedNetworkId.value);
      await loadAudits();
      networkTab.value = 'sync';
      message.success(`${result.provider} 配置同步完成。`);
    } catch (error) {
      handleError(error, '同步配置失败');
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
      message.success(`${member.name || member.providerMemberId} 已更新授权状态。`);
    } catch (error) {
      handleError(error, '更新成员授权失败');
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
      message.success(`${member.name || member.providerMemberId} 的虚拟 IP 已保存。`);
    } catch (error) {
      handleError(error, '保存虚拟 IP 失败');
    }
  }

  async function loadAudits() {
    try {
      audits.value = await request<AuditLog[]>('/api/audit-logs');
    } catch (error) {
      handleError(error, '读取审计日志失败');
    }
  }

  function generateAccessArtifact() {
    inviteVersion.value = Date.now();
    message.info('邀请码和预览二维码已刷新。plant 文件下载仍然走真实后端接口。');
  }

  async function copyAccessCode() {
    try {
      await navigator.clipboard.writeText(accessCode.value);
      message.success('邀请码已复制。');
    } catch {
      message.warning('当前环境不支持自动复制，请手动复制邀请码。');
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
    message.error(`${fallback}：${friendlyError(error)}`);
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
