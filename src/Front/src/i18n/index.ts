import { ref } from "vue";

export type Locale = "zh" | "en";

const storageKey = "app-locale";

export const locale = ref<Locale>(
  (localStorage.getItem(storageKey) as Locale) || "zh"
);

export const messages = {
  zh: {
    appTitle: "服务治理中心",
    language: "中文",
    english: "English",
    serviceDiscovery: "服务注册发现",
    configCenter: "配置文件中心",
    scheduledTasks: "定时任务",
    serviceName: "服务名称",
    namespace: "命名空间",
    status: "状态",
    instanceCount: "实例数",
    search: "搜索",
    detail: "详情",
    publish: "发布",
    group: "分组",
    configKey: "配置文件",
    configFile: "配置文件",
    serviceDropdown: "微服务名称",
    viewDetail: "查看详情",
    editConfig: "编辑配置",
    downloadJson: "下载 JSON",
    refresh: "刷新",
    publishSuccess: "发布成功",
    execute: "执行",
    start: "启动",
    stop: "停止",
    cron: "Cron",
    lastRun: "最近执行",
    lastSaved: "最后保存时间",
    lastPublished: "最后发布时间",
    nextRun: "下一次执行时间",
    all: "全部",
    open: "运行中",
    closed: "已停用",
    normal: "正常",
    warning: "告警",
    description: "说明",
    microserviceList: "微服务列表",
    currentMicroserviceList: "当前微服务列表",
    instanceList: "实例列表",
    instanceName: "实例名",
    internalIp: "内网 IP",
    port: "端口",
    disconnect: "断开",
    groupList: "分组列表",
    configEditor: "配置编辑器",
    add: "新增",
    save: "保存",
    cancel: "取消",
    close: "关闭",
    edit: "编辑",
    run: "运行",
    logs: "日志",
    parameter: "参数",
    success: "成功",
    failed: "失败",
    taskName: "任务名称",
    taskStatus: "任务状态",
    executor: "执行器",
    executionMode: "执行方式",
    singleMode: "单次模式",
    broadcastMode: "广播模式",
    shardingMode: "分片模式",
    trigger: "触发",
    searchPlaceholder: "请输入关键字搜索",
    noData: "暂无数据",
    executionTime: "执行时间",
    executionLog: "执行日志",
    result: "结果"
  },
  en: {
    appTitle: "Service Governance Center",
    language: "English",
    english: "English",
    serviceDiscovery: "Service Registration Discovery",
    configCenter: "Config Center",
    scheduledTasks: "Scheduled Tasks",
    serviceName: "Service Name",
    namespace: "Namespace",
    status: "Status",
    instanceCount: "Instance Count",
    search: "Search",
    detail: "Detail",
    publish: "Publish",
    group: "Group",
    configKey: "Config File",
    configFile: "Config File",
    serviceDropdown: "Microservice Name",
    viewDetail: "View Detail",
    editConfig: "Edit Config",
    downloadJson: "Download JSON",
    refresh: "Refresh",
    publishSuccess: "Published successfully",
    execute: "Execute",
    start: "Start",
    stop: "Stop",
    cron: "Cron",
    lastRun: "Last Run",
    lastSaved: "Last saved time",
    lastPublished: "Last published time",
    nextRun: "Next execution time",
    all: "All",
    open: "Running",
    closed: "Stopped",
    normal: "Normal",
    warning: "Warning",
    description: "Description",
    microserviceList: "Microservice List",
    currentMicroserviceList: "Current Microservice List",
    instanceList: "Instance List",
    instanceName: "Instance Name",
    internalIp: "Internal IP",
    port: "Port",
    disconnect: "Disconnect",
    groupList: "Group List",
    configEditor: "Config Editor",
    add: "Add",
    save: "Save",
    cancel: "Cancel",
    close: "Close",
    edit: "Edit",
    run: "Run",
    logs: "Logs",
    parameter: "Parameter",
    success: "Success",
    failed: "Failed",
    taskName: "Task Name",
    taskStatus: "Task Status",
    executor: "Executor",
    executionMode: "Execution mode",
    singleMode: "Single mode",
    broadcastMode: "Broadcast mode",
    shardingMode: "Sharding mode",
    trigger: "Trigger",
    searchPlaceholder: "Enter keyword to search",
    noData: "No data",
    executionTime: "Execution time",
    executionLog: "Execution log",
    result: "Result"
  }
} as const;

export function setLocale(value: Locale) {
  locale.value = value;
  localStorage.setItem(storageKey, value);
}

export function t(key: keyof (typeof messages)["zh"]) {
  return messages[locale.value]?.[key] ?? messages.zh[key] ?? key;
}

export function resolveRouteTitle(title?: string) {
  if (!title) return "";

  const staticMap: Record<string, Record<Locale, string>> = {
    首页: { zh: "首页", en: "Home" },
    welcome: { zh: "首页", en: "Home" },
    登录: { zh: "登录", en: "Login" },
    "服务发现": { zh: "服务注册发现", en: "Service Registration Discovery" },
    "服务注册发现": { zh: "服务注册发现", en: "Service Registration Discovery" },
    serviceDiscovery: { zh: "服务注册发现", en: "Service Registration Discovery" },
    "配置文件中心": { zh: "配置文件中心", en: "Config Center" },
    configCenter: { zh: "配置文件中心", en: "Config Center" },
    "定时任务": { zh: "定时任务", en: "Scheduled Tasks" },
    scheduledTasks: { zh: "定时任务", en: "Scheduled Tasks" },
    主页: { zh: "主页", en: "Home" },
    "页面权限": { zh: "页面权限", en: "Page Permission" },
    "按钮权限": { zh: "按钮权限", en: "Button Permission" },
    权限管理: { zh: "权限管理", en: "Permission Management" }
  };

  const direct = staticMap[title];
  if (direct) return direct[locale.value] ?? direct.zh;
  return title;
}

export const localeOptions = [
  { label: "中文", value: "zh" },
  { label: "English", value: "en" }
] as const;
