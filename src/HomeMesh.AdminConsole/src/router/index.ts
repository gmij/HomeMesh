import { createRouter, createWebHistory } from 'vue-router';

const AdminAccessPage = () => import('../views/AdminAccessPage.vue');
const AdminConsoleLayout = () => import('../views/AdminConsoleLayout.vue');
const AdminDashboardPage = () => import('../views/AdminDashboardPage.vue');
const AdminNetworkDetailPage = () => import('../views/AdminNetworkDetailPage.vue');
const AdminNetworkPage = () => import('../views/AdminNetworkPage.vue');
const AdminProvidersPage = () => import('../views/AdminProvidersPage.vue');

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    {
      path: '/',
      component: AdminConsoleLayout,
      children: [
        {
          path: '',
          redirect: '/dashboard'
        },
        {
          path: 'dashboard',
          name: 'admin-dashboard',
          component: AdminDashboardPage,
          meta: {
            section: 'dashboard'
          }
        },
        {
          path: 'network',
          name: 'admin-network',
          component: AdminNetworkPage,
          meta: {
            section: 'network'
          }
        },
        {
          path: 'network/:networkId',
          name: 'admin-network-detail',
          component: AdminNetworkDetailPage,
          meta: {
            section: 'network'
          }
        },
        {
          path: 'access',
          name: 'admin-access',
          component: AdminAccessPage,
          meta: {
            section: 'access'
          }
        },
        {
          path: 'providers',
          name: 'admin-providers',
          component: AdminProvidersPage,
          meta: {
            section: 'providers'
          }
        }
      ]
    }
  ]
});

export default router;
