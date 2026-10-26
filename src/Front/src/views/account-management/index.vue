<script setup lang="ts">
import { computed, ref } from "vue";
import { useRouter } from "vue-router";
import { ElMessage } from "element-plus";
import { storageLocal } from "@pureadmin/utils";
import { userKey } from "@/utils/auth";
import { useUserStoreHook } from "@/store/modules/user";

const router = useRouter();
const userStore = useUserStoreHook();

const username = ref(userStore.username || "");
const nickname = ref(userStore.nickname || "");
const password = ref("");
const confirmPassword = ref("");

const accountTitle = computed(() => username.value || "当前用户");

function updateLocalUserInfo() {
  const storedUser = storageLocal().getItem(userKey) || {};
  storageLocal().setItem(userKey, {
    ...storedUser,
    username: username.value.trim(),
    nickname: nickname.value.trim() || username.value.trim(),
    password: password.value || storedUser.password || ""
  });
}

function saveAccount() {
  const trimmedUsername = username.value.trim();

  if (!trimmedUsername) {
    ElMessage.warning("请输入用户名");
    return;
  }

  if (password.value && password.value.length < 6) {
    ElMessage.warning("密码长度不能少于 6 位");
    return;
  }

  if (password.value && password.value !== confirmPassword.value) {
    ElMessage.warning("两次输入的密码不一致");
    return;
  }

  const nextNickname = nickname.value.trim() || trimmedUsername;
  userStore.SET_USERNAME(trimmedUsername);
  userStore.SET_NICKNAME(nextNickname);
  updateLocalUserInfo();

  if (password.value) {
    ElMessage.success("账号信息已更新，密码已修改");
  } else {
    ElMessage.success("账号信息已更新");
  }
}
</script>

<template>
  <div class="account-page">
    <el-card class="account-card" shadow="never">
      <template #header>
        <div class="account-header">
          <span>账户管理</span>
          <el-tag type="primary">{{ accountTitle }}</el-tag>
        </div>
      </template>

      <el-form label-position="top" class="account-form">
        <el-form-item label="用户名">
          <el-input v-model="username" placeholder="请输入用户名" clearable />
        </el-form-item>

        <el-form-item label="昵称">
          <el-input v-model="nickname" placeholder="请输入昵称" clearable />
        </el-form-item>

        <el-form-item label="新密码">
          <el-input
            v-model="password"
            type="password"
            placeholder="请输入新密码，不修改可留空"
            show-password
            clearable
          />
        </el-form-item>

        <el-form-item label="确认密码">
          <el-input
            v-model="confirmPassword"
            type="password"
            placeholder="请再次输入新密码"
            show-password
            clearable
          />
        </el-form-item>
      </el-form>

      <div class="account-actions">
        <el-button @click="router.back()">返回</el-button>
        <el-button type="primary" @click="saveAccount">保存修改</el-button>
      </div>
    </el-card>
  </div>
</template>

<style scoped lang="scss">
.account-page {
  display: flex;
  justify-content: center;
  padding: 24px;
}

.account-card {
  width: min(720px, 100%);
  border-radius: 16px;
  border: 1px solid rgba(148, 163, 184, 0.2);
  background: #fff;
}

.account-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  font-size: 18px;
  font-weight: 600;
}

.account-form {
  margin-top: 8px;
}

.account-actions {
  display: flex;
  justify-content: flex-end;
  gap: 12px;
  margin-top: 24px;
}
</style>
