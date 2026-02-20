<script setup lang="ts">
import { computed, onMounted, ref } from 'vue';
import { MonitoringApiClient } from './api';
import type { MonitoringSummary, NodeStatus, StatusEvent } from './types';

const envBaseUrl = (import.meta.env.VITE_MONITORING_API_BASE_URL as string | undefined) ?? 'http://localhost:5000';
const envApiKey = (import.meta.env.VITE_MONITORING_API_KEY as string | undefined) ?? '';

const baseUrl = ref(envBaseUrl);
const apiKey = ref(envApiKey);
const autoRefreshSeconds = ref(15);
const loading = ref(false);
const error = ref('');

const summary = ref<MonitoringSummary | null>(null);
const nodes = ref<NodeStatus[]>([]);
const selectedNode = ref('');
const nodeEvents = ref<StatusEvent[]>([]);

let timer: number | undefined;

const selectedNodeDetails = computed(() => nodes.value.find(n => n.nodeName === selectedNode.value) ?? null);

function getClient() {
  return new MonitoringApiClient({ baseUrl: baseUrl.value, apiKey: apiKey.value });
}

async function loadDashboard() {
  loading.value = true;
  error.value = '';
  try {
    const client = getClient();
    const [summaryData, nodesData] = await Promise.all([client.getSummary(), client.getNodes()]);
    summary.value = summaryData;
    nodes.value = nodesData;

    if (!selectedNode.value && nodesData.length > 0) {
      selectedNode.value = nodesData[0]?.nodeName ?? '';
    }

    if (selectedNode.value) {
      nodeEvents.value = await client.getNodeEvents(selectedNode.value, 100);
    } else {
      nodeEvents.value = [];
    }
  } catch (e) {
    error.value = e instanceof Error ? e.message : String(e);
  } finally {
    loading.value = false;
  }
}

function startAutoRefresh() {
  stopAutoRefresh();
  const intervalMs = Math.max(5, autoRefreshSeconds.value) * 1000;
  timer = window.setInterval(() => {
    void loadDashboard();
  }, intervalMs);
}

function stopAutoRefresh() {
  if (timer !== undefined) {
    window.clearInterval(timer);
    timer = undefined;
  }
}

async function onNodeChanged() {
  if (!selectedNode.value) {
    nodeEvents.value = [];
    return;
  }

  try {
    const client = getClient();
    nodeEvents.value = await client.getNodeEvents(selectedNode.value, 100);
  } catch (e) {
    error.value = e instanceof Error ? e.message : String(e);
  }
}

async function reconnect() {
  await loadDashboard();
  startAutoRefresh();
}

onMounted(async () => {
  await loadDashboard();
  startAutoRefresh();
});
</script>

<template>
  <main class="page">
    <header class="header">
      <h1>WinBus Remote Monitoring</h1>
      <p>Fleet-level status visibility for optimization utilities running on remote systems.</p>
    </header>

    <section class="controls">
      <label>
        API Base URL
        <input v-model="baseUrl" placeholder="http://localhost:5000" />
      </label>
      <label>
        API Key
        <input v-model="apiKey" type="password" placeholder="Optional X-Api-Key" />
      </label>
      <label>
        Auto Refresh (sec)
        <input v-model.number="autoRefreshSeconds" type="number" min="5" />
      </label>
      <button @click="reconnect">Connect / Refresh</button>
    </section>

    <section v-if="error" class="error">
      {{ error }}
    </section>

    <section class="cards">
      <article class="card">
        <h3>Total Nodes</h3>
        <div class="value">{{ summary?.totalNodes ?? '-' }}</div>
      </article>
      <article class="card">
        <h3>Active Nodes</h3>
        <div class="value">{{ summary?.activeNodes ?? '-' }}</div>
      </article>
      <article class="card alert">
        <h3>Alerts (1h)</h3>
        <div class="value">{{ summary?.alertsLastHour ?? '-' }}</div>
      </article>
      <article class="card fail">
        <h3>Failures (1h)</h3>
        <div class="value">{{ summary?.failuresLastHour ?? '-' }}</div>
      </article>
    </section>

    <section class="grid">
      <article class="panel">
        <h2>Nodes</h2>
        <div class="table-wrap">
          <table>
            <thead>
              <tr>
                <th>Node</th>
                <th>Fleet</th>
                <th>Status</th>
                <th>Module</th>
                <th>Last Seen (UTC)</th>
              </tr>
            </thead>
            <tbody>
              <tr
                v-for="node in nodes"
                :key="node.nodeName"
                :class="{ selected: selectedNode === node.nodeName }"
                @click="selectedNode = node.nodeName; onNodeChanged()"
              >
                <td>{{ node.nodeName }}</td>
                <td>{{ node.fleet }}</td>
                <td>{{ node.lastStatus }}</td>
                <td>{{ node.lastModule }}</td>
                <td>{{ node.lastSeen }}</td>
              </tr>
            </tbody>
          </table>
        </div>
      </article>

      <article class="panel">
        <h2>Node Events</h2>
        <p v-if="selectedNodeDetails" class="subtitle">
          {{ selectedNodeDetails.nodeName }} · {{ selectedNodeDetails.machine }} · {{ selectedNodeDetails.lastMessage }}
        </p>
        <div class="events">
          <div v-for="event in nodeEvents" :key="`${event.timestamp}-${event.eventType}-${event.module}`" class="event">
            <div class="event-time">{{ event.timestamp }}</div>
            <div class="event-body">
              <strong>{{ event.status }}</strong>
              <span>{{ event.eventType }} / {{ event.module }}</span>
              <p>{{ event.message }}</p>
            </div>
          </div>
        </div>
      </article>
    </section>

    <footer class="footer">
      <span>Status: {{ loading ? 'Refreshing…' : 'Up to date' }}</span>
      <button @click="stopAutoRefresh">Pause Auto Refresh</button>
      <button @click="startAutoRefresh">Resume Auto Refresh</button>
    </footer>
  </main>
</template>
