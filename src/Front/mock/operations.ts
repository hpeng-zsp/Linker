import { defineFakeRoute } from "vite-plugin-fake-server/client";

const serviceDiscovery = [
  {
    id: "svc-user",
    name: "gateway-user",
    namespace: "production",
    status: "open",
    instanceCount: 3,
    description: "用户中心网关",
    microservices: ["user-service", "account-service", "profile-service"],
    lastHeartbeat: "2026-10-23 09:12:10"
  },
  {
    id: "svc-order",
    name: "gateway-order",
    namespace: "production",
    status: "open",
    instanceCount: 5,
    description: "订单中心网关",
    microservices: ["order-service", "payment-service", "shipping-service"],
    lastHeartbeat: "2026-10-23 09:11:21"
  }
];

const configGroups = [
  { id: "default", name: "DEFAULT_GROUP", description: "默认公共配置" },
  { id: "order", name: "ORDER_GROUP", description: "订单域配置" },
  { id: "user", name: "USER_GROUP", description: "用户域配置" }
];

const configItems = [
  {
    id: "cfg-001",
    groupId: "default",
    key: "system.redis.pool.max",
    value: { max: 200, min: 20, timeout: 3000 },
    updatedAt: "2026-10-22 23:00:11",
    status: "normal"
  },
  {
    id: "cfg-002",
    groupId: "order",
    key: "order.pay.callbackUrl",
    value: { url: "https://pay.example.com/callback", enabled: true, retries: 3 },
    updatedAt: "2026-10-23 08:24:55",
    status: "warning"
  }
];

const scheduledTasks = [
  {
    id: "job-001",
    name: "sync-user-cache",
    serviceName: "user-service",
    cron: "0 0/5 * * * ?",
    status: "open",
    executor: "executor-user-01",
    lastRun: "2026-10-23 09:00:00"
  },
  {
    id: "job-002",
    name: "sync-order-status",
    serviceName: "order-service",
    cron: "0 0/15 * * * ?",
    status: "open",
    executor: "executor-order-03",
    lastRun: "2026-10-23 08:45:00"
  }
];

export default defineFakeRoute([
  {
    url: "/api/service-discovery",
    method: "get",
    response: () => ({ success: true, data: serviceDiscovery })
  },
  {
    url: "/api/config-groups",
    method: "get",
    response: () => ({ success: true, data: configGroups })
  },
  {
    url: "/api/config-items",
    method: "get",
    response: () => ({ success: true, data: configItems })
  },
  {
    url: "/api/scheduled-tasks",
    method: "get",
    response: () => ({ success: true, data: scheduledTasks })
  },
  {
    url: "/api/config-items/publish",
    method: "post",
    response: ({ body }) => ({ success: true, data: body })
  }
]);
