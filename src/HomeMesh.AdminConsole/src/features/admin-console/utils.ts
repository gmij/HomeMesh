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
    return '暂无';
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
    return error.message || `接口返回 ${error.status}`;
  }

  if (error instanceof Error) {
    return error.message;
  }

  return '发生未知错误。';
}

export function buildPseudoQrMatrix(seed: string) {
  const size = 21;
  const values: boolean[] = [];

  for (let y = 0; y < size; y += 1) {
    for (let x = 0; x < size; x += 1) {
      const finder = isFinderPattern(x, y, size);
      if (finder !== null) {
        values.push(finder);
        continue;
      }

      const source = seed.charCodeAt((x * 7 + y * 13) % seed.length) || 0;
      values.push(((source + x * 3 + y * 5) % 7) < 3);
    }
  }

  return values;
}

function isFinderPattern(x: number, y: number, size: number) {
  const zones = [
    [0, 0],
    [size - 7, 0],
    [0, size - 7]
  ];

  for (const [left, top] of zones) {
    if (x >= left && x < left + 7 && y >= top && y < top + 7) {
      const dx = x - left;
      const dy = y - top;
      const border = dx === 0 || dx === 6 || dy === 0 || dy === 6;
      const inner = dx >= 2 && dx <= 4 && dy >= 2 && dy <= 4;
      return border || inner;
    }
  }

  return null;
}
