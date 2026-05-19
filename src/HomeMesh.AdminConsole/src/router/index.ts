import { createRouter, createWebHistory } from 'vue-router';

import AdminAccessPage from '../views/AdminAccessPage.vue';
import AdminConsoleLayout from '../views/AdminConsoleLayout.vue';
import AdminDashboardPage from '../views/AdminDashboardPage.vue';
import AdminNetworkPage from '../views/AdminNetworkPage.vue';
import AdminProvidersPage from '../views/AdminProvidersPage.vue';

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
