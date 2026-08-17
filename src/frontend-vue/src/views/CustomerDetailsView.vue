<script setup lang="ts">
import { onMounted, reactive } from 'vue'
import { RouterLink, useRoute } from 'vue-router'
import { useCustomersStore } from '@/stores/customers'
import { useInteractionsStore } from '@/stores/interactions'
import { formatMmmDdYyyy, formatMmmDdYyyyHm, statusBadgeClass } from '@/utils/display'

const route = useRoute()
const customersStore = useCustomersStore()
const interactionsStore = useInteractionsStore()
const customerId = Number(route.params.id)

const form = reactive({
  type: 'Call',
  note: '',
})

onMounted(async () => {
  await Promise.all([
    customersStore.fetchOne(customerId),
    interactionsStore.fetchByCustomer(customerId),
  ])
})

async function handleLog() {
  if (!form.note.trim()) return
  await interactionsStore.create(customerId, { type: form.type, note: form.note.trim() })
  form.type = 'Call'
  form.note = ''
}
</script>

<template>
  <div v-if="customersStore.current">
    <div class="d-flex justify-content-between align-items-center mb-3">
      <h2>{{ customersStore.current.name }}</h2>
      <div>
        <RouterLink :to="`/customers/${customerId}/edit`" class="btn btn-warning btn-sm">Edit</RouterLink>
      </div>
    </div>

    <div class="card mb-4">
      <div class="card-header">Customer Info</div>
      <div class="card-body">
        <dl class="row mb-0">
          <dt class="col-sm-3">Email</dt>
          <dd class="col-sm-9">{{ customersStore.current.email }}</dd>
          <dt class="col-sm-3">Phone</dt>
          <dd class="col-sm-9">{{ customersStore.current.phone }}</dd>
          <dt class="col-sm-3">Company</dt>
          <dd class="col-sm-9">{{ customersStore.current.company }}</dd>
          <dt class="col-sm-3">Status</dt>
          <dd class="col-sm-9">
            <span :class="statusBadgeClass(customersStore.current.status)">{{ customersStore.current.status }}</span>
          </dd>
          <dt class="col-sm-3">Created</dt>
          <dd class="col-sm-9">
            {{ customersStore.current.createdAt ? formatMmmDdYyyy(customersStore.current.createdAt) : '' }}
          </dd>
        </dl>
      </div>
    </div>

    <div class="card">
      <div class="card-header">Interaction History</div>
      <div class="card-body">
        <ul v-if="interactionsStore.list.length > 0" class="list-group mb-3">
          <li v-for="i in interactionsStore.list" :key="i.id" class="list-group-item">
            <div class="d-flex justify-content-between">
              <span>
                <span class="badge badge-secondary mr-2">{{ i.type }}</span>{{ i.note }}
              </span>
              <small class="text-muted">{{ i.loggedByUsername }} &middot; {{ formatMmmDdYyyyHm(i.loggedAt) }}</small>
            </div>
          </li>
        </ul>
        <p v-else class="text-muted">No interactions logged yet.</p>

        <hr />
        <h6>Log New Interaction</h6>
        <form class="form-inline" @submit.prevent="handleLog">
          <select v-model="form.type" class="form-control mr-2">
            <option value="Call">Call</option>
            <option value="Email">Email</option>
            <option value="Meeting">Meeting</option>
            <option value="Note">Note</option>
          </select>
          <input
            v-model="form.note"
            type="text"
            class="form-control mr-2 flex-grow-1"
            placeholder="Note..."
            required
          />
          <button type="submit" class="btn btn-primary btn-sm">Log</button>
        </form>
      </div>
    </div>

    <RouterLink to="/customers" class="btn btn-secondary mt-3">Back to List</RouterLink>
  </div>
</template>
