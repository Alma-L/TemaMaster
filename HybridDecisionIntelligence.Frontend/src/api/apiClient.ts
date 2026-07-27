const rawBaseUrl = process.env.REACT_APP_API_URL;

export const API_BASE_URL = rawBaseUrl
  ? rawBaseUrl.replace(/\/+$/, '')
  : '/api';

export interface ApiRequestOptions extends RequestInit {
  parseJson?: boolean;
}

export async function apiFetch<T = any>(
  path: string,
  options: ApiRequestOptions = {}
): Promise<T> {
  const normalizedPath = path.replace(/^\/+/, '');
  const url = path.startsWith('http') ? path : `${API_BASE_URL}/${normalizedPath}`;

  const headers = {
    'Content-Type': 'application/json',
    ...(options.headers || {}),
  } as Record<string, string>;

  const response = await fetch(url, {
    ...options,
    headers,
  });

  if (!response.ok) {
    const body = await response.text();
    throw new Error(
      `API request failed: ${response.status} ${response.statusText} - ${body}`
    );
  }

  if (options.parseJson === false || response.status === 204) {
    return undefined as unknown as T;
  }

  return (await response.json()) as T;
}
