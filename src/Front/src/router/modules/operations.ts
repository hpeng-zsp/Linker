const Layout = () => import("@/layout/index.vue");

export default [
  {
    path: "/service-discovery",
    name: "ServiceDiscovery",
    component: () => import("@/views/ops/service-discovery/index.vue"),
    meta: {
      title: "服务注册发现",
      icon: "ep:connection",
      rank: 8,
      showLink: true
    }
  },
  {
    path: "/config-center",
    name: "ConfigCenter",
    component: () => import("@/views/ops/config-center/index.vue"),
    meta: {
      title: "配置文件中心",
      icon: "ep:files",
      rank: 9,
      showLink: true
    }
  },
  {
    path: "/scheduled-tasks",
    name: "ScheduledTasks",
    component: () => import("@/views/ops/scheduled-tasks/index.vue"),
    meta: {
      title: "定时任务",
      icon: "ep:timer",
      rank: 10,
      showLink: true
    }
  }
] satisfies Array<RouteConfigsTable>;
