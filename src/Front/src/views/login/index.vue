<script setup lang="ts">
import Motion from "./utils/motion";
import { useRouter } from "vue-router";
import { message } from "@/utils/message";
import { loginRules } from "./utils/rule";
import { computed, ref, reactive } from "vue";
import { debounce } from "@pureadmin/utils";
import { useEventListener } from "@vueuse/core";
import type { FormInstance } from "element-plus";
import { useLayout } from "@/layout/hooks/useLayout";
import { useUserStoreHook } from "@/store/modules/user";
import { initRouter, getTopMenu } from "@/router/utils";
import { useRenderIcon } from "@/components/ReIcon/src/hooks";
import { useDataThemeChange } from "@/layout/hooks/useDataThemeChange";
import { locale, setLocale } from "@/i18n";

import dayIcon from "@/assets/svg/day.svg?component";
import darkIcon from "@/assets/svg/dark.svg?component";
import Lock from "~icons/ri/lock-fill";
import User from "~icons/ri/user-3-fill";

defineOptions({
  name: "Login"
});

const router = useRouter();
const loading = ref(false);
const disabled = ref(false);
const ruleFormRef = ref<FormInstance>();

const { initStorage } = useLayout();
initStorage();

const { dataTheme, overallStyle, dataThemeChange } = useDataThemeChange();
dataThemeChange(overallStyle.value);

const pageText = computed(() => {
  if (locale.value === "en") {
    return {
      title: "Service Governance Center",
      subtitle: "Unified service registry, configuration management, and job scheduling platform.",
      username: "Username",
      password: "Password",
      login: "Sign in",
      mock: "Mock mode ready",
      brand: "Linker",
      featureTitle: "Platform capabilities",
      features: [
        "Service discovery and registry",
        "Config center and JSON editor",
        "Scheduled task management",
        "Multi-language support"
      ],
      hint: "Demo account: admin / admin123"
    };
  }

  return {
    title: "服务治理中心",
    subtitle: "统一管理服务注册发现、配置中心和定时任务的运维平台。",
    username: "账号",
    password: "密码",
    login: "登录",
    mock: "Mock 模式已就绪",
    brand: "Linker",
    featureTitle: "平台能力",
    features: [
      "服务注册发现",
      "配置中心与 JSON 编辑",
      "定时任务管理",
      "中英双语支持"
    ],
    hint: "演示账号：admin / admin123"
  };
});

const ruleForm = reactive({
  username: "admin",
  password: "admin123"
});

const onLogin = async (formEl: FormInstance | undefined) => {
  if (!formEl) return;
  await formEl.validate(valid => {
    if (valid) {
      loading.value = true;
      useUserStoreHook()
        .loginByUsername({
          username: ruleForm.username,
          password: ruleForm.password
        })
        .then(res => {
          if (res.success) {
            return initRouter().then(() => {
              disabled.value = true;
              router
                .push(getTopMenu(true).path)
                .then(() => {
                  message(locale.value === "en" ? "Login successful" : "登录成功", {
                    type: "success"
                  });
                })
                .finally(() => (disabled.value = false));
            });
          } else {
            message(locale.value === "en" ? "Login failed" : "登录失败", {
              type: "error"
            });
          }
        })
        .finally(() => (loading.value = false));
    }
  });
};

const immediateDebounce: any = debounce(
  formRef => onLogin(formRef),
  1000,
  true
);

useEventListener(document, "keydown", ({ code }) => {
  if (["Enter", "NumpadEnter"].includes(code) && !disabled.value && !loading.value) {
    immediateDebounce(ruleFormRef.value);
  }
});
</script>

