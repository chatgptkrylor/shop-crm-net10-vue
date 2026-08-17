<script setup lang="ts">
import { reactive, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'

const router = useRouter()
const route = useRoute()
const authStore = useAuthStore()

const form = reactive({
  username: '',
  password: '',
})
const errors = reactive({
  username: '',
  password: '',
})
const serverError = ref('')
const submitted = ref(false)

function validate(): boolean {
  errors.username = form.username.trim() ? '' : 'Username is required'
  errors.password = form.password ? '' : 'Password is required'
  return !errors.username && !errors.password
}

async function handleSubmit() {
  submitted.value = true
  serverError.value = ''
  if (!validate()) return
  try {
    await authStore.login({ username: form.username, password: form.password })
    const redirect = (route.query.redirect as string) || '/dashboard'
    router.push(redirect)
  } catch {
    serverError.value = 'Invalid username or password'
  }
}
</script>

<template>
  <div class="container">
    <div class="row justify-content-center align-items-center min-vh-100">
      <div class="col-md-6 col-lg-4">
        <div class="card shadow">
          <div class="card-body p-4">
            <h3 class="text-center mb-3">Tiny CRM</h3>
            <p class="text-center text-muted mb-4">Sign in to your account</p>

            <form class="needs-validation" @submit.prevent="handleSubmit">
              <div v-if="serverError" class="text-danger small mb-2">{{ serverError }}</div>

              <div class="form-group">
                <label for="username">Username</label>
                <input
                  id="username"
                  v-model="form.username"
                  type="text"
                  class="form-control"
                  autofocus
                />
                <span v-if="submitted && errors.username" class="text-danger small">{{ errors.username }}</span>
              </div>

              <div class="form-group">
                <label for="password">Password</label>
                <input
                  id="password"
                  v-model="form.password"
                  type="password"
                  class="form-control"
                />
                <span v-if="submitted && errors.password" class="text-danger small">{{ errors.password }}</span>
              </div>

              <button type="submit" class="btn btn-primary btn-block">Sign In</button>
            </form>

            <div class="text-center mt-3">
              <small class="text-muted">Demo: admin / Admin@123</small>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>
