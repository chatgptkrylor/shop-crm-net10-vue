import { createRouter, createWebHistory } from 'vue-router'
import { useAuthStore } from '@/stores/auth'

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    {
      path: '/login',
      name: 'login',
      component: () => import('@/views/LoginView.vue'),
      meta: { requiresAuth: false, title: 'Login' },
    },
    {
      path: '/dashboard',
      name: 'dashboard',
      component: () => import('@/views/DashboardView.vue'),
      meta: { requiresAuth: true, title: 'Dashboard' },
    },
    {
      path: '/customers',
      name: 'customers',
      component: () => import('@/views/CustomersIndexView.vue'),
      meta: { requiresAuth: true, title: 'Customers' },
    },
    {
      path: '/customers/create',
      name: 'customer-create',
      component: () => import('@/views/CustomerCreateView.vue'),
      meta: { requiresAuth: true, title: 'New Customer' },
    },
    {
      path: '/customers/:id',
      name: 'customer-details',
      component: () => import('@/views/CustomerDetailsView.vue'),
      meta: { requiresAuth: true, title: 'Customer Details' },
    },
    {
      path: '/customers/:id/edit',
      name: 'customer-edit',
      component: () => import('@/views/CustomerEditView.vue'),
      meta: { requiresAuth: true, title: 'Edit Customer' },
    },
    {
      path: '/reports',
      name: 'reports',
      component: () => import('@/views/ReportsView.vue'),
      meta: { requiresAuth: true, title: 'Reports' },
    },
    {
      path: '/',
      redirect: '/dashboard',
    },
    {
      path: '/:pathMatch(.*)*',
      name: 'not-found',
      component: () => import('@/views/NotFoundView.vue'),
      meta: { title: 'Error' },
    },
  ],
})

router.beforeEach(async (to) => {
  const authStore = useAuthStore()
  if (!authStore.isAuthenticated && to.meta.requiresAuth !== false && to.name !== 'not-found') {
    return { name: 'login', query: { redirect: to.fullPath } }
  }
  if (authStore.isAuthenticated && to.name === 'login') {
    return { name: 'dashboard' }
  }
})

router.afterEach((to) => {
  const title = typeof to.meta.title === 'string' ? to.meta.title : 'Tiny CRM'
  document.title = `${title} - Tiny CRM`
})

export default router
