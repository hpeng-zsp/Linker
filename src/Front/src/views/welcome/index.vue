<script setup lang="ts">
import { computed, onMounted, ref } from "vue";
import {
  fetchConfigItems,
  fetchScheduledTasks,
  fetchServiceDiscovery,
  getServiceInstanceStat
} from "@/api/operations";
import { locale, setLocale } from "@/i18n";

defineOptions({
  name: "Welcome"
});

const services = ref<any[]>([]);
const configs = ref<any[]>([]);
const tasks = ref<any[]>([]);
const loading = ref(true);

const copy = computed(() => {
  if (locale.value === "en") {
    return {
      title: "Service governance center",
      subtitle:
        "One platform for service discovery, config management, and scheduled jobs.",
      primaryAction: "Open service discovery",
      secondaryAction: "View scheduled tasks",
      overview: "Overview",
      serviceDiscovery: "Service discovery",
      configCenter: "Config center",
      raftCluster: "Raft cluster",
      scheduledTasks: "Scheduled tasks",
      operationSummary: "Operational summary",
      recentTasks: "Recent tasks",
      health: "Health status",
      servicesLabel: "Services",
      configLabel: "Configs",
      taskLabel: "Jobs",
      instancesLabel: "Instances",
      online: "online",
      today: "today",
      healthy: "healthy",
      language: "English",
      Chinese: "中文",
      systemStatus: "Current system status",
      metrics: "Runtime metrics",
      quickLink: "Quick access",
      alertTitle: "Platform alerts",
      noData: "No data",
      serviceBadge: "registry",
      configBadge: "config",
      taskBadge: "job"
    };
  }

  return {
    title: "服务治理中心",
    subtitle: "统一管理服务注册发现、配置文件和定时任务的运维中心。",
    primaryAction: "查看服务发现",
    secondaryAction: "查看定时任务",
    overview: "总览",
    serviceDiscovery: "服务发现",
    configCenter: "配置中心",
    raftCluster: "Raft 集群",
    scheduledTasks: "定时任务",
    operationSummary: "运维概览",
    recentTasks: "最近任务",
    health: "健康状态",
    servicesLabel: "服务",
    configLabel: "配置",
    taskLabel: "任务",
    instancesLabel: "实例",
    online: "在线",
    today: "今日",
    healthy: "健康",
    language: "中文",
    Chinese: "English",
    systemStatus: "当前系统状态",
    metrics: "运行指标",
    quickLink: "快捷入口",
    alertTitle: "平台告警",
    noData: "暂无数据",
    serviceBadge: "注册中心",
    configBadge: "配置",
    taskBadge: "任务"
  };
});

const totalServiceCount = computed(() => services.value.length);
const runningServiceCount = computed(
  () => services.value.filter(item => item.status === "open").length
);
const totalInstanceCount = computed(() =>
  services.value.reduce(
    (sum, item) => sum + getServiceInstanceStat(item.instances).totalCount,
    0
  )
);
const activeInstanceCount = computed(() =>
  services.value.reduce(
    (sum, item) => sum + getServiceInstanceStat(item.instances).activeCount,
    0
  )
);
const configCount = computed(() => configs.value.length);
const enabledTaskCount = computed(
  () => tasks.value.filter(item => item.status === "open").length
);

const statCards = computed(() => [
  {
    label: copy.value.servicesLabel,
    value: totalServiceCount.value,
    meta: `${runningServiceCount.value} ${copy.value.online}`,
    tone: "blue"
  },
  {
    label: copy.value.instancesLabel,
    value: totalInstanceCount.value,
    meta: `${activeInstanceCount.value} ${copy.value.healthy}`,
    tone: "green"
  },
  {
    label: copy.value.configLabel,
    value: configCount.value,
    meta: `${copy.value.today} 3`,
    tone: "purple"
  },
  {
    label: copy.value.taskLabel,
    value: enabledTaskCount.value,
    meta: `${tasks.value.length} ${copy.value.today}`,
    tone: "orange"
  }
]);