<template>
  <div class="login-page">
    <div class="login-shell">
      <div class="brand-panel">
        <div class="brand-row">
          <div class="brand-mark">L</div>
          <div class="brand-text">{{ pageText.brand }}</div>
        </div>

        <div class="brand-copy">
          <div class="badge">{{ pageText.mock }}</div>
          <h1>{{ pageText.title }}</h1>
          <p>{{ pageText.subtitle }}</p>
        </div>

        <div class="feature-list">
          <div v-for="item in pageText.features" :key="item" class="feature-item">
            <span class="feature-dot" />
            <span>{{ item }}</span>
          </div>
        </div>

        <div class="panel-footer">
          <span>{{ pageText.featureTitle }}</span>
          <span>{{ pageText.hint }}</span>
        </div>
      </div>

      <div class="form-panel">
        <div class="topbar-right">
          <el-switch
            v-model="dataTheme"
            inline-prompt
            :active-icon="dayIcon"
            :inactive-icon="darkIcon"
            @change="dataThemeChange"
          />

          <div class="language-switch" aria-label="language switch">
            <button
              class="lang-btn"
              :class="{ active: locale === 'zh' }"
              type="button"
              @click="setLocale('zh')"
            >
              中文
            </button>
            <button
              class="lang-btn"
              :class="{ active: locale === 'en' }"
              type="button"
              @click="setLocale('en')"
            >
              EN
            </button>
          </div>
        </div>

        <div class="form-card">
          <Motion>
            <div class="form-title">{{ pageText.login }}</div>
          </Motion>

          <el-form ref="ruleFormRef" :model="ruleForm" :rules="loginRules" size="large">
            <Motion :delay="100">
              <el-form-item
                :rules="[{ required: true, message: locale === 'en' ? 'Please enter your username' : '请输入账号', trigger: 'blur' }]"
                prop="username"
              >
                <el-input v-model="ruleForm.username" clearable :placeholder="pageText.username" :prefix-icon="useRenderIcon(User)" />
              </el-form-item>
            </Motion>

            <Motion :delay="150">
              <el-form-item prop="password">
                <el-input v-model="ruleForm.password" clearable show-password :placeholder="pageText.password" :prefix-icon="useRenderIcon(Lock)" />
              </el-form-item>
            </Motion>

            <Motion :delay="200">
              <el-button class="submit-btn" size="large" type="primary" :loading="loading" :disabled="disabled" @click="onLogin(ruleFormRef)">
                {{ pageText.login }}
              </el-button>
            </Motion>
          </el-form>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped lang="scss">
