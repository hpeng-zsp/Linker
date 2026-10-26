const { VITE_HIDE_HOME } = import.meta.env;
const Layout = () => import("@/layout/index.vue");

export default {
  path: "/",
  name: "Home",
  component: Layout,
  redirect: "/welcome",
  meta: {
    icon: "ep/home-filled",
    title: "首页",
    rank: 0
  },
  children: [
    {
      path: "/welcome",
      name: "Welcome",
      component: () => import("@/views/welcome/index.vue"),
      meta: {
        title: "首页",
        showLink: VITE_HIDE_HOME === "true" ? false : true
      }
    },
    {
      path: "/account-management",
      name: "AccountManagement",
      component: () => import("@/views/account-management/index.vue"),
      meta: {
        title: "账户管理",
        showLink: false
      }
    },
    {
      path: "/notifications",
      name: "Notifications",
      component: () => import("@/views/notifications/index.vue"),
      meta: {
        title: "通知列表",
        showLink: false
      }
    }
  ]
} satisfies RouteConfigsTable;
