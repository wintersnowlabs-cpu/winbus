export type MonitoringSummary = {
  totalNodes: number;
  activeNodes: number;
  alertsLastHour: number;
  failuresLastHour: number;
  generatedAt: string;
};

export type NodeStatus = {
  nodeName: string;
  machine: string;
  fleet: string;
  lastStatus: string;
  lastEventType: string;
  lastModule: string;
  lastSeen: string;
  lastMessage: string;
};

export type StatusEvent = {
  timestamp: string;
  machine: string;
  user: string;
  fleet: string;
  nodeName: string;
  eventType: string;
  module: string;
  status: string;
  message: string;
};
