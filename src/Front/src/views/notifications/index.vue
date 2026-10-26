<script setup lang="ts">
import { computed, ref } from "vue";
import { noticesData } from "@/layout/components/lay-notice/data";

const pageItems = ref([
  ...noticesData[0].list,
  {
    avatar: "",
    title: "",
    description: "系统检测到新的一轮实例健康检查，相关告警已按优先级更新。",
    datetime: "刚刚",
    type: "1"
  },
  {
    avatar: "",
    title: "",
    description: "平台服务版本更新已完成，通知中心已同步最新变更说明。",
    datetime: "刚刚",
    type: "1"
  }
]);

const totalItems = computed(() => pageItems.value.length);
</script>

<template>
  <div class="notification-page">
    <el-card shadow="never" class="notification-card">
      <template #header>
        <div class="page-header">
          <span>通知列表</span>
        </div>
      </template>

      <div class="summary">
        <span>共有 {{ totalItems }} 条通知</span>
      </div>

      <div v-if="pageItems.length" class="notification-list">
        <div v-for="(item, index) in pageItems" :key="`${item.datetime}-${index}`" class="notification-item">
          <div class="notification-content">{{ item.description }}</div>
          <div class="notification-time">{{ item.datetime }}</div>
        </div>
      </div>

      <el-empty v-else description="暂无通知" />
    </el-card>
  </div>
</template>

<style scoped>
.notification-page {
  padding: 16px;
}

.notification-card {
  min-height: calc(100vh - 120px);
}

.page-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  font-size: 16px;
  font-weight: 600;
}

.summary {
  margin: 0 0 12px;
  color: #606266;
  font-size: 13px;
}

.notification-list {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.notification-item {
  display: flex;
  flex-direction: column;
  gap: 6px;
  padding: 14px 16px;
  border: 1px solid #f0f0f0;
  border-radius: 10px;
  background: #fafafa;
}

.notification-content {
  color: #303133;
  line-height: 1.7;
}

.notification-time {
  color: #909399;
  font-size: 12px;
}
</style>
