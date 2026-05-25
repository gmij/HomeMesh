export interface AuthUser {
  username: string;
  role: string;
}

export interface AuthStatusResponse {
  initialized: boolean;
  authenticated: boolean;
  user?: AuthUser;
}

export interface ProviderStatus {
  providerName: string;
  status: string;
  message?: string | null;
  checkedAt?: string;
}

export interface ZeroTierConfig {
  enabled: boolean;
  port: number;
  authTokenPath: string;
}

export interface ZeroTierConfigSaveResult {
  config: ZeroTierConfig;
  restartRequired: boolean;
  message: string;
}

export interface ZeroTierTestResult {
  status: string;
  message: string;
  detail?: string;
  checkedAt?: string;
  online?: boolean;
  nodeAddress?: string;
  version?: string;
}

export interface ZeroTierTokenUploadResult {
  authTokenPath: string;
}

export interface DashboardSummary {
  networkCount: number;
  memberCount: number;
  authorizedMemberCount: number;
  onlineMemberCount: number;
  routeCount: number;
  ipPoolCount: number;
  errorSyncCount: number;
  lastAuditAt?: string | null;
}

export interface NetworkSummary {
  id: string;
  homeId: string;
  name: string;
  cidr?: string | null;
  private: boolean;
  v4AssignMode: boolean;
  autoApproveMembers: boolean;
  status: string;
  createdAt: string;
  updatedAt: string;
}

export interface NetworkProviderBinding {
  provider: string;
  providerNetworkId: string;
  isPrimary: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface NetworkDetail extends NetworkSummary {
  memberCount: number;
  onlineMemberCount: number;
  routeCount: number;
  gatewayCount: number;
  providerBindings: NetworkProviderBinding[];
}

export interface Member {
  id: string;
  networkId: string;
  deviceId?: string | null;
  provider: string;
  providerMemberId: string;
  name?: string | null;
  role: string;
  authorized: boolean;
  activeBridge: boolean;
  online: boolean;
  ipAssignmentsJson?: string | null;
  tagsJson?: string | null;
  lastSeenAt?: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface RouteItem {
  id: string;
  networkId: string;
  type: string;
  target: string;
  via?: string | null;
  enabled: boolean;
  providerManaged: boolean;
}

export interface IpPoolItem {
  id: string;
  networkId: string;
  ipRangeStart: string;
  ipRangeEnd: string;
  providerManaged: boolean;
}

export interface DnsConfig {
  id: string;
  networkId: string;
  domain?: string | null;
  servers: string[];
  providerManaged: boolean;
}

export interface SyncState {
  id: string;
  provider: string;
  resourceType: string;
  resourceId: string;
  status: string;
  lastError?: string | null;
  lastSyncAt?: string | null;
}

export interface AuditLog {
  id: string;
  type: string;
  actor?: string | null;
  message: string;
  createdAt: string;
}

export interface MemberSyncResult {
  networkId: string;
  provider: string;
  providerNetworkId: string;
  syncedMemberCount: number;
  autoApprovedMemberCount: number;
  status: string;
  error?: string | null;
  syncedAt: string;
}

export interface ConfigSyncResult {
  networkId: string;
  provider: string;
  providerNetworkId: string;
  routeCount: number;
  ipPoolCount: number;
  hasDnsConfig: boolean;
  status: string;
  error?: string | null;
  syncedAt: string;
  updatedProviderNetworkId?: string | null;
}

export interface AccessArtifacts {
  networkId: string;
  expiryDays: number;
  expiresAt: string;
  planetUrl: string;
  moonUrl: string;
}
