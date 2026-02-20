import type { MonitoringSummary, NodeStatus, StatusEvent } from './types';

export type MonitoringApiClientOptions = {
  baseUrl: string;
  apiKey?: string;
};

export class MonitoringApiClient {
  private readonly baseUrl: string;
  private readonly apiKey?: string;

  constructor(options: MonitoringApiClientOptions) {
    this.baseUrl = options.baseUrl.replace(/\/+$/, '');
    this.apiKey = options.apiKey;
  }

  async getSummary(): Promise<MonitoringSummary> {
    return this.getJson<MonitoringSummary>('/api/query/summary');
  }

  async getNodes(): Promise<NodeStatus[]> {
    return this.getJson<NodeStatus[]>('/api/query/nodes');
  }

  async getNodeEvents(nodeName: string, take = 100): Promise<StatusEvent[]> {
    const encoded = encodeURIComponent(nodeName);
    return this.getJson<StatusEvent[]>(`/api/query/nodes/${encoded}/events?take=${take}`);
  }

  private async getJson<T>(path: string): Promise<T> {
    const headers: Record<string, string> = {};
    if (this.apiKey && this.apiKey.trim().length > 0) {
      headers['X-Api-Key'] = this.apiKey;
    }

    const response = await fetch(`${this.baseUrl}${path}`, { headers });
    if (!response.ok) {
      const body = await response.text();
      throw new Error(`Request failed (${response.status}): ${body || response.statusText}`);
    }

    return await response.json() as T;
  }
}
