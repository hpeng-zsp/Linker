export interface ListItem {
  avatar: string;
  title: string;
  datetime: string;
  type: string;
  description: string;
  status?: "primary" | "success" | "warning" | "info" | "danger";
  extra?: string;
}

export interface TabItem {
  key: string;
  name: string;
  list: ListItem[];
  emptyText: string;
}

export const noticesData: TabItem[] = [
  {
    key: "1",
    name: "通知",
    list: [
      {
        avatar: "",
        title: "",
        description: "系统已成功完成服务发现同步，当前最新实例状态已更新。",
        datetime: "今天 09:32",
        type: "1"
      },
      {
        avatar: "",
        title: "",
        description: "配置中心发布记录已更新，最新版本已生效于生产环境。",
        datetime: "昨天 18:10",
        type: "1"
      },
      {
        avatar: "",
        title: "",
        description: "定时任务执行异常已修复，队列已恢复正常调度。",
        datetime: "2026-10-24 11:45",
        type: "1"
      }
    ],
    emptyText: "暂无通知"
  }
];
