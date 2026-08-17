import { defineStore } from 'pinia'
import { ref } from 'vue'
import * as customersApi from '@/api/customers'
import type { CustomerDto, PagedResult } from '@/api/customers'

export const useCustomersStore = defineStore('customers', () => {
  const list = ref<CustomerDto[]>([])
  const pagination = ref<PagedResult<CustomerDto> | null>(null)
  const current = ref<CustomerDto | null>(null)
  const loading = ref(false)

  async function fetchAll(page: number = 1) {
    loading.value = true
    try {
      const result = await customersApi.getCustomers(page)
      list.value = result.items
      pagination.value = result
    } finally {
      loading.value = false
    }
  }

  async function fetchOne(id: number) {
    loading.value = true
    try {
      current.value = await customersApi.getCustomer(id)
    } finally {
      loading.value = false
    }
  }

  async function create(data: Omit<CustomerDto, 'id' | 'createdAt'>) {
    return await customersApi.createCustomer(data)
  }

  async function update(id: number, data: Omit<CustomerDto, 'id' | 'createdAt'>) {
    return await customersApi.updateCustomer(id, data)
  }

  async function remove(id: number) {
    await customersApi.deleteCustomer(id)
    await fetchAll(pagination.value?.page ?? 1)
  }

  return { list, pagination, current, loading, fetchAll, fetchOne, create, update, remove }
})