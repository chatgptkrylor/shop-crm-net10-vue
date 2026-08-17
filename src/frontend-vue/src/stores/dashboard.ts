import { defineStore } from 'pinia'
import { ref } from 'vue'
import * as dashboardApi from '@/api/dashboard'
import type { DashboardDto } from '@/api/dashboard'

export const useDashboardStore = defineStore('dashboard', () => {
  const data = ref<DashboardDto | null>(null)
  const loading = ref(false)

  async function fetch() {
    loading.value = true
    try {
      data.value = await dashboardApi.getDashboard()
    } finally {
      loading.value = false
    }
  }

  return { data, loading, fetch }
})