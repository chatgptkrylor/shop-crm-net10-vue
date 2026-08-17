<script setup lang="ts">
import { RouterLink } from 'vue-router'
</script>

<template>
  <div class="text-center">
    <h1 class="display-4">Something went wrong</h1>
    <p class="text-muted">An error occurred while processing your request.</p>
    <RouterLink to="/dashboard" class="btn btn-primary">Back to Dashboard</RouterLink>
  </div>
</template>
