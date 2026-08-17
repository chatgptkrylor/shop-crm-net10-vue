import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import * as authApi from '@/api/auth'

export const useAuthStore = defineStore('auth', () => {
  const username = ref<string>('')
  const role = ref<string>('')
  const isAuthenticated = computed(() => !!username.value)

  async function login(creds: { username: string; password: string }) {
    const response = await authApi.login(creds)
    username.value = response.username
    role.value = response.role
  }

  async function logout() {
    try {
      await authApi.logout()
    } finally {
      clear()
    }
  }

  async function fetchMe() {
    try {
      const user = await authApi.me()
      username.value = user.username
      role.value = user.role
    } catch {
      clear()
    }
  }

  function clear() {
    username.value = ''
    role.value = ''
  }

  return { username, role, isAuthenticated, login, logout, fetchMe, clear }
})