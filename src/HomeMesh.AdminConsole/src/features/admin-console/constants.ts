import type { PlaceholderTabKey, SectionNavItem } from './types';

export const prototypeSections = {
  dashboard: 'dashboard',
  network: 'network',
  access: 'access',
  providers: 'providers'
} as const;

export const prototypeNavItems: SectionNavItem[] = [
  { key: prototypeSections.dashboard, label: '总览', index: 1 },
  { key: prototypeSections.network, label: '家庭网络', index: 2 },
  { key: prototypeSections.access, label: '设备接入', index: 3 },
  { key: prototypeSections.providers, label: '协议 Provider', index: 4 }
];

export const sectionPathMap: Record<SectionNavItem['key'], string> = {
  dashboard: '/dashboard',
  network: '/network',
  access: '/access',
  providers: '/providers'
};

export const placeholderTitles: Record<PlaceholderTabKey, string> = {
  gateway: '网关节点',
  acl: '访问控制',
  protocol: '协议配置'
};

export const accessExpiryOptions = [
  { label: '1 天', value: '1' },
  { label: '7 天', value: '7' },
  { label: '30 天', value: '30' }
];
