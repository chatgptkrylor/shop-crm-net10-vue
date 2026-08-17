<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue'
import { RouterLink, useRoute, useRouter } from 'vue-router'
import { useCustomersStore } from '@/stores/customers'

const router = useRouter()
const route = useRoute()
const customersStore = useCustomersStore()
const customerId = Number(route.params.id)

const form = reactive({
  name: '',
  email: '',
  phone: '',
  company: '',
  status: 'Lead',
})
const nameError = ref('')
const submitted = ref(false)

onMounted(async () => {
  await customersStore.fetchOne(customerId)
  if (customersStore.current) {
    form.name = customersStore.current.name
    form.email = customersStore.current.email ?? ''
    form.phone = customersStore.current.phone ?? ''
    form.company = customersStore.current.company ?? ''
    form.status = customersStore.current.status
  }
})

async function handleSubmit() {
  submitted.value = true
  nameError.value = form.name.trim() ? '' : 'Name is required'
  if (nameError.value) return
  try {
    await customersStore.update(customerId, { ...form })
    router.push('/customers')
  } catch (error: unknown) {
    const err = error as { response?: { data?: { errors?: Record<string, string[]> } } }
    const errors = err.response?.data?.errors
    if (errors?.Name?.[0]) {
      nameError.value = errors.Name[0]
    } else if (errors?.name?.[0]) {
      nameError.value = errors.name[0]
    } else {
      nameError.value = 'Failed to update customer'
    }
  }
}
</script>

<template>
  <div>
    <h2>Edit Customer</h2>

    <form class="needs-validation" @submit.prevent="handleSubmit">
      <div class="form-group">
        <label for="name">Name</label>
        <input id="name" v-model="form.name" type="text" class="form-control" />
        <span v-if="submitted && nameError" class="text-danger">{{ nameError }}</span>
      </div>
      <div class="form-group">
        <label for="email">Email</label>
        <input id="email" v-model="form.email" type="text" class="form-control" />
      </div>
      <div class="form-group">
        <label for="phone">Phone</label>
        <input id="phone" v-model="form.phone" type="text" class="form-control" />
      </div>
      <div class="form-group">
        <label for="company">Company</label>
        <input id="company" v-model="form.company" type="text" class="form-control" />
      </div>
      <div class="form-group">
        <label for="status">Status</label>
        <select id="status" v-model="form.status" class="form-control">
          <option value="Lead">Lead</option>
          <option value="Contact">Contact</option>
          <option value="Customer">Customer</option>
        </select>
      </div>
      <button type="submit" class="btn btn-warning">Save</button>
      <RouterLink to="/customers" class="btn btn-secondary ml-1">Cancel</RouterLink>
    </form>
  </div>
</template>
