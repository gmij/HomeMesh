import { ApiError } from '../../api/client';

export function parseIpAssignments(payload?: string | null) {
  if (!payload) {
    return [] as string[];
  }

  try {
    const parsed = JSON.parse(payload) as string[];
    return Array.isArray(parsed) ? parsed : [];
  } catch {
    return [];
  }
}

export function parseTags(payload?: string | null) {
  if (!payload) {
    return [] as string[];
  }

  try {
    const parsed = JSON.parse(payload) as string[];
    return Array.isArray(parsed) ? parsed : [];
  } catch {
    return [];
  }
}

export function parseCsv(value: string) {
  return value
    .split(',')
    .map((item) => item.trim())
    .filter(Boolean);
}

export function formatTime(value?: string | null, mode: 'full' | 'time' = 'full') {
  if (!value) {
    return 'N/A';
  }

  const date = new Date(value);
  return new Intl.DateTimeFormat('zh-CN', {
    month: mode === 'full' ? '2-digit' : undefined,
    day: mode === 'full' ? '2-digit' : undefined,
    hour: '2-digit',
    minute: '2-digit',
    second: mode === 'time' ? undefined : '2-digit'
  }).format(date);
}

export function statusColor(status?: string) {
  if (!status) {
    return 'default';
  }

  const normalized = status.toLowerCase();
  if (['healthy', 'ok', 'ready', 'created', 'connected', 'online', 'success'].includes(normalized)) {
    return 'green';
  }

  if (['warning', 'planned', 'pending'].includes(normalized)) {
    return 'orange';
  }

  if (['error', 'failed', 'offline'].includes(normalized)) {
    return 'red';
  }

  return 'blue';
}

export function statusAlertType(status?: string): 'success' | 'warning' | 'error' | 'info' {
  if (!status) {
    return 'info';
  }

  const normalized = status.toLowerCase();
  if (['healthy', 'ok', 'ready', 'connected', 'success'].includes(normalized)) {
    return 'success';
  }
  if (['warning', 'planned', 'pending'].includes(normalized)) {
    return 'warning';
  }
  if (['error', 'failed', 'offline'].includes(normalized)) {
    return 'error';
  }
  return 'info';
}

export function isHealthyStatus(status?: string) {
  return ['healthy', 'ok', 'ready', 'connected', 'success'].includes((status || '').toLowerCase());
}

export function friendlyError(error: unknown) {
  if (error instanceof ApiError) {
    return error.message || `API returned ${error.status}`;
  }

  if (error instanceof Error) {
    return error.message;
  }

  return 'Unknown error occurred.';
}
