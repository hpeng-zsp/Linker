import { http } from "@/utils/http";

export type SystemInitStatusResponse = {
  success: boolean;
  data: {
    initialized: boolean;
    message?: string;
  };
};

export type SystemInitializePayload = {
  mode: "primary" | "secondary";
  clusterAddress?: string;
  clusterPort?: number;
};

export const checkSystemInitStatus = async () => {
  try {
    return await http.request<SystemInitStatusResponse>("get", "/system/init-status");
  } catch {
    return {
      success: true,
      data: {
        initialized: false,
        message: "mock: system not initialized"
      }
    } as SystemInitStatusResponse;
  }
};

export const initializeSystem = async (payload: SystemInitializePayload) => {
  try {
    return await http.request<SystemInitStatusResponse>("post", "/system/init", {
      data: payload
    });
  } catch {
    return {
      success: true,
      data: {
        initialized: true,
        message: "mock: system initialized"
      }
    } as SystemInitStatusResponse;
  }
};
