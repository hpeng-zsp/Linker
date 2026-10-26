import { http } from "@/utils/http";

export type ServiceInstance = {
  id: string;
  ip: string;
  port: number;
  status: "open" | "closed";
  health: "healthy" | "warning";
  lastHeartbeat: string;
};

export type ServiceDiscoveryItem = {
  id: string;
  name: string;
  namespace: string;
  status: "open" | "closed";
  instanceCount: number;
  description: string;
  microservices: string[];
  instances: ServiceInstance[];
  lastHeartbeat: string;
};

const FIVE_MINUTES_MS = 5 * 60 * 1000;

export const getServiceInstanceStat = (instances: ServiceInstance[] = []) => {
  const validInstances = instances.filter(instance => {
    if (instance.status === "open") return true;
    if (!instance.lastHeartbeat) return false;

    const ms = Date.now() - new Date(instance.lastHeartbeat.replace(" ", "T")).getTime();
    return ms <= FIVE_MINUTES_MS;
  });

  const activeCount = validInstances.filter(instance => instance.status === "open").length;
  const totalCount = validInstances.length;

  return {
    activeCount,
    totalCount,
    display: `${activeCount}/${totalCount}`
  };
};

export type ConfigGroup = {
  id: string;
  name: string;
  description: string;
};

export type ConfigItem = {
  id: string;
  groupId: string;
  key: string;
  value: Record<string, any>;
  updatedAt: string;
  publishedAt: string;
  status: "normal" | "warning";
};

export type ScheduledTaskLog = {
  time: string;
  status: "success" | "failed";
  message: string;
};

export type ScheduledTaskItem = {
  id: string;
  name: string;
  serviceName: string;
  cron: string;
  status: "open" | "closed";
  executionMode: "single" | "broadcast" | "sharding";
  lastRun: string;
  nextRun: string;
  params: string;
  logs: ScheduledTaskLog[];
};

const mockMode = true;
const cloneMock = <T>(value: T): T => JSON.parse(JSON.stringify(value));

const mockServiceDiscovery: ServiceDiscoveryItem[] = [
  {
    id: "svc-user",
    name: "user-services",
    namespace: "production",
    status: "open",
    instanceCount: 3,
    description: "用户中心服务",
    microservices: ["user-service", "account-service", "profile-service"],
    instances: [
      {
        id: "inst-user-01",
        ip: "10.10.10.11",
        port: 8081,
        status: "open",
        health: "healthy",
        lastHeartbeat: "2026-10-23 09:12:10"
      },
      {
        id: "inst-user-02",
        ip: "10.10.10.12",
        port: 8082,
        status: "open",
        health: "healthy",
        lastHeartbeat: "2026-10-23 09:09:40"
      },
      {
        id: "inst-user-03",
        ip: "10.10.10.13",
        port: 8083,
        status: "closed",
        health: "warning",
        lastHeartbeat: "2026-10-23 08:55:15"
      }
    ],
    lastHeartbeat: "2026-10-23 09:12:10"
  },
  {
    id: "svc-order",
    name: "order-services",
    namespace: "production",
    status: "open",
    instanceCount: 5,
    description: "订单中心服务",
    microservices: ["order-service", "payment-service", "shipping-service"],
    instances: [
      {
        id: "inst-order-01",
        ip: "10.10.20.11",
        port: 8091,
        status: "open",
        health: "healthy",
        lastHeartbeat: "2026-10-23 09:11:21"
      },
      {
        id: "inst-order-02",
        ip: "10.10.20.12",
        port: 8092,
        status: "open",
        health: "healthy",
        lastHeartbeat: "2026-10-23 09:08:51"
      },
      {
        id: "inst-order-03",
        ip: "10.10.20.13",
        port: 8093,
        status: "open",
        health: "warning",
        lastHeartbeat: "2026-10-23 08:57:12"
      },
      {
        id: "inst-order-04",
        ip: "10.10.20.14",
        port: 8094,
        status: "open",
        health: "healthy",
        lastHeartbeat: "2026-10-23 09:06:42"
      },
      {
        id: "inst-order-05",
        ip: "10.10.20.15",
        port: 8095,
        status: "closed",
        health: "warning",
        lastHeartbeat: "2026-10-23 08:40:01"
      }
    ],
    lastHeartbeat: "2026-10-23 09:11:21"
  },
  {
    id: "svc-report",
    name: "report-services",
    namespace: "staging",
    status: "closed",
    instanceCount: 2,
    description: "报表中心服务",
    microservices: ["report-service", "etl-service"],
    instances: [
      {
        id: "inst-report-01",
        ip: "10.10.30.11",
        port: 8101,
        status: "closed",
        health: "warning",
        lastHeartbeat: "2026-10-23 08:50:31"
      },
      {
        id: "inst-report-02",
        ip: "10.10.30.12",
        port: 8102,
        status: "closed",
        health: "warning",
        lastHeartbeat: "2026-10-23 08:32:10"
      }
    ],
    lastHeartbeat: "2026-10-23 08:50:31"
  }
];

