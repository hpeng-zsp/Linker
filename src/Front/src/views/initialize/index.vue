<script setup lang="ts">
import { computed, ref } from "vue";
import { useRouter } from "vue-router";
import { ElMessage } from "element-plus";
import { initializeSystem } from "@/api/system";
import { locale, setLocale, t } from "@/i18n";

const router = useRouter();
const mode = ref<"primary" | "secondary">("primary");
const clusterAddress = ref("10.0.0.10");
const clusterPort = ref(8001);
const loading = ref(false);
const progress = ref(0);

const stepList = computed(() => [
  { key: "mode", title: t("initStepSelectMode") },
  { key: "cluster", title: t("initStepConfigCluster") },
  { key: "finish", title: t("initStepFinish") }
]);

const stepIndex = computed(() => {
  if (mode.value === "secondary") return 1;
  return 0;
});

const modeOptions = computed(() => [
  {
    value: "primary",
    title: t("initModePrimary"),
    subtitle: t("initModePrimarySub"),
    badge: t("initModePrimaryBadge")
  },
  {
    value: "secondary",
    title: t("initModeSecondary"),
    subtitle: t("initModeSecondarySub"),
    badge: t("initModeSecondaryBadge")
  }
]);

const handleComplete = async () => {
  if (mode.value === "secondary" && (!clusterAddress.value.trim() || !clusterPort.value)) {
    ElMessage.warning(t("initClusterRequired"));
    return;
  }

  loading.value = true;
  progress.value = 10;

  try {
    const result = await initializeSystem({
      mode: mode.value,
      clusterAddress: mode.value === "secondary" ? clusterAddress.value.trim() : undefined,
      clusterPort: mode.value === "secondary" ? Number(clusterPort.value) : undefined
    });

    if (result?.success === false) {
      ElMessage.error(result?.data?.message || t("initConnectionFailure"));
      loading.value = false;
      return;
    }

    const interval = window.setInterval(() => {
      progress.value = Math.min(progress.value + 18, 100);
      if (progress.value >= 100) {
        window.clearInterval(interval);
        setTimeout(() => {
          loading.value = false;
          router.push("/login");
        }, 300);
      }
    }, 200);
  } catch (error) {
    console.error(error);
    ElMessage.error(t("initFailure"));
    loading.value = false;
  }
};
</script>

<template>
  <div class="initialize-page">
    <div class="initialize-shell">
      <aside class="init-aside">
        <div class="brand-block">
          <div class="brand-mark">L</div>
          <div>
            <div class="brand-name">Linker</div>
            <div class="brand-sub">{{ t("initBrandSub") }}</div>
          </div>
        </div>

        <div class="status-list">
          <div class="status-label">{{ t("initSteps") }}</div>
          <div v-for="(item, index) in stepList" :key="item.key" class="step-item" :class="{ active: index === stepIndex, done: index < stepIndex }">
            <span class="step-dot">{{ index + 1 }}</span>
            <span>{{ item.title }}</span>
          </div>
        </div>

        <div class="mini-card">
          <div class="mini-title">{{ t("initDeployInfo") }}</div>
          <p>{{ t("initDeployDesc") }}</p>
        </div>
      </aside>

      <main class="init-main">
        <div class="topbar-right">
          <div class="language-switch" aria-label="language switch">
            <button class="lang-btn" :class="{ active: locale === 'zh' }" type="button" @click="setLocale('zh')">中文</button>
            <button class="lang-btn" :class="{ active: locale === 'en' }" type="button" @click="setLocale('en')">EN</button>
          </div>
        </div>

        <div class="initialize-header">
          <div class="title-block">
            <h1>{{ t("initSystems") }}</h1>
            <span class="meta-pill">Raft</span>
          </div>
          <p>{{ t("initHeaderHint") }}</p>
        </div>

        <div class="option-grid">
          <button
            v-for="option in modeOptions"
            :key="option.value"
            class="mode-card"
            :class="{ active: mode === option.value }"
            type="button"
            @click="mode = option.value"
          >
            <div class="mode-badge">{{ option.badge }}</div>
            <div class="mode-label">{{ option.title }}</div>
            <div class="mode-subtitle">{{ option.subtitle }}</div>
          </button>
        </div>

        <div v-if="mode === 'secondary'" class="cluster-config">
          <div class="config-row">
            <label>{{ t("initClusterAddress") }}</label>
            <el-input v-model="clusterAddress" :placeholder="t('initClusterAddressPlaceholder')" />
          </div>
          <div class="config-row">
            <label>{{ t("initClusterPort") }}</label>
            <el-input-number v-model="clusterPort" :min="1" :max="65535" style="width: 100%" />
          </div>
        </div>

        <div v-if="loading" class="progress-panel">
          <div class="progress-header">
            <span>{{ t("initProgress") }}</span>
            <span>{{ progress }}%</span>
          </div>
          <el-progress :percentage="progress" :show-text="false" status="success" />
        </div>

        <div class="action-row">
          <el-button size="large" type="primary" :loading="loading" @click="handleComplete">
            {{ t("initComplete") }}
          </el-button>
        </div>
      </main>
    </div>
  </div>
