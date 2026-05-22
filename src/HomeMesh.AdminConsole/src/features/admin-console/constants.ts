import type { PlaceholderTabKey, SectionNavItem } from './types';

export const prototypeSections = {
  dashboard: 'dashboard',
  network: 'network',
  access: 'access',
  providers: 'providers'
} as const;

// Navigation items now use i18n keys instead of hard-coded labels
// Update the labels in component using useI18n() composable
export const prototypeNavItems: SectionNavItem[] = [
  { key: prototypeSections.dashboard, label: 'nav.dashboard', index: 1 },
  { key: prototypeSections.network, label: 'nav.network', index: 2 },
  { key: prototypeSections.access, label: 'nav.access', index: 3 },
  { key: prototypeSections.providers, label: 'nav.providers', index: 4 }
];

export const sectionPathMap: Record<SectionNavItem['key'], string> = {
  dashboard: '/dashboard',
  network: '/network',
  access: '/access',
  providers: '/providers'
};

// Placeholder titles now use i18n keys
export const placeholderTitles: Record<PlaceholderTabKey, string> = {
  gateway: 'network.tabs.gateway',
  acl: 'network.tabs.acl',
  protocol: 'network.tabs.protocol'
};

// Access expiry options now use i18n keys
export const accessExpiryOptions = [
  { label: 'access.expiry_1d', value: '1' },
  { label: 'access.expiry_7d', value: '7' },
  { label: 'access.expiry_30d', value: '30' }
];
