<script setup lang="ts">
import { computed, onMounted, ref, watch } from "vue";
import { ElMessage, ElMessageBox } from "element-plus";
import { fetchScheduledTasks, type ScheduledTaskItem } from "@/api/operations";
import { t } from "@/i18n";

type TaskItem = Awaited<ReturnType<typeof fetchScheduledTasks>>[number];

const tasks = ref<TaskItem[]>([]);
const selectedService = ref("all");
const keyword = ref("");
const searchText = ref("");
const formVisible = ref(false);
const formMode = ref<"create" | "edit">("create");
const runDialogVisible = ref(false);
const logsVisible = ref(false);
const currentTask = ref<Partial<TaskItem>>({
  name: "",
  description: "",
  serviceName: "",
  cron: "0 0/5 * * * ?",
  executionMode: "single",
  status: "open",
  params: "{}",
  logs: []
});
const runTarget = ref<TaskItem | null>(null);
const runParams = ref("{}");
const selectedLogsTask = ref<TaskItem | null>(null);

const formatDate = (date = new Date()) =>
  new Date(date).toLocaleString("zh-CN", {
    year: "numeric",
    month: "2-digit",
    day: "2-digit",
    hour: "2-digit",
    minute: "2-digit",
    second: "2-digit",
    hour12: false
  });

const load = async () => {
  tasks.value = await fetchScheduledTasks();
};

const handleSearch = () => {
  searchText.value = keyword.value.trim();
};

const executionModeOptions = [
  { label: "单次模式", value: "single" },
  { label: "广播模式", value: "broadcast" },
  { label: "分片模式", value: "sharding" }
] as const;

const cronPresetOptions = [
  { label: "每天", value: "daily" },
  { label: "每周", value: "weekly" },
  { label: "每月", value: "monthly" },
  { label: "自定义 Cron", value: "custom" }
] as const;

const weekdayOptions = [
  { label: "周一", value: "MON" },
  { label: "周二", value: "TUE" },
  { label: "周三", value: "WED" },
  { label: "周四", value: "THU" },
  { label: "周五", value: "FRI" },
  { label: "周六", value: "SAT" },
  { label: "周日", value: "SUN" }
] as const;

const cronPreset = ref<"daily" | "weekly" | "monthly" | "custom">("daily");
const cronTime = ref("09:00");
const cronWeekday = ref("MON");
const cronDayOfMonth = ref(1);

const resolveCronFromPreset = () => {
  const [hour, minute] = cronTime.value.split(":").map(Number);

  switch (cronPreset.value) {
    case "daily":
      return `0 ${minute} ${hour} * * ?`;
    case "weekly":
      return `0 ${minute} ${hour} ? * ${cronWeekday.value}`;
    case "monthly":
      return `0 ${minute} ${hour} ${cronDayOfMonth.value} * ?`;
    case "custom":
      return currentTask.value.cron?.trim() || "0 0 9 * * ?";
    default:
      return currentTask.value.cron?.trim() || "0 0 9 * * ?";
  }
};

const cronSummary = computed(() => {
  if (cronPreset.value === "custom") {
    return `自定义表达式：${currentTask.value.cron || "0 0 9 * * ?"}`;
  }

  const timeText = `${cronTime.value || "09:00"}`;

  if (cronPreset.value === "daily") {
    return `每天 ${timeText} 执行`;
  }

  if (cronPreset.value === "weekly") {
    const weekdayText = weekdayOptions.find(item => item.value === cronWeekday.value)?.label || "周一";
    return `每周 ${weekdayText} ${timeText} 执行`;
  }

  return `每月 ${cronDayOfMonth.value} 日 ${timeText} 执行`;
});

const applyCronPreset = () => {
  if (cronPreset.value !== "custom") {
    currentTask.value.cron = resolveCronFromPreset();
  }
};

const syncCronPresetFromTask = () => {
  const cronValue = (currentTask.value.cron || "").trim();
  if (!cronValue) {
    cronPreset.value = "daily";
    return;
  }

  if (/^\d+\s+\d+\s+\d+\s+\*\s+\*\s+\?$/.test(cronValue)) {
    cronPreset.value = "daily";
    const [, minute, hour] = cronValue.split(" ");
    cronTime.value = `${String(hour).padStart(2, "0")}:${String(minute).padStart(2, "0")}`;
    return;
  }

  if (/^\d+\s+\d+\s+\d+\s+\?\s+\*\s+[A-Z]+$/.test(cronValue)) {
    cronPreset.value = "weekly";
    const parts = cronValue.split(" ");
    const [, minute, hour, , , dayOfWeek] = parts;
    cronTime.value = `${String(hour).padStart(2, "0")}:${String(minute).padStart(2, "0")}`;
    cronWeekday.value = dayOfWeek;
    return;
  }

  if (/^\d+\s+\d+\s+\d+\s+\d+\s+\*\s+\?$/.test(cronValue)) {
    cronPreset.value = "monthly";
    const [, minute, hour, dayOfMonth] = cronValue.split(" ");
    cronTime.value = `${String(hour).padStart(2, "0")}:${String(minute).padStart(2, "0")}`;
    cronDayOfMonth.value = Number(dayOfMonth);
    return;
  }

  cronPreset.value = "custom";
};

