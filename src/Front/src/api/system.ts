import { http } from "@/utils/http";

export type ClusterNodeStatus = {
  id: string;
  role: "leader" | "follower";
  address: string;
  port: number;
  status: "healthy" | "warning" | "degraded";
};

export type DefaultUserInfo = {
  username: string;
  password: string;
};

export type DatabaseConfig = {
  type: "Sqlite" | "MySql" | "PostgreSQL" | "SqlServer";
  host?: string;
  port?: number;
  databaseName?: string;
  username?: string;
  password?: string;
};

export type SystemInitStatusResponse = {
  success: boolean;
  data: {
    initialized: boolean;
    mode?: "primary" | "secondary";
    role?: "primary" | "secondary" | "leader" | "follower";
    clusterAddress?: string;
    clusterPort?: number;
    leaderFrontendUrl?: string;
    clusterHealth?: "healthy" | "warning" | "degraded";
    clusterNodes?: ClusterNodeStatus[];
    defaultUser?: DefaultUserInfo;
    database?: DatabaseConfig;
    message?: string;
  };
};

export type SystemInitializePayload = {
  mode: "primary" | "secondary";
  role?: "leader" | "follower";
  clusterAddress?: string;
  clusterPort?: number;
  database?: DatabaseConfig;
};

const STORAGE_KEY = "linker:system-init";
const DEFAULT_USER: DefaultUserInfo = {
  username: "admin",
  password: "admin123"
};

const getMockClusterNodes = (mode: "primary" | "secondary") => {
  const leader = {
    id: "node-primary",
    role: "leader" as const,
    address: "10.0.0.10",
    port: 8001,
    status: "healthy" as const
  };

  if (mode === "primary") {
    return [leader, {
      id: "node-follower-1",
      role: "follower" as const,
      address: "10.0.0.11",
      port: 8002,
      status: "healthy" as const
    }];
  }

  return [leader, {
    id: "node-follower-local",
    role: "follower" as const,
    address: "127.0.0.1",
    port: 5173,
    status: "healthy" as const
  }];
};

const readSystemInitState = () => {
  try {
    const raw = localStorage.getItem(STORAGE_KEY);
    if (!raw) {
      return null;
    }

    return JSON.parse(raw) as SystemInitStatusResponse["data"];
  } catch {
    return null;
  }
};

const writeSystemInitState = (data: SystemInitStatusResponse["data"]) => {
  localStorage.setItem(STORAGE_KEY, JSON.stringify(data));
};

const buildInitResponse = (data: Partial<SystemInitStatusResponse["data"]> = {}) => {
  const normalized = {
    initialized: false,
    mode: "primary",
    role: "primary",
    clusterAddress: "10.0.0.10",
    clusterPort: 8001,
    leaderFrontendUrl: "http://127.0.0.1:5173",
    clusterHealth: "healthy",
    clusterNodes: getMockClusterNodes("primary"),
    defaultUser: DEFAULT_USER,
    database: {
      type: "Sqlite" as const,
      host: "127.0.0.1",
      port: 3306,
      databaseName: "linker",
      username: "root",
      password: "root"
    },
    message: "mock: system not initialized",
    ...data
  };

  return {
    success: true,
    data: normalized
  } as SystemInitStatusResponse;
};

export const checkSystemInitStatus = async () => {
  try {
    const persisted = readSystemInitState();
    if (persisted) {
      return buildInitResponse(persisted);
    }

    return await http.request<SystemInitStatusResponse>("get", "/system/init-status");
  } catch {
    return buildInitResponse();
  }
};

export const initializeSystem = async (payload: SystemInitializePayload) => {
  try {
    const currentDatabase: DatabaseConfig = {
      type: payload.database?.type || "Sqlite",
      host: payload.database?.host || "127.0.0.1",
      port: payload.database?.port || 3306,
      databaseName: payload.database?.databaseName || "linker",
      username: payload.database?.username || "root",
      password: payload.database?.password || "root"
    };

    const normalizedRole = payload.role || (payload.mode === "secondary" ? "follower" : "leader");
    const shouldPersistDatabase = normalizedRole === "leader";
    const requestBody = {
      mode: payload.mode,
      role: normalizedRole,
      clusterAddress: payload.clusterAddress || "10.0.0.10",
      clusterPort: payload.clusterPort || 8001,
      defaultUserName: DEFAULT_USER.username,
      defaultPassword: DEFAULT_USER.password,
      databaseType: shouldPersistDatabase ? currentDatabase.type : "Sqlite",
      databaseHost: shouldPersistDatabase ? currentDatabase.host : undefined,
      databasePort: shouldPersistDatabase ? currentDatabase.port : undefined,
      databaseName: shouldPersistDatabase ? currentDatabase.databaseName : "linker",
      databaseUsername: shouldPersistDatabase ? currentDatabase.username : undefined,
      databasePassword: shouldPersistDatabase ? currentDatabase.password : undefined
    };

    const result = await http.request<SystemInitStatusResponse>("post", "/system/init", { data: requestBody });

    const state = buildInitResponse({
      initialized: true,
      mode: payload.mode,
      role: normalizedRole,
      clusterAddress: payload.clusterAddress || "10.0.0.10",
      clusterPort: payload.clusterPort || 8001,
      leaderFrontendUrl:
        payload.mode === "secondary"
          ? `http://${payload.clusterAddress || "10.0.0.10"}:${payload.clusterPort || 8001}`
          : "http://127.0.0.1:5173",
      clusterHealth: "healthy",
      clusterNodes: getMockClusterNodes(payload.mode),
      defaultUser: DEFAULT_USER,
      database: shouldPersistDatabase ? currentDatabase : undefined,
      message: result?.data?.message || "mock: system initialized"
    });

    writeSystemInitState(state.data);
    return state;
  } catch {
    const state = buildInitResponse({
      initialized: true,
      message: "mock: system initialized"
    });
    writeSystemInitState(state.data);
    return state;
  }
};

export const getClusterStatus = async () => {
  const persisted = readSystemInitState();
  if (!persisted) {
    return buildInitResponse();
  }

  return buildInitResponse(persisted);
};

export const exitCluster = async () => {
  const resetState = buildInitResponse({
    initialized: false,
    mode: "primary",
    role: "primary",
    clusterAddress: "10.0.0.10",
    clusterPort: 8001,
    leaderFrontendUrl: "http://127.0.0.1:5173",
    clusterHealth: "healthy",
    clusterNodes: getMockClusterNodes("primary"),
    message: "mock: cluster exited"
  });

  writeSystemInitState(resetState.data);
  return resetState;
};
