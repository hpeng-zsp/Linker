<script setup lang="ts">
import { computed, onMounted, ref } from "vue";
import { ElMessage } from "element-plus";
import JsonEditor from "@/views/ops/components/JsonEditor.vue";
import { fetchConfigItems, fetchServiceGroups, publishConfig } from "@/api/operations";
import { t } from "@/i18n";

type GroupItem = Awaited<ReturnType<typeof fetchServiceGroups>>[number];
type ConfigItem = Awaited<ReturnType<typeof fetchConfigItems>>[number];

const groups = ref<GroupItem[]>([]);
const configs = ref<ConfigItem[]>([]);
const selectedGroup = ref<string>("all");
const selectedConfig = ref<ConfigItem | null>(null);
const editorValue = ref("");
const drawerVisible = ref(false);
const isCreateMode = ref(false);
const keyword = ref("");
const searchText = ref("");

const filteredConfigs = computed(() => {
  const key = searchText.value.trim().toLowerCase();
  return configs.value.filter(item => !key || item.key.toLowerCase().includes(key));
});

const handleSearch = () => {
  searchText.value = keyword.value.trim();
};

const normalizeConfigKey = (value: string) => {
  const trimmed = value.trim();
  if (!trimmed) return trimmed;
  return trimmed.endsWith(".json") ? trimmed : `${trimmed}.json`;
};

const openConfig = (item: ConfigItem) => {
  isCreateMode.value = false;
  selectedConfig.value = { ...item };
  editorValue.value = JSON.stringify(item.value, null, 2);
  drawerVisible.value = true;
};

const openCreateConfig = () => {
  isCreateMode.value = true;
  const currentGroup = selectedGroup.value === "all" ? groups.value[0]?.id || "default" : selectedGroup.value;
  const defaultKey = `${currentGroup.toLowerCase()}.json`;
  const now = new Date().toLocaleString("zh-CN", {
    year: "numeric",
    month: "2-digit",
    day: "2-digit",
    hour: "2-digit",
    minute: "2-digit",
    second: "2-digit",
    hour12: false
  });

  selectedConfig.value = {
    id: `config-${Date.now()}`,
    groupId: currentGroup,
    key: defaultKey,
    value: {},
    updatedAt: now,
    publishedAt: now,
    status: "normal"
  };
  editorValue.value = "{}";
  drawerVisible.value = true;
};

const saveConfig = async () => {
  if (!selectedConfig.value) return;

  const nextGroup = selectedConfig.value.groupId.trim();
  const nextKey = normalizeConfigKey(selectedConfig.value.key);

  if (!nextGroup || !nextKey) {
    ElMessage.error("分组和配置文件不能为空");
    return;
  }

  try {
    const parsed = JSON.parse(editorValue.value || "{}");
    const now = new Date().toLocaleString("zh-CN", {
      year: "numeric",
      month: "2-digit",
      day: "2-digit",
      hour: "2-digit",
      minute: "2-digit",
      second: "2-digit",
      hour12: false
    });

    const payload = {
      ...selectedConfig.value,
      groupId: nextGroup,
      key: nextKey,
      value: parsed,
      updatedAt: now,
      publishedAt: now
    };

    await publishConfig(payload);

    if (isCreateMode.value) {
      configs.value.unshift(payload);
      const exists = groups.value.some(group => group.id === nextGroup);
      if (!exists) {
        groups.value.push({
          id: nextGroup,
          name: nextGroup.toUpperCase(),
          description: "自定义分组"
        });
      }
      selectedGroup.value = nextGroup;
    } else {
      const index = configs.value.findIndex(item => item.id === payload.id);
      if (index >= 0) {
        configs.value[index] = payload;
      } else {
        configs.value.unshift(payload);
      }
      selectedGroup.value = nextGroup;
    }

    selectedConfig.value = payload;
    editorValue.value = JSON.stringify(parsed, null, 2);
    ElMessage.success(t("publishSuccess"));
  } catch (error) {
    ElMessage.error("JSON 格式不合法");
  }
};

const downloadJson = () => {
  if (!selectedConfig.value) return;
  const blob = new Blob([JSON.stringify(selectedConfig.value.value, null, 2)], {
    type: "application/json;charset=utf-8"
  });
  const url = URL.createObjectURL(blob);
  const link = document.createElement("a");
  link.href = url;
  link.download = `${selectedConfig.value.key}.json`;
  link.click();
  URL.revokeObjectURL(url);
};

const load = async () => {
  groups.value = await fetchServiceGroups();
  configs.value = await fetchConfigItems();
  if (selectedGroup.value === "all" && groups.value[0]) {
    selectedGroup.value = "all";
  }
  if (!selectedConfig.value && configs.value[0]) {
    selectedConfig.value = null;
    drawerVisible.value = false;
  }
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
          <span>{{ t("configCenter") }}</span>
          <div class="header-actions">
            <el-button type="primary" @click="openCreateConfig">{{ t("add") }}</el-button>
            <el-button type="primary" plain @click="load">{{ t("refresh") }}</el-button>
          </div>
        </div>
      </template>

      <div class="config-layout">
        <div class="toolbar-row">
          <el-input
            v-model="keyword"
            clearable
            :placeholder="t('searchPlaceholder')"
            style="max-width: 320px"
          />
          <el-button type="primary" @click="handleSearch">{{ t("search") }}</el-button>
        </div>

        <div class="section-title config-title">{{ t("configFile") }}</div>
        <el-table :data="filteredConfigs" stripe border style="width: 100%">
          <el-table-column prop="key" :label="t('configFile')" min-width="220" />
          <el-table-column prop="updatedAt" :label="t('lastSaved')" width="180" />
          <el-table-column prop="publishedAt" :label="t('lastPublished')" width="180" />
          <el-table-column :label="t('detail')" width="120">
            <template #default="{ row }">
              <el-button link type="primary" @click="openConfig(row)">{{ t("viewDetail") }}</el-button>
            </template>
          </el-table-column>
        </el-table>
      </div>
    </el-card>

    <el-drawer
      v-model="drawerVisible"
      :title="isCreateMode ? t('add') : selectedConfig?.key || t('configCenter')"
      size="75%"
      direction="rtl"
    >
      <div v-if="selectedConfig" class="editor-block">
        <el-form label-width="110px">
          <el-form-item :label="t('configFile')">
            <el-input v-model="selectedConfig.key" />
          </el-form-item>
        </el-form>

        <div class="shortcut-hint">Tip: Alt + Shift + F</div>
        <JsonEditor v-model="editorValue" />

        <div class="editor-actions">
          <el-button type="primary" @click="saveConfig">{{ t("save") }}</el-button>
          <el-button @click="downloadJson">{{ t("downloadJson") }}</el-button>
        </div>
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
.header-actions {
  display: flex;
  align-items: center;
  gap: 8px;
}
.config-layout {
  display: flex;
  flex-direction: column;
  width: 100%;
}
.toolbar-row {
  display: flex;
  justify-content: flex-start;
  align-items: center;
  gap: 12px;
  margin-bottom: 16px;
}
.section-title {
  margin-bottom: 12px;
  font-weight: 700;
}
.config-title {
  margin-top: 18px;
}
.editor-block {
  display: flex;
  flex-direction: column;
  gap: 16px;
}
.editor-actions {
  display: flex;
  gap: 8px;
}
.shortcut-hint {
  color: #6b7280;
  font-size: 12px;
  line-height: 1.5;
}
</style>