const moduleCards = computed(() => [
  {
    title: copy.value.serviceDiscovery,
    tag: copy.value.serviceBadge,
    description:
      locale.value === "en"
        ? "Service registry and instance health overview."
        : "服务注册与实例健康状况总览。",
    path: "/service-discovery",
    accent: "blue"
  },
  {
    title: copy.value.configCenter,
    tag: copy.value.configBadge,
    description:
      locale.value === "en"
        ? "Configuration groups and JSON-driven configuration management."
        : "分组化配置管理，支持 JSON 配置编辑。",
    path: "/config-center",
    accent: "purple"
  },
  {
    title: copy.value.raftCluster,
    tag: "raft",
    description:
      locale.value === "en"
        ? "Leader election, quorum health, and node membership status."
        : "领导选举、仲裁状态和节点成员关系监控。",
    path: "/raft-cluster",
    accent: "green"
  },
  {
    title: copy.value.scheduledTasks,
    tag: copy.value.taskBadge,
    description:
      locale.value === "en"
        ? "Cron-based jobs with execution control and logs."
        : "基于 Cron 的任务调度与执行日志管理。",
    path: "/scheduled-tasks",
    accent: "orange"
  }
]);

const recentTaskList = computed(() =>
  tasks.value.slice(0, 3).map(task => ({
    name: task.name,
    service: task.serviceName,
    time: task.nextRun,
    state: task.status === "open" ? (locale.value === "en" ? "Running" : "运行中") : locale.value === "en" ? "Stopped" : "已停用"
  }))
);

const alerts = computed(() => [
  {
    title: locale.value === "en" ? "Service health" : "服务健康",
    value: `${runningServiceCount.value}/${totalServiceCount.value}`,
    tone: "success"
  },
  {
    title: locale.value === "en" ? "Config changes" : "配置变更",
    value: `${configCount.value}`,
    tone: "warning"
  },
  {
    title: locale.value === "en" ? "Schedule execution" : "任务执行",
    value: `${enabledTaskCount.value}`,
    tone: "primary"
  }
]);

onMounted(async () => {
  try {
    const [serviceData, configData, taskData] = await Promise.all([
      fetchServiceDiscovery(),
      fetchConfigItems(),
      fetchScheduledTasks()
    ]);
    services.value = serviceData;
    configs.value = configData;
    tasks.value = taskData;
  } finally {
    loading.value = false;
  }
});
</script>

<template>
  <div class="welcome-page">
    <div class="page-shell">
      <section class="hero-panel">
        <div class="hero-copy">
          <h1>{{ copy.title }}</h1>
          <p>{{ copy.subtitle }}</p>

          <div class="hero-actions">
            <el-button type="primary" size="large" @click="$router.push('/service-discovery')">
              {{ copy.primaryAction }}
            </el-button>
            <el-button plain size="large" @click="$router.push('/scheduled-tasks')">
              {{ copy.secondaryAction }}
            </el-button>
          </div>

            </div>

        <div class="hero-visual">
          <div class="status-box">
            <div class="status-header">
              <span>{{ copy.systemStatus }}</span>
              <span class="pulse-dot" />
            </div>

            <div class="metric-grid">
              <div v-for="item in statCards" :key="item.label" class="mini-card" :class="item.tone">
                <span class="mini-label">{{ item.label }}</span>
                <strong>{{ item.value }}</strong>
                <small>{{ item.meta }}</small>
              </div>
            </div>
          </div>
        </div>
      </section>

      <section class="module-section">
        <div class="module-grid">
          <el-card
            v-for="card in moduleCards"
            :key="card.title"
            shadow="never"
            class="module-card"
            :class="card.accent"
            @click="$router.push(card.path)"
          >
            <h3>{{ card.title }}</h3>
            <p>{{ card.description }}</p>
            <div class="card-footer">
              <span>{{ copy.overview }}</span>
              <span>→</span>
            </div>
          </el-card>
        </div>
      </section>

    </div>
  </div>
</template>

<style scoped lang="scss">
.welcome-page {
  min-height: calc(100vh - 48px);
  background:
    radial-gradient(circle at top left, rgba(59, 130, 246, 0.12), transparent 32%),
    radial-gradient(circle at bottom right, rgba(168, 85, 247, 0.12), transparent 28%),
    #f4f7fb;
  padding: 24px;
}

