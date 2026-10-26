<script setup lang="ts">
import { ref, computed } from "vue";
import { useRouter } from "vue-router";
import { noticesData } from "./data";
import NoticeList from "./components/NoticeList.vue";
import BellIcon from "~icons/ep/bell";

const router = useRouter();
const noticesNum = ref(0);
const notices = ref(noticesData);

notices.value.forEach(item => {
  noticesNum.value += item.list.length;
});

const noticesList = computed(() => notices.value[0]?.list ?? []);
const goNotificationList = () => {
  router.push("/notifications");
};
</script>

<template>
  <el-dropdown trigger="click" placement="bottom-end">
    <span
      :class="[
        'dropdown-badge',
        'navbar-bg-hover',
        'select-none',
        Number(noticesNum) !== 0 && 'mr-[10px]'
      ]"
    >
      <el-badge :value="Number(noticesNum) === 0 ? '' : noticesNum" :max="99">
        <span class="header-notice-icon">
          <IconifyIconOffline :icon="BellIcon" />
        </span>
      </el-badge>
    </span>
    <template #dropdown>
      <el-dropdown-menu>
        <div class="notice-popover">
          <div class="notice-popover-header">
            <span>通知</span>
            <el-button text size="small" @click="goNotificationList">查看全部</el-button>
          </div>
          <el-scrollbar max-height="330px">
            <div class="noticeList-container">
              <NoticeList :list="noticesList" :emptyText="notices[0]?.emptyText || '暂无通知'" />
            </div>
          </el-scrollbar>
        </div>
      </el-dropdown-menu>
    </template>
  </el-dropdown>
</template>

<style lang="scss" scoped>
.dropdown-badge {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 40px;
  height: 48px;
  cursor: pointer;

  .header-notice-icon {
    font-size: 18px;
  }
}

.notice-popover {
  width: 330px;

  .notice-popover-header {
    display: flex;
    align-items: center;
    justify-content: space-between;
    padding: 12px 16px 8px;
    font-size: 14px;
    font-weight: 600;
    color: #303133;
    border-bottom: 1px solid #f0f0f0;
  }

  .noticeList-container {
    padding: 0 16px 12px;
  }
}
</style>