watch([cronPreset, cronTime, cronWeekday, cronDayOfMonth], () => {
  if (formVisible.value) {
    applyCronPreset();
  }
});

watch(() => currentTask.value.cron, value => {
  if (!value || cronPreset.value === "custom") return;
  syncCronPresetFromTask();
});

const getExecutionModeLabel = (value: TaskItem["executionMode"]) => {
  return executionModeOptions.find(option => option.value === value)?.label || value;
};

const getExecutionModeType = (value: TaskItem["executionMode"]) => {
  switch (value) {
    case "single":
      return "primary";
    case "broadcast":
      return "success";
    case "sharding":
      return "warning";
    default:
      return "info";
  }
};

const filteredTasks = computed(() => {
  const key = searchText.value.trim().toLowerCase();
  return tasks.value.filter(item => {
    const matchesService = selectedService.value === "all" || item.serviceName === selectedService.value;
    const matchesKeyword =
      !key ||
      [item.name, item.serviceName, item.executionMode, item.cron].some(value =>
        String(value).toLowerCase().includes(key)
      );
    return matchesService && matchesKeyword;
  });
});

const serviceOptions = computed(() => {
  const values = tasks.value.map(item => item.serviceName);
  return ["all", ...Array.from(new Set(values))];
});

const openCreateTask = () => {
  formMode.value = "create";
  cronPreset.value = "daily";
  cronTime.value = "09:00";
  cronWeekday.value = "MON";
  cronDayOfMonth.value = 1;
  currentTask.value = {
    id: `job-${Date.now()}`,
    name: "",
    description: "",
    serviceName: "user-service",
    cron: "0 0 9 * * ?",
    executionMode: "single",
    status: "open",
    params: "{}",
    lastRun: formatDate(),
    logs: []
  };
  applyCronPreset();
  formVisible.value = true;
};

const openEditTask = (task: TaskItem) => {
  formMode.value = "edit";
  currentTask.value = { ...task };
  syncCronPresetFromTask();
  formVisible.value = true;
};

const saveTask = () => {
  const item = currentTask.value;
  if (!item?.name?.trim() || !item.serviceName?.trim() || !item.cron?.trim()) {
    ElMessage.error("任务名称、服务名和 Cron 不能为空");
    return;
  }

  const snapshot: TaskItem = {
    id: item.id || `job-${Date.now()}`,
    name: item.name.trim(),
    description: item.description?.trim() || "",
    serviceName: item.serviceName.trim(),
    cron: item.cron.trim(),
    executionMode: item.executionMode || "single",
    status: item.status || "open",
    params: item.params || "{}",
    lastRun: item.lastRun || formatDate(),
    nextRun: item.nextRun || formatDate(new Date(Date.now() + 5 * 60 * 1000)),
    logs: item.logs || []
  };

  if (formMode.value === "create") {
    tasks.value.unshift(snapshot);
    ElMessage.success("任务创建成功");
  } else {
    const index = tasks.value.findIndex(task => task.id === snapshot.id);
    if (index >= 0) tasks.value[index] = snapshot;
    ElMessage.success("任务更新成功");
  }

  formVisible.value = false;
};

const runTask = (task: TaskItem) => {
  runTarget.value = task;
  runParams.value = task.params || "{}";
  runDialogVisible.value = true;
};

const toggleTaskStatus = (task: TaskItem) => {
  const nextStatus = task.status === "open" ? "closed" : "open";
  const actionText = nextStatus === "open" ? t("start") : t("stop");
  task.status = nextStatus;
  ElMessage.success(`${task.name} ${actionText}`);
};

const confirmRunTask = () => {
  if (!runTarget.value) return;

  const now = formatDate();
  const payload = runParams.value.trim() || "{}";
  const success = Math.random() > 0.2;

  runTarget.value.params = payload;
  runTarget.value.lastRun = now;
  runTarget.value.nextRun = formatDate(new Date(Date.now() + 5 * 60 * 1000));
  runTarget.value.status = "open";
  runTarget.value.logs = [
    {
      time: now,
      status: success ? "success" : "failed",
      message: success ? `参数 ${payload} 执行成功` : `参数 ${payload} 执行失败，稍后重试`
    },
    ...(runTarget.value.logs || [])
  ];

  ElMessage.success(success ? "任务执行成功" : "任务执行失败");
  runDialogVisible.value = false;
};