.login-page {
  min-height: 100vh;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 32px;
  background:
    radial-gradient(circle at top left, rgba(59, 130, 246, 0.18), transparent 28%),
    radial-gradient(circle at bottom right, rgba(168, 85, 247, 0.18), transparent 30%),
    linear-gradient(135deg, #eef4ff 0%, #f8fafc 36%, #eef2ff 100%);
}

.login-shell {
  width: min(1200px, 100%);
  min-height: 760px;
  display: grid;
  grid-template-columns: 1.15fr 0.85fr;
  border-radius: 28px;
  overflow: hidden;
  border: 1px solid rgba(148, 163, 184, 0.18);
  background: rgba(255, 255, 255, 0.72);
  backdrop-filter: blur(20px);
  box-shadow: 0 30px 80px rgba(15, 23, 42, 0.12);
}

.brand-panel {
  position: relative;
  padding: 38px 36px 26px;
  background:
    linear-gradient(135deg, rgba(15, 23, 42, 0.96), rgba(30, 64, 175, 0.82)),
    linear-gradient(135deg, rgba(37, 99, 235, 0.8), rgba(124, 58, 237, 0.7));
  color: #fff;
  display: flex;
  flex-direction: column;
  justify-content: space-between;
}

.brand-row {
  display: flex;
  align-items: center;
  gap: 14px;
}

.brand-mark {
  width: 48px;
  height: 48px;
  display: grid;
  place-items: center;
  border-radius: 14px;
  background: rgba(255, 255, 255, 0.12);
  border: 1px solid rgba(255, 255, 255, 0.16);
  font-size: 24px;
  font-weight: 700;
}

.brand-text {
  font-size: 1.4rem;
  font-weight: 700;
}

.brand-copy {
  margin-top: 24px;
}

.badge {
  display: inline-flex;
  align-items: center;
  padding: 6px 10px;
  border-radius: 999px;
  background: rgba(255, 255, 255, 0.1);
  border: 1px solid rgba(255, 255, 255, 0.15);
  font-size: 12px;
  font-weight: 600;
  letter-spacing: 0.08em;
  text-transform: uppercase;
}

.brand-copy h1 {
  margin: 18px 0 12px;
  font-size: clamp(2.2rem, 3vw, 3.5rem);
  line-height: 1.1;
  font-weight: 800;
}

.brand-copy p {
  margin: 0;
  max-width: 540px;
  font-size: 1rem;
  line-height: 1.8;
  color: rgba(255, 255, 255, 0.8);
}

.feature-list {
  display: flex;
  flex-direction: column;
  gap: 14px;
  margin-top: 28px;
}

.feature-item {
  display: flex;
  align-items: center;
  gap: 10px;
  color: rgba(255, 255, 255, 0.9);
  font-size: 0.96rem;
}

.feature-dot {
  width: 10px;
  height: 10px;
  border-radius: 50%;
  background: linear-gradient(135deg, #67e8f9, #60a5fa);
  box-shadow: 0 0 0 6px rgba(103, 232, 249, 0.18);
}

.panel-footer {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  margin-top: 26px;
  padding-top: 18px;
  border-top: 1px solid rgba(255, 255, 255, 0.14);
  color: rgba(255, 255, 255, 0.76);
  font-size: 0.78rem;
  line-height: 1.6;
}

.form-panel {
  display: flex;
  flex-direction: column;
  justify-content: center;
  background: rgba(255, 255, 255, 0.72);
  padding: 24px 32px 30px;
}

.topbar-right {
  display: flex;
  align-items: center;
  justify-content: flex-end;
  gap: 12px;
  margin-bottom: 18px;
}

.language-switch {
  display: inline-flex;
  align-items: center;
  padding: 4px;
  border-radius: 999px;
  background: rgba(148, 163, 184, 0.08);
  border: 1px solid rgba(148, 163, 184, 0.14);
}

.lang-btn {
  border: 0;
  background: transparent;
  color: #475569;
  padding: 7px 12px;
  border-radius: 999px;
  font-size: 12px;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.2s ease;

  &.active {
    background: #fff;
    color: #111827;
    box-shadow: 0 4px 14px rgba(15, 23, 42, 0.08);
  }
}

.form-card {
  width: min(420px, 100%);
  margin: 0 auto;
  padding: 18px 0 0;
}

.form-title {
  margin-bottom: 24px;
  font-size: 2rem;
  font-weight: 800;
  color: #0f172a;
}

:deep(.el-form-item) {
  margin-bottom: 18px;
}

:deep(.el-input__wrapper) {
  border-radius: 14px;
  background: rgba(248, 250, 252, 0.9);
  box-shadow: none;
  border: 1px solid rgba(148, 163, 184, 0.2);
  padding: 0 14px;
  height: 48px;
}

:deep(.el-input__inner) {
  font-size: 15px;
}

.submit-btn {
  width: 100%;
  margin-top: 10px;
  height: 48px;
  border-radius: 14px;
  font-weight: 700;
  letter-spacing: 0.02em;
}

@media (max-width: 980px) {
  .login-shell {
    grid-template-columns: 1fr;
    min-height: auto;
  }

  .brand-panel {
    min-height: 360px;
  }
}

@media (max-width: 620px) {
  .login-page {
    padding: 18px;
  }

  .brand-panel,
  .form-panel {
    padding-left: 20px;
    padding-right: 20px;
  }

  .topbar-right {
    justify-content: space-between;
  }
}
</style>