.page-shell {
  width: 100%;
  max-width: none;
  margin: 0;
  display: flex;
  flex-direction: column;
  gap: 24px;
}

.hero-panel {
  display: grid;
  grid-template-columns: 1.1fr 0.9fr;
  gap: 24px;
  padding: 26px;
  border: 1px solid rgba(148, 163, 184, 0.18);
  border-radius: 16px;
  background: linear-gradient(135deg, rgba(15, 23, 42, 0.95), rgba(37, 99, 235, 0.7));
  color: #fff;
  box-shadow: 0 20px 50px rgba(15, 23, 42, 0.16);
}

.hero-copy {
  display: flex;
  flex-direction: column;
  justify-content: center;
}

.eyebrow {
  display: inline-flex;
  align-self: flex-start;
  padding: 6px 10px;
  border-radius: 999px;
  background: rgba(255, 255, 255, 0.12);
  border: 1px solid rgba(255, 255, 255, 0.2);
  font-size: 12px;
  letter-spacing: 0.08em;
  text-transform: uppercase;
}

.hero-copy h1 {
  margin: 18px 0 12px;
  font-size: clamp(2.3rem, 4vw, 4rem);
  line-height: 1.08;
  font-weight: 800;
}

.hero-copy p {
  max-width: 560px;
  color: rgba(255, 255, 255, 0.8);
  font-size: 1rem;
  line-height: 1.8;
}

.hero-actions {
  display: flex;
  gap: 12px;
  margin-top: 22px;
}

.feature-list {
  display: flex;
  flex-wrap: wrap;
  gap: 10px;
  margin-top: 24px;
  padding: 0;
  list-style: none;

  li {
    padding: 8px 12px;
    border-radius: 999px;
    background: rgba(255, 255, 255, 0.08);
    border: 1px solid rgba(255, 255, 255, 0.14);
    color: rgba(255, 255, 255, 0.82);
    font-size: 0.82rem;
  }
}

.hero-visual {
  display: flex;
  align-items: center;
  justify-content: center;
}

.status-box {
  width: 100%;
  min-height: 260px;
  padding: 18px;
  border-radius: 14px;
  background: rgba(15, 23, 42, 0.42);
  border: 1px solid rgba(255, 255, 255, 0.18);
}

.status-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  font-size: 0.9rem;
  color: rgba(255, 255, 255, 0.72);
  margin-bottom: 16px;
}

.pulse-dot {
  width: 10px;
  height: 10px;
  border-radius: 50%;
  display: inline-block;
  background: #34d399;
  box-shadow: 0 0 0 8px rgba(52, 211, 153, 0.18);
}

.metric-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(140px, 1fr));
  gap: 14px;
}

.mini-card {
  display: flex;
  flex-direction: column;
  gap: 8px;
  padding: 16px 14px;
  border-radius: 12px;
  border: 1px solid rgba(255, 255, 255, 0.08);
  background: rgba(255, 255, 255, 0.06);

  &.blue {
    background: rgba(59, 130, 246, 0.12);
  }

  &.green {
    background: rgba(16, 185, 129, 0.12);
  }

  &.purple {
    background: rgba(168, 85, 247, 0.12);
  }

  &.orange {
    background: rgba(249, 115, 22, 0.12);
  }

  .mini-label {
    color: rgba(255, 255, 255, 0.72);
    font-size: 0.76rem;
  }

  strong {
    font-size: 1.8rem;
    line-height: 1;
    color: #fff;
  }

  small {
    color: rgba(255, 255, 255, 0.7);
  }
}

