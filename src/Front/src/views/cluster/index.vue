<script setup lang="ts">
import { onMounted, ref } from "vue";
import { useRouter } from "vue-router";
import { ElMessage } from "element-plus";
import { exitCluster, getClusterStatus } from "@/api/system";

const router = useRouter();
const clusterHealth = ref<any>(null);

const loadCluster = async () => {
  const result = await getClusterStatus();
  clusterHealth.value = result.data;
};

const handleExitCluster = async () => {
  const result = await exitCluster();
  if (result.success) {
    ElMessage.info("集群已退出，返回初始化页");
    router.push("/initialize");
  }
};

onMounted(() => {
  loadCluster();
});
</script>

<template>
  <div class="cluster-page">
    <div class="cluster-header">
      <div>
        <p class="eyebrow">Raft</p>
        <h2>分布式 Raft 集群</h2>
      </div>
      <el-button type="danger" plain @click="handleExitCluster"
        >退出集群</el-button
      >
    </div>

    <div class="cards">
      <div class="summary-card">
        <span class="label">当前角色</span>
        <strong>{{ clusterHealth?.role || "primary" }}</strong>
      </div>
      <div class="summary-card">
        <span class="label">健康状态</span>
        <strong
          class="status"
          :class="clusterHealth?.clusterHealth || 'healthy'"
        >
          {{ clusterHealth?.clusterHealth || "healthy" }}
        </strong>
      </div>
      <div class="summary-card">
        <span class="label">主节点地址</span>
        <strong>{{
          clusterHealth?.leaderFrontendUrl || "http://127.0.0.1:5173"
        }}</strong>
      </div>
    </div>

    <div class="node-panel">
      <h3>集群节点</h3>
      <el-table
        :data="clusterHealth?.clusterNodes || []"
        stripe
        style="width: 100%"
      >
        <el-table-column prop="id" label="节点 ID" />
        <el-table-column prop="role" label="角色" />
        <el-table-column prop="address" label="地址" />
        <el-table-column prop="port" label="端口" />
        <el-table-column prop="status" label="状态" />
      </el-table>
    </div>
  </div>
</template>

<style scoped lang="scss">
.cluster-page {
  padding: 24px;
}

.cluster-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 22px;
}

.eyebrow {
  margin: 0 0 8px;
  letter-spacing: 0.2em;
  text-transform: uppercase;
  color: #7c8aa5;
  font-size: 12px;
}

h2 {
  margin: 0;
  font-size: 28px;
}

.cards {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(220px, 1fr));
  gap: 16px;
  margin-bottom: 24px;
}

.summary-card {
  background: #fff;
  border: 1px solid #e5e7eb;
  border-radius: 18px;
  padding: 18px;
  display: flex;
  flex-direction: column;
  gap: 8px;
  box-shadow: 0 10px 24px rgba(15, 23, 42, 0.04);
}

.label {
  color: #6b7280;
  font-size: 12px;
}

.status {
  text-transform: capitalize;
}

.status.healthy {
  color: #16a34a;
}

.status.warning {
  color: #d97706;
}

.status.degraded {
  color: #dc2626;
}

.node-panel {
  background: #fff;
  border: 1px solid #e5e7eb;
  border-radius: 18px;
  padding: 18px;
}

h3 {
  margin: 0 0 16px;
}
</style>
