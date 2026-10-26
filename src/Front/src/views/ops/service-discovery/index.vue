<script setup lang="ts">
import { computed, onMounted, ref } from "vue";
import { fetchServiceDiscovery, getServiceInstanceStat } from "@/api/operations";
import { t } from "@/i18n";

type ServiceItem = Awaited<ReturnType<typeof fetchServiceDiscovery>>[number];

const keyword = ref("");
const searchText = ref("");
const list = ref<ServiceItem[]>([]);
const selected = ref<ServiceItem | null>(null);
const drawerVisible = ref(false);

const disconnectInstance = (instanceId: string) => {
  if (!selected.value) return;
  const target = selected.value.instances.find(item => item.id === instanceId);
  if (target) {
    target.status = "closed";
    target.health = "warning";
  }
};

const getInstanceCount = (item: ServiceItem) => {
  return getServiceInstanceStat(item.instances).display;
};

const handleSearch = () => {
  searchText.value = keyword.value.trim();
};

const filteredList = computed(() => {
  const key = searchText.value.trim().toLowerCase();
  if (!key) return list.value;
  return list.value.filter(
    item =>
      item.name.toLowerCase().includes(key) ||
      item.description.toLowerCase().includes(key) ||
      item.microservices.some(name => name.toLowerCase().includes(key))
  );
});

const load = async () => {
  list.value = await fetchServiceDiscovery();
  if (!selected.value && list.value[0]) {
    selected.value = null;
    drawerVisible.value = false;
  }
};

const openDetail = (item: ServiceItem) => {
  selected.value = item;
  drawerVisible.value = true;
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
          <span>{{ t("serviceDiscovery") }}</span>
          <el-button type="primary" @click="load">{{ t("refresh") }}</el-button>
        </div>
      </template>

      <div class="toolbar">
        <el-input
          v-model="keyword"
          :placeholder="t('searchPlaceholder')"
          clearable
          style="max-width: 300px"
        />
        <el-button type="primary" @click="handleSearch">{{ t("search") }}</el-button>
      </div>

      <el-table :data="filteredList" stripe border style="width: 100%">
        <el-table-column prop="name" :label="t('serviceName')" min-width="220" />
        <el-table-column prop="status" :label="t('status')" width="120">
          <template #default="{ row }">
            <el-tag :type="row.status === 'open' ? 'success' : 'info'">
              {{ row.status === "open" ? t("open") : t("closed") }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column :label="t('instanceCount')" width="120">
          <template #default="{ row }">
            {{ getInstanceCount(row) }}
          </template>
        </el-table-column>
        <el-table-column :label="t('detail')" width="120">
          <template #default="{ row }">
            <el-button link type="primary" @click="openDetail(row)">{{ t("detail") }}</el-button>
          </template>
        </el-table-column>
      </el-table>
    </el-card>

    <el-drawer
      v-model="drawerVisible"
      :title="selected?.name || t('serviceDiscovery')"
      size="70%"
      direction="rtl"
    >
      <div v-if="selected" class="drawer-content">
        <div class="service-summary">
          <div class="service-summary__name">{{ selected.name }}</div>
          <div class="service-summary__desc">{{ selected.description }}</div>
        </div>

        <el-descriptions :column="1" border>
          <el-descriptions-item :label="t('serviceName')">{{ selected.name }}</el-descriptions-item>
          <el-descriptions-item :label="t('status')">
            <el-tag :type="selected.status === 'open' ? 'success' : 'info'">
              {{ selected.status === "open" ? t("open") : t("closed") }}
            </el-tag>
          </el-descriptions-item>
          <el-descriptions-item :label="t('instanceCount')">{{ getInstanceCount(selected) }}</el-descriptions-item>
          <el-descriptions-item :label="t('description')">{{ selected.description }}</el-descriptions-item>
        </el-descriptions>

        <div class="microservice-header">{{ t("instanceList") }}</div>
        <el-table :data="selected.instances" stripe border style="width: 100%">
          <el-table-column prop="id" :label="t('instanceName')" min-width="140" />
          <el-table-column prop="ip" :label="t('internalIp')" width="150" />
          <el-table-column prop="port" :label="t('port')" width="100" />
          <el-table-column prop="status" :label="t('status')" width="120">
            <template #default="{ row }">
              <el-tag :type="row.status === 'open' ? 'success' : 'info'">
                {{ row.status === "open" ? t("open") : t("closed") }}
              </el-tag>
            </template>
          </el-table-column>
          <el-table-column prop="lastHeartbeat" :label="t('lastRun')" min-width="180" />
          <el-table-column :label="t('disconnect')" width="110">
            <template #default="{ row }">
              <el-button
                type="danger"
                link
                :disabled="row.status === 'closed'"
                @click="disconnectInstance(row.id)"
              >
                {{ t("disconnect") }}
              </el-button>
            </template>
          </el-table-column>
        </el-table>
      </div>
    </el-drawer>
  </div>
</template>

<style scoped>
.page-wrap {
  display: flex;
  flex-direction: column;
  gap: 18px;
}
.card-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
}
.toolbar {
  display: flex;
  align-items: center;
  gap: 12px;
  margin-bottom: 16px;
}
.drawer-content {
  display: flex;
  flex-direction: column;
  gap: 18px;
}
.service-summary {
  padding: 12px 14px;
  border: 1px solid #ebeef5;
  border-radius: 8px;
  background: #f5f7fa;
}
.service-summary__name {
  font-size: 18px;
  font-weight: 700;
  color: #1f2937;
}
.service-summary__desc {
  margin-top: 4px;
  color: #6b7280;
  font-size: 13px;
}
.microservice-header {
  margin-top: 8px;
  font-size: 18px;
  font-weight: 700;
  color: #1f2937;
}
.microservice-list {
  display: flex;
  flex-wrap: wrap;
  gap: 10px;
}
.microservice-tag {
  margin: 4px 0;
  padding: 8px 12px;
  font-size: 14px;
  border-radius: 999px;
}
</style>
