<script setup lang="ts">
import { onMounted } from 'vue'
import { useReportsStore } from '@/stores/reports'

const reportsStore = useReportsStore()

onMounted(() => {
  reportsStore.fetch()
})

function pct(count: number): number {
  const total = reportsStore.data?.totalCustomers ?? 0
  return total > 0 ? Math.floor((count * 100) / total) : 0
}
</script>

<template>
  <div>
    <h2>Customer Report</h2>
    <p class="text-muted">Total customers: {{ reportsStore.data?.totalCustomers ?? 0 }}</p>

    <div class="card">
      <div class="card-header">Customers by Status</div>
      <div class="card-body">
        <table class="table">
          <thead>
            <tr>
              <th>Status</th>
              <th>Count</th>
              <th>Distribution</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="sc in reportsStore.data?.statusCounts ?? []" :key="sc.status">
              <td>{{ sc.status }}</td>
              <td>{{ sc.count }}</td>
              <td>
                <div class="progress" style="height: 20px;">
                  <div
                    class="progress-bar"
                    role="progressbar"
                    :style="{ width: pct(sc.count) + '%' }"
                    :aria-valuenow="sc.count"
                    aria-valuemin="0"
                    :aria-valuemax="reportsStore.data?.totalCustomers ?? 0"
                  >
                    {{ pct(sc.count) }}%
                  </div>
                </div>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>
  </div>
</template>
