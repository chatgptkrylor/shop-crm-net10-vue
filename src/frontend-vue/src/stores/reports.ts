import { defineStore } from 'pinia'
import { ref } from 'vue'
import * as reportsApi from '@/api/reports'
import type { ReportDto } from '@/api/reports'

export const useReportsStore = defineStore('reports', () => {
  const data = ref<ReportDto | null>(null)
  const loading = ref(false)

  async function fetch() {
    loading.value = true
    try {
      data.value = await reportsApi.getReports()
    } finally {
      loading.value = false
    }
  }

  return { data, loading, fetch }
})