.module-section,
.bottom-grid {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.section-title {
  display: flex;
  align-items: center;
  justify-content: space-between;
  font-size: 1.05rem;
  font-weight: 700;
  color: #0f172a;
}

.module-grid {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  gap: 20px;
}

.module-card {
  cursor: pointer;
  border-radius: 14px;
  border: 1px solid rgba(148, 163, 184, 0.18);
  background: rgba(255, 255, 255, 0.7);
  transition: transform 0.2s ease, box-shadow 0.2s ease;

  &:hover {
    transform: translateY(-2px);
    box-shadow: 0 14px 30px rgba(15, 23, 42, 0.08);
  }

  :deep(.el-card__body) {
    padding: 18px 18px 16px;
  }

  &.blue {
    .module-tag { background: rgba(37, 99, 235, 0.1); color: #1d4ed8; }
  }

  &.purple {
    .module-tag { background: rgba(168, 85, 247, 0.1); color: #7c3aed; }
  }

  &.orange {
    .module-tag { background: rgba(249, 115, 22, 0.1); color: #c2410c; }
  }

  h3 {
    margin: 14px 0 8px;
    font-size: 1.2rem;
    color: #0f172a;
  }

  p {
    margin: 0;
    color: #64748b;
    line-height: 1.7;
    min-height: 48px;
  }
}

.module-tag {
  display: inline-flex;
  align-items: center;
  padding: 6px 10px;
  border-radius: 999px;
  font-size: 11px;
  font-weight: 700;
  letter-spacing: 0.04em;
  text-transform: uppercase;
}

.card-footer {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-top: 18px;
  padding-top: 12px;
  border-top: 1px solid rgba(148, 163, 184, 0.18);
  font-size: 0.82rem;
  color: #475569;
  font-weight: 600;
}

.bottom-grid {
  display: grid;
  grid-template-columns: 1.1fr 0.9fr;
  gap: 20px;
}

.info-card {
  border-radius: 18px;
  border: 1px solid rgba(148, 163, 184, 0.18);
  background: rgba(255, 255, 255, 0.72);

  :deep(.el-card__header) {
    padding: 16px 20px;
    border-bottom: 1px solid #eef2f7;
    background: rgba(248, 250, 252, 0.8);
  }

  :deep(.el-card__body) {
    padding: 18px 20px 20px;
  }
}

.card-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  font-weight: 700;
  color: #0f172a;
}

.tiny-tag {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  min-width: 28px;
  height: 24px;
  padding: 0 6px;
  border-radius: 999px;
  background: rgba(37, 99, 235, 0.08);
  color: #1d4ed8;
  font-size: 11px;
  font-weight: 700;
}

.task-list,
.alert-list {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.task-item,
.alert-item {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  padding: 12px 14px;
  border-radius: 12px;
  background: #f8fafc;
  border: 1px solid #edf2f7;
}

.task-item {
  strong {
    display: block;
    color: #0f172a;
  }

  small {
    display: block;
    color: #64748b;
    margin-top: 4px;
  }
}

.task-meta {
  text-align: right;
  display: flex;
  flex-direction: column;
  align-items: flex-end;
  gap: 4px;

  span {
    font-size: 12px;
    color: #64748b;
  }

  em {
    font-style: normal;
    font-size: 11px;
    font-weight: 700;
    color: #0f766e;
  }
}

.alert-item {
  &.success { border-color: rgba(16, 185, 129, 0.2); }
  &.warning { border-color: rgba(245, 158, 11, 0.2); }
  &.primary { border-color: rgba(59, 130, 246, 0.2); }

  .alert-label {
    display: block;
    color: #475569;
    font-size: 12px;
    margin-bottom: 4px;
  }

  strong {
    color: #0f172a;
    font-size: 1.1rem;
  }
}

.status-pill {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  min-width: 52px;
  height: 28px;
  padding: 0 10px;
  border-radius: 999px;
  background: rgba(15, 23, 42, 0.06);
  color: #475569;
  font-size: 10px;
  font-weight: 800;
  letter-spacing: 0.06em;
}

.empty-state {
  padding: 18px 0 8px;
  text-align: center;
  color: #64748b;
}

@media (max-width: 1100px) {
  .hero-panel,
  .bottom-grid {
    grid-template-columns: 1fr;
  }

  .module-grid {
    grid-template-columns: 1fr;
  }
}

@media (max-width: 720px) {
  .welcome-page {
    padding: 16px;
  }

  .hero-panel {
    padding: 18px;
  }

  .hero-actions {
    flex-direction: column;
  }

  .metric-grid {
    grid-template-columns: 1fr 1fr;
  }
}
</style>
