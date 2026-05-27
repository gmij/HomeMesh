import type { Component } from 'vue';

export type AuthMode = 'loading' | 'setup' | 'login' | 'ready';

export type PrototypeSectionKey = 'dashboard' | 'network' | 'access' | 'providers';

export type NetworkTabKey =
  | 'overview'
  | 'members'
  | 'routes'
  | 'dns'
  | 'gateway'
  | 'acl'
  | 'protocol';

export type PlaceholderTabKey = 'gateway' | 'acl' | 'protocol';

export interface HealthStatus {
  status: string;
  service: string;
  checkedAt: string;
}

export interface SectionNavItem {
  key: PrototypeSectionKey;
  label: string;
  index: number;
}

export interface SummaryMetric {
  label: string;
  value: string | number;
  meta: string;
  icon: Component;
  tone: 'blue' | 'cyan' | 'violet' | 'purple';
}

export interface DashboardStatusItem {
  key: string;
  label: string;
  value: string;
  tone: 'blue' | 'cyan' | 'violet' | 'purple';
  note: string;
}

export interface DetailMetric {
  label: string;
  value: string | number;
  meta: string;
}

export interface ProviderCardModel {
  key: string;
  badge: string;
  title: string;
  stage: string;
  description: string;
  status: string;
  controlPlane: string;
  networkId: string;
  actionLabel: string;
  actionType: 'default' | 'primary';
  disabled: boolean;
  tone: 'blue' | 'gold' | 'purple';
  tagColor: string;
}

export interface AuthFormState {
  username: string;
  password: string;
}

export interface CreateNetworkFormState {
  name: string;
  provider: string;
  cidr: string;
  private: boolean;
  autoApproveMembers: boolean;
}

export interface EasySetupFormState {
  cidr: string;
  ipPoolStart: string;
  ipPoolEnd: string;
  enableAutoAssign: boolean;
  autoApproveMembers: boolean;
  dnsDomain: string;
  dnsServers: string;
}

export interface RouteFormState {
  target: string;
  via: string;
  type: string;
  enabled: boolean;
  providerManaged: boolean;
}

export interface PoolFormState {
  ipRangeStart: string;
  ipRangeEnd: string;
  providerManaged: boolean;
}

export interface DnsFormState {
  domain: string;
  servers: string;
  providerManaged: boolean;
}

export interface AccessFormState {
  expiryDays: string;
}
