<script setup lang="ts">
import { onMounted } from 'vue'
import { RouterLink, useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { useDashboardStore } from '@/stores/dashboard'
import { formatMmmDd } from '@/utils/display'

const router = useRouter()
const authStore = useAuthStore()
const dashboardStore = useDashboardStore()

onMounted(() => {
  dashboardStore.fetch()
})

async function handleLogout() {
  await authStore.logout()
  router.push('/login')
}
</script>

<template>
  <div>
    <h2>Dashboard</h2>
    <p class="text-muted">Welcome back, {{ dashboardStore.data?.username ?? authStore.username }}!</p>

    <div v-if="dashboardStore.data" class="row mt-4">
      <div class="col-md-4">
        <div class="card text-white bg-primary">
          <div class="card-body">
            <h5 class="card-title">Total Customers</h5>
            <p class="display-4">{{ dashboardStore.data.totalCustomers }}</p>
          </div>
        </div>
      </div>
      <div class="col-md-8">
        <div class="card">
          <div class="card-header">Customers by Status</div>
          <div class="card-body">
            <table class="table table-sm">
              <thead>
                <tr>
                  <th>Status</th>
                  <th>Count</th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="sc in dashboardStore.data.statusCounts" :key="sc.status">
                  <td>{{ sc.status }}</td>
                  <td>{{ sc.count }}</td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>
      </div>
    </div>

    <div v-if="dashboardStore.data" class="row mt-4">
      <div class="col-md-7">
        <div class="card">
          <div class="card-header">Recent Interactions</div>
          <div class="card-body">
            <p v-if="dashboardStore.data.recentInteractions.length === 0" class="text-muted">
              No recent interactions.
            </p>
            <ul v-else class="list-group list-group-flush">
              <li
                v-for="i in dashboardStore.data.recentInteractions"
                :key="i.id"
                class="list-group-item d-flex justify-content-between align-items-center"
              >
                <span>
                  <span class="badge badge-secondary mr-2">{{ i.type }}</span>{{ i.note }}
                </span>
                <small class="text-muted">{{ i.loggedByUsername }} &middot; {{ formatMmmDd(i.loggedAt) }}</small>
              </li>
            </ul>
          </div>
        </div>
      </div>
      <div class="col-md-5">
        <div class="card">
          <div class="card-header">Quick Access</div>
          <div class="card-body">
            <RouterLink to="/customers/create" class="btn btn-success btn-block mb-2">New Customer</RouterLink>
            <RouterLink to="/reports" class="btn btn-info btn-block mb-2">View Reports</RouterLink>
            <button type="button" class="btn btn-outline-danger btn-block" @click="handleLogout">Logout</button>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>
