<script setup lang="ts">
import { computed, onMounted, watch } from 'vue'
import { RouterLink, useRoute } from 'vue-router'
import { useCustomersStore } from '@/stores/customers'
import { statusBadgeClass } from '@/utils/display'

const route = useRoute()
const customersStore = useCustomersStore()
const currentPage = computed(() => Number(route.query.page) || 1)

onMounted(() => {
  customersStore.fetchAll(currentPage.value)
})

watch(currentPage, (page) => {
  customersStore.fetchAll(page)
})
</script>

<template>
  <div>
    <div class="d-flex justify-content-between align-items-center mb-3">
      <h2>Customers</h2>
      <RouterLink to="/customers/create" class="btn btn-success">New Customer</RouterLink>
    </div>

    <table class="table table-striped table-hover">
      <thead class="thead-dark">
        <tr>
          <th>Name</th>
          <th>Email</th>
          <th>Phone</th>
          <th>Company</th>
          <th>Status</th>
          <th>Actions</th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="c in customersStore.list" :key="c.id">
          <td>{{ c.name }}</td>
          <td>{{ c.email }}</td>
          <td>{{ c.phone }}</td>
          <td>{{ c.company }}</td>
          <td>
            <span :class="statusBadgeClass(c.status)">{{ c.status }}</span>
          </td>
          <td>
            <RouterLink :to="`/customers/${c.id}`" class="btn btn-sm btn-info">Details</RouterLink>
            <RouterLink :to="`/customers/${c.id}/edit`" class="btn btn-sm btn-warning ml-1">Edit</RouterLink>
          </td>
        </tr>
      </tbody>
    </table>

    <nav v-if="customersStore.pagination && customersStore.pagination.totalPages > 1">
      <ul class="pagination justify-content-center">
        <li
          v-for="i in customersStore.pagination.totalPages"
          :key="i"
          class="page-item"
          :class="{ active: i === currentPage }"
        >
          <RouterLink class="page-link" :to="{ name: 'customers', query: { page: i } }">{{ i }}</RouterLink>
        </li>
      </ul>
    </nav>
  </div>
</template>