const viewLogs = (task: TaskItem) => {
  selectedLogsTask.value = task;
  logsVisible.value = true;
};

const deleteTask = (task: TaskItem) => {
  ElMessageBox.confirm(`确定删除任务 "${task.name}" 吗？`, "删除确认", {
    confirmButtonText: "删除",
    cancelButtonText: "取消",
    type: "warning"
  })
    .then(() => {
      tasks.value = tasks.value.filter(item => item.id !== task.id);
      ElMessage.success(`${task.name} 已删除`);
    })
    .catch(() => {});
};

onMounted(() => {
  load();
});
</script>

<template>
  <div class="page-wrap">
    <el-card shadow="never">
      <template #header>
        <div class="card-header">
          <span>{{ t("scheduledTasks") }}</span>
          <div class="header-actions">
            <el-button type="primary" @click="openCreateTask">{{ t("add") }}</el-button>
            <el-button type="primary" plain @click="load">{{ t("refresh") }}</el-button>
          </div>
        </div>
      </template>

      <div class="toolbar">
        <el-select v-model="selectedService" style="width: 220px">
          <el-option :label="t('all')" value="all" />
          <el-option
            v-for="item in serviceOptions.filter(option => option !== 'all')"
            :key="item"
            :label="item"
            :value="item"
          />
        </el-select>
        <el-input v-model="keyword" :placeholder="t('searchPlaceholder')" clearable style="width: 240px" />
        <el-button type="primary" @click="handleSearch">{{ t("search") }}</el-button>
      </div>

      <el-table :data="filteredTasks" stripe border style="width: 100%">
        <el-table-column prop="name" :label="t('taskName')" min-width="220" />
        <el-table-column prop="description" :label="t('description')" min-width="220" />
        <el-table-column prop="serviceName" :label="t('serviceDropdown')" min-width="180" />
        <el-table-column prop="cron" :label="t('cron')" min-width="180" />
        <el-table-column prop="nextRun" :label="t('nextRun')" width="132">
          <template #default="{ row }">
            <span class="next-run-cell">{{ row.nextRun }}</span>
          </template>
        </el-table-column>
        <el-table-column prop="executionMode" :label="t('executionMode')" width="105">
          <template #default="{ row }">
            <el-tag :type="getExecutionModeType(row.executionMode)" class="execution-tag">{{ getExecutionModeLabel(row.executionMode) }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="status" :label="t('taskStatus')" width="84">
          <template #default="{ row }">
            <el-tag :type="row.status === 'open' ? 'success' : 'info'">
              {{ row.status === "open" ? t("open") : t("closed") }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column :label="t('operation')" width="286" fixed="right">
          <template #default="{ row }">
            <div class="action-group">
              <div class="action-row">
                <el-button size="small" type="primary" plain @click="openEditTask(row)">{{ t("edit") }}</el-button>
                <el-button size="small" type="success" plain @click="runTask(row)">{{ t("execute") }}</el-button>
                <el-button
                  size="small"
                  :type="row.status === 'open' ? 'warning' : 'success'"
                  plain
                  @click="toggleTaskStatus(row)"
                >
                  {{ row.status === 'open' ? t('stop') : t('start') }}
                </el-button>
              </div>
              <div class="action-row second-row">
                <el-button size="small" type="info" plain @click="viewLogs(row)">{{ t("logs") }}</el-button>
                <el-button size="small" type="danger" plain @click="deleteTask(row)">{{ t("delete") }}</el-button>
              </div>
            </div>
          </template>
        </el-table-column>
      </el-table>
    </el-card>

    <el-dialog v-model="formVisible" :title="formMode === 'create' ? t('add') : t('edit')" width="560px">
      <el-form label-width="110px">
        <el-form-item :label="t('taskName')">
          <el-input v-model="currentTask.name" />
        </el-form-item>
        <el-form-item :label="t('description')">
          <el-input v-model="currentTask.description" type="textarea" :rows="2" />
        </el-form-item>
        <el-form-item :label="t('serviceDropdown')">
          <el-input v-model="currentTask.serviceName" />
        </el-form-item>
        <el-form-item :label="t('cron')">
          <el-select v-model="cronPreset" style="width: 100%; margin-bottom: 12px">
            <el-option
              v-for="option in cronPresetOptions"
              :key="option.value"
              :label="option.label"
              :value="option.value"
            />
          </el-select>

          <div v-if="cronPreset !== 'custom'" class="cron-builder">
            <div v-if="cronPreset === 'daily'" class="cron-row">
              <span class="cron-label">执行时间</span>
              <el-time-picker
                v-model="cronTime"
                value-format="HH:mm"
                format="HH:mm"
                placeholder="选择时间"
                style="width: 100%"
              />
            </div>

            <div v-else-if="cronPreset === 'weekly'" class="cron-row">
              <span class="cron-label">执行周期</span>
              <el-select v-model="cronWeekday" style="width: 100%">
                <el-option
                  v-for="option in weekdayOptions"
                  :key="option.value"
                  :label="option.label"
                  :value="option.value"
                />
              </el-select>
              <el-time-picker
                v-model="cronTime"
                value-format="HH:mm"
                format="HH:mm"
                placeholder="选择时间"
                style="width: 100%"
              />
            </div>

            <div v-else-if="cronPreset === 'monthly'" class="cron-row">
              <span class="cron-label">执行日期</span>
              <el-input-number v-model="cronDayOfMonth" :min="1" :max="31" style="width: 100%" />
              <el-time-picker
                v-model="cronTime"
                value-format="HH:mm"
                format="HH:mm"
                placeholder="选择时间"
                style="width: 100%"
              />
            </div>

            <div class="cron-summary">{{ cronSummary }}</div>
          </div>

          <el-input
            v-else
            v-model="currentTask.cron"
            placeholder="例如：0 0 9 * * ?"
            style="margin-top: 8px"
          />
        </el-form-item>
        <el-form-item :label="t('executionMode')">
          <el-select v-model="currentTask.executionMode" style="width: 100%">
            <el-option
              v-for="option in executionModeOptions"
              :key="option.value"
              :label="option.label"
              :value="option.value"
            />
          </el-select>
        </el-form-item>
        <el-form-item :label="t('parameter')">
          <el-input v-model="currentTask.params" type="textarea" :rows="4" />
        </el-form-item>
      </el-form>
      <template #footer>
        <span class="dialog-footer">
          <el-button @click="formVisible = false">{{ t("cancel") }}</el-button>
          <el-button type="primary" @click="saveTask">{{ t("save") }}</el-button>
        </span>
      </template>
    </el-dialog>

    <el-dialog v-model="runDialogVisible" :title="t('run')" width="480px">
      <el-form label-width="88px">
        <el-form-item :label="t('parameter')">
          <el-input v-model="runParams" type="textarea" :rows="6" />
        </el-form-item>
      </el-form>
      <template #footer>
        <span class="dialog-footer">
          <el-button @click="runDialogVisible = false">{{ t("cancel") }}</el-button>
          <el-button type="primary" @click="confirmRunTask">{{ t("run") }}</el-button>
        </span>
      </template>
    </el-dialog>

    <el-dialog v-model="logsVisible" :title="selectedLogsTask?.name || t('logs')" width="640px">
      <el-table :data="selectedLogsTask?.logs || []" stripe border style="width: 100%">
        <el-table-column prop="time" :label="t('executionTime')" width="170" />
        <el-table-column :label="t('result')" width="90">
          <template #default="{ row }">
            <el-tag :type="row.status === 'success' ? 'success' : 'danger'">
              {{ row.status === 'success' ? t('success') : t('failed') }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="message" :label="t('executionLog')" min-width="220" />
      </el-table>
      <template #footer>
        <span class="dialog-footer">
          <el-button type="primary" @click="logsVisible = false">{{ t("close") }}</el-button>
        </span>
      </template>
    </el-dialog>
  </div>
</template>

<style scoped>
.page-wrap {
  display: flex;
  flex-direction: column;
}
.card-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
}
.header-actions {
  display: flex;
  align-items: center;
  gap: 8px;
}
.toolbar {
  display: flex;
  align-items: center;
  gap: 12px;
  margin-bottom: 16px;
}
.action-group {
  display: flex;
  flex-direction: column;
  align-items: flex-start;
  gap: 4px;
}

.action-row {
  display: flex;
  align-items: center;
  gap: 4px;
}

.second-row {
  justify-content: flex-start;
}

.execution-tag {
  margin: 0;
}

.next-run-cell {
  display: inline-block;
  line-height: 1.5;
  white-space: normal;
  word-break: break-word;
}
.cron-builder {
  display: flex;
  flex-direction: column;
  gap: 12px;
  padding: 12px;
  border: 1px solid #e4e7ed;
  border-radius: 10px;
  background: #f8fafc;
}
.cron-row {
  display: flex;
  flex-direction: column;
  gap: 8px;
}
.cron-label {
  font-size: 12px;
  color: #606266;
}
.cron-summary {
  padding: 8px 10px;
  border-radius: 8px;
  background: rgba(64, 158, 255, 0.08);
  color: #2b5bd7;
  font-size: 12px;
  line-height: 1.6;
}
.dialog-footer {
  display: flex;
  justify-content: flex-end;
  gap: 8px;
}
</style>