const mockConfigGroups: ConfigGroup[] = [
  {
    id: "default",
    name: "DEFAULT_GROUP",
    description: "默认公共配置"
  },
  {
    id: "order",
    name: "ORDER_GROUP",
    description: "订单域配置"
  },
  {
    id: "user",
    name: "USER_GROUP",
    description: "用户域配置"
  }
];

const mockConfigItems: ConfigItem[] = [
  {
    id: "cfg-001",
    groupId: "default",
    key: "system.redis.pool.max",
    value: {
      max: 200,
      min: 20,
      timeout: 3000
    },
    updatedAt: "2026-10-22 23:00:11",
    publishedAt: "2026-10-22 23:15:02",
    status: "normal"
  },
  {
    id: "cfg-002",
    groupId: "order",
    key: "order.pay.callbackUrl",
    value: {
      url: "https://pay.example.com/callback",
      enabled: true,
      retries: 3
    },
    updatedAt: "2026-10-23 08:24:55",
    publishedAt: "2026-10-23 08:36:10",
    status: "warning"
  },
  {
    id: "cfg-003",
    groupId: "user",
    key: "user.login.limit",
    value: {
      maxAttempts: 5,
      lockMinutes: 15,
      captchaEnabled: true
    },
    updatedAt: "2026-10-23 09:02:01",
    publishedAt: "2026-10-23 09:12:18",
    status: "normal"
  }
];

const mockScheduledTasks: ScheduledTaskItem[] = [
  {
    id: "job-001",
    name: "sync-user-cache",
    serviceName: "user-service",
    cron: "0 0/5 * * * ?",
    status: "open",
    executionMode: "single",
    lastRun: "2026-10-23 09:00:00",
    nextRun: "2026-10-23 09:05:00",
    params: "{\"tenant\":\"default\",\"force\":false}",
    logs: [
      { time: "2026-10-23 09:00:00", status: "success", message: "用户缓存同步完成，共 1284 条数据更新" },
      { time: "2026-10-23 08:55:00", status: "success", message: "增量同步已完成" }
    ]
  },
  {
    id: "job-002",
    name: "sync-order-status",
    serviceName: "order-service",
    cron: "0 0/15 * * * ?",
    status: "open",
    executionMode: "broadcast",
    lastRun: "2026-10-23 08:45:00",
    nextRun: "2026-10-23 09:00:00",
    params: "{\"window\":\"15m\",\"retry\":3}",
    logs: [
      { time: "2026-10-23 08:45:00", status: "success", message: "订单状态同步成功" },
      { time: "2026-10-23 08:30:00", status: "failed", message: "回调超时，已触发重试" }
    ]
  },
  {
    id: "job-003",
    name: "retry-payment-close",
    serviceName: "payment-service",
    cron: "0 0/30 * * * ?",
    status: "closed",
    executionMode: "sharding",
    lastRun: "2026-10-23 08:10:00",
    nextRun: "2026-10-23 08:40:00",
    params: "{\"limit\":50,\"mode\":\"retry\"}",
    logs: [
      { time: "2026-10-23 08:10:00", status: "failed", message: "支付关闭重试失败，等待人工确认" }
    ]
  }
];

export const fetchServiceDiscovery = async (): Promise<ServiceDiscoveryItem[]> => {
  if (mockMode) {
    return Promise.resolve(cloneMock(mockServiceDiscovery));
  }
  return http.request<ServiceDiscoveryItem[]>("get", "/api/service-discovery");
};

export const fetchServiceGroups = async (): Promise<ConfigGroup[]> => {
  if (mockMode) {
    return Promise.resolve(cloneMock(mockConfigGroups));
  }
  return http.request<ConfigGroup[]>("get", "/api/config-groups");
};

export const fetchConfigItems = async (): Promise<ConfigItem[]> => {
  if (mockMode) {
    return Promise.resolve(cloneMock(mockConfigItems));
  }
  return http.request<ConfigItem[]>("get", "/api/config-items");
};

export const fetchScheduledTasks = async (): Promise<ScheduledTaskItem[]> => {
  if (mockMode) {
    return Promise.resolve(cloneMock(mockScheduledTasks));
  }
  return http.request<ScheduledTaskItem[]>("get", "/api/scheduled-tasks");
};

export const publishConfig = async (payload: ConfigItem) => {
  if (mockMode) {
    return Promise.resolve({
      success: true,
      data: payload
    });
  }
  return http.request("post", "/api/config-items/publish", { data: payload });
};
