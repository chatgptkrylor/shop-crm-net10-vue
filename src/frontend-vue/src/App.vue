<script setup lang="ts">
import { ref } from 'vue'
import { RouterLink, RouterView, useRoute, useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'

const route = useRoute()
const router = useRouter()
const authStore = useAuthStore()
const navOpen = ref(false)

async function handleLogout() {
  await authStore.logout()
  router.push('/login')
}
</script>

<template>
  <template v-if="route.name === 'login'">
    <RouterView />
  </template>
  <template v-else>
    <nav class="navbar navbar-expand-lg navbar-dark bg-dark">
      <div class="container">
        <RouterLink class="navbar-brand" to="/dashboard">Tiny CRM</RouterLink>
        <button
          class="navbar-toggler"
          type="button"
          aria-label="Toggle navigation"
          @click="navOpen = !navOpen"
        >
          <span class="navbar-toggler-icon"></span>
        </button>
        <div id="nav" class="collapse navbar-collapse" :class="{ show: navOpen }">
          <ul class="navbar-nav mr-auto">
            <li class="nav-item">
              <RouterLink class="nav-link" to="/dashboard">Dashboard</RouterLink>
            </li>
            <li class="nav-item">
              <RouterLink class="nav-link" to="/customers">Customers</RouterLink>
            </li>
            <li class="nav-item">
              <RouterLink class="nav-link" to="/reports">Reports</RouterLink>
            </li>
          </ul>
          <ul class="navbar-nav">
            <template v-if="authStore.isAuthenticated">
              <li class="nav-item">
                <span class="navbar-text text-light mr-3">Hello, {{ authStore.username }}</span>
              </li>
              <li class="nav-item">
                <a class="nav-link" href="#" @click.prevent="handleLogout">Logout</a>
              </li>
            </template>
          </ul>
        </div>
      </div>
    </nav>
    <div class="container mt-4">
      <RouterView />
    </div>
    <footer class="container mt-5 mb-3 text-center text-muted small">
      Tiny CRM &mdash; .NET 10 / Vue 3 / Kestrel on Linux
    </footer>
  </template>
</template>
