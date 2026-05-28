export class ApiError extends Error {
  status: number;

  constructor(message: string, status: number) {
    super(message);
    this.name = 'ApiError';
    this.status = status;
  }
}

export function logClientError(context: string, error: unknown) {
  console.warn(`[HomeMesh] ${context}`, error);
}

export async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(path, {
    credentials: 'same-origin',
    headers: {
      'Content-Type': 'application/json',
      ...(init?.headers ?? {})
    },
    ...init
  });

  if (!response.ok) {
    throw new ApiError(await readError(response), response.status);
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return (await response.json()) as T;
}

export async function requestMaybe<T>(path: string): Promise<T | null> {
  const response = await fetch(path, {
    credentials: 'same-origin',
    headers: {
      'Content-Type': 'application/json'
    }
  });

  if (response.status === 404) {
    return null;
  }

  if (!response.ok) {
    throw new ApiError(await readError(response), response.status);
  }

  if (response.status === 204) {
    return null;
  }

  return (await response.json()) as T;
}

async function readError(response: Response): Promise<string> {
  const text = await response.text();
  const fallbackMessage = `Request failed with status ${response.status}.`;

  if (!text) {
    return fallbackMessage;
  }

  try {
    const payload = JSON.parse(text) as { error?: string; message?: string; detail?: string };
    const primary = payload.error ?? payload.message;

    if (payload.detail && payload.detail !== '{}') {
      logClientError('Server returned hidden error detail.', payload.detail);
    }

    return primary || fallbackMessage;
  } catch (error) {
    logClientError('Failed to parse API error payload.', error);
    logClientError('Received non-JSON error response body.', text);
    return fallbackMessage;
  }
}
