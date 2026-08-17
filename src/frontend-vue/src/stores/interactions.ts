import { defineStore } from 'pinia'
import { ref } from 'vue'
import * as interactionsApi from '@/api/interactions'
import type { InteractionDto } from '@/api/dashboard'

export const useInteractionsStore = defineStore('interactions', () => {
  const list = ref<InteractionDto[]>([])
  const loading = ref(false)

  async function fetchByCustomer(customerId: number) {
    loading.value = true
    try {
      list.value = await interactionsApi.getCustomerInteractions(customerId)
    } finally {
      loading.value = false
    }
  }

  async function create(customerId: number, data: { type: string; note: string }) {
    const created = await interactionsApi.createInteraction({
      customerId,
      type: data.type,
      note: data.note,
    })
    await fetchByCustomer(customerId)
    return created
  }

  return { list, loading, fetchByCustomer, create }
})