</template>

<style scoped lang="scss">
.initialize-page {
  min-height: 100vh;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 32px;
  background:
    radial-gradient(circle at top left, rgba(56, 189, 248, 0.18), transparent 22%),
    radial-gradient(circle at bottom right, rgba(59, 130, 246, 0.18), transparent 24%),
    linear-gradient(135deg, #f8fbff 0%, #eef2ff 100%);
}

.initialize-shell {
  display: grid;
  grid-template-columns: 280px minmax(0, 1fr);
  width: min(1100px, 100%);
  min-height: 720px;
  border-radius: 28px;
  background: rgba(255, 255, 255, 0.9);
  border: 1px solid rgba(148, 163, 184, 0.2);
  box-shadow: 0 28px 80px rgba(37, 99, 235, 0.08);
  overflow: hidden;
}

.init-aside {
  padding: 30px 22px;
  background: linear-gradient(180deg, rgba(15, 23, 42, 0.97), rgba(30, 64, 175, 0.9));
  color: #fff;
}

.brand-block {
  display: flex;
  align-items: center;
  gap: 12px;
  margin-bottom: 34px;
}

.brand-mark {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 42px;
  height: 42px;
  border-radius: 14px;
  background: rgba(255, 255, 255, 0.14);
  border: 1px solid rgba(255, 255, 255, 0.15);
  font-weight: 700;
}

.brand-name {
  font-size: 22px;
  font-weight: 700;
  letter-spacing: 0.05em;
}

.brand-sub {
  font-size: 11px;
  color: rgba(255, 255, 255, 0.72);
  letter-spacing: 0.1em;
  text-transform: uppercase;
}

.status-list {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.status-label {
  margin-bottom: 8px;
  color: rgba(255, 255, 255, 0.68);
  font-size: 12px;
  letter-spacing: 0.12em;
  text-transform: uppercase;
}

.step-item {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 11px 12px;
  border-radius: 12px;
  background: rgba(255, 255, 255, 0.04);
  color: rgba(255, 255, 255, 0.8);
  font-size: 14px;

  &.active {
    background: rgba(147, 197, 253, 0.2);
    color: #fff;
    border: 1px solid rgba(191, 219, 254, 0.32);
  }

  &.done {
    background: rgba(34, 197, 94, 0.14);
    color: #dcfce7;
  }
}

.step-dot {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 22px;
  height: 22px;
  border-radius: 50%;
  background: rgba(255, 255, 255, 0.12);
  font-size: 12px;
  font-weight: 700;
}

.mini-card {
  margin-top: 28px;
  padding: 16px 14px;
  border: 1px solid rgba(255, 255, 255, 0.12);
  border-radius: 14px;
  background: rgba(15, 118, 110, 0.12);

  .mini-title {
    margin-bottom: 8px;
    color: #e2e8f0;
    font-size: 12px;
    letter-spacing: 0.08em;
    text-transform: uppercase;
  }

  p {
    margin: 0;
    line-height: 1.7;
    color: rgba(255, 255, 255, 0.82);
    font-size: 13px;
  }
}

.init-main {
  position: relative;
  padding: 36px 38px;
}

.topbar-right {
  display: flex;
  justify-content: flex-end;
  margin-bottom: 18px;
}

.language-switch {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 6px;
  border-radius: 999px;
  background: rgba(148, 163, 184, 0.12);
  border: 1px solid rgba(148, 163, 184, 0.18);
}

.lang-btn {
  border: none;
  background: transparent;
  color: #475569;
  padding: 6px 10px;
  border-radius: 999px;
  cursor: pointer;
  font-size: 12px;
  font-weight: 600;
  transition: all 0.2s ease;

  &.active {
    background: #1d4ed8;
    color: #fff;
    box-shadow: 0 8px 18px rgba(37, 99, 235, 0.18);
  }
}

.initialize-header {
  margin-bottom: 28px;

  .title-block {
    display: flex;
    align-items: center;
    gap: 12px;
    margin-bottom: 10px;

    h1 {
      margin: 0;
      font-size: 34px;
      line-height: 1.2;
      color: #111827;
    }
  }

  p {
    margin: 0;
    color: #475569;
    font-size: 15px;
  }
}

.meta-pill {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  min-width: 56px;
  height: 26px;
  padding: 0 10px;
  border-radius: 999px;
  background: rgba(37, 99, 235, 0.08);
  color: #2563eb;
  font-size: 12px;
  font-weight: 700;
}

.option-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 18px;
  margin-bottom: 24px;
}

.mode-card {
  position: relative;
  width: 100%;
  padding: 24px 22px 20px;
  border: 1px solid rgba(148, 163, 184, 0.22);
  border-radius: 18px;
  background: #f8fafc;
  text-align: left;
  cursor: pointer;
  transition: all 0.2s ease;

  &:hover {
    border-color: rgba(59, 130, 246, 0.5);
    transform: translateY(-1px);
  }

  &.active {
    border-color: rgba(37, 99, 235, 0.8);
    background: rgba(37, 99, 235, 0.05);
    box-shadow: 0 14px 30px rgba(37, 99, 235, 0.08);
  }
}

.mode-badge {
  position: absolute;
  top: 14px;
  right: 14px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 28px;
  height: 28px;
  border-radius: 8px;
  background: rgba(37, 99, 235, 0.1);
  color: #1d4ed8;
  font-size: 12px;
  font-weight: 700;
}

.mode-label {
  margin: 18px 0 8px;
  color: #111827;
  font-size: 22px;
  font-weight: 700;
}

.mode-subtitle {
  color: #475569;
  font-size: 14px;
  line-height: 1.6;
}

.cluster-config {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 18px;
  margin: 8px 0 18px;
}

.config-row {
  display: flex;
  flex-direction: column;
  gap: 8px;

  label {
    color: #334155;
    font-size: 14px;
    font-weight: 600;
  }
}

.progress-panel {
  margin-top: 16px;
  padding: 18px 18px 12px;
  border-radius: 14px;
  background: rgba(15, 118, 110, 0.04);
  border: 1px solid rgba(16, 185, 129, 0.15);
}

.progress-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 12px;
  color: #0f172a;
  font-size: 14px;
  font-weight: 600;
}

.action-row {
  display: flex;
  justify-content: flex-end;
  margin-top: 24px;
}

@media (max-width: 900px) {
  .initialize-shell {
    grid-template-columns: 1fr;
  }

  .init-aside {
    padding-bottom: 20px;
  }
}

@media (max-width: 768px) {
  .option-grid,
  .cluster-config {
    grid-template-columns: 1fr;
  }

  .init-main {
    padding: 24px 20px;
  }
}
</style>
