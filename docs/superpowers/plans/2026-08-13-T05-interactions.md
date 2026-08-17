# T05: Interactions — API + Vue details view with log-interaction dialog

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add customer interactions functionality with API endpoints, Vue customer details view with embedded interactions list, and log-interaction dialog.

**Architecture:** Backend already implements interactions controller and DTOs. Frontend needs to create interactions store, API module, and update CustomerDetailsView to display interactions list and log-interaction dialog.

**Tech Stack:** .NET 10 (Web API), Vue 3 + TypeScript, Pinia, Element Plus, Vite

## Global Constraints

- .NET 10 preview SDK is installed
- SQL Server 2022 running on localhost:1433
- Vue 3 + TS + Vite + Pinia + Vue Router + Element Plus frontend
- Build output to `../backend-net10/ShopApi/wwwroot`
- All endpoints use `[Authorize]` and `[ServiceFilter(typeof(XRequestedWithFilter))]`
- JWT claims: `userId`, `username`, `role`

---

### Task 1: Fix getCustomer API to return CustomerDetailDto with interactions

**Files:**
- Modify: `src/frontend-vue/src/api/customers.ts:25-28`

**Interfaces:**
- Consumes: None
- Produces: `getCustomer(id: number)` that returns `CustomerDetailDto` with both customer and interactions data

- [ ] **Step 1: Define new interfaces for CustomerDetailDto and InteractionDto**

```typescript
export interface InteractionDto {
  id: number
  customerId: number
  type: string
  note: string
  loggedAt: string
  loggedByUserId: number
  loggedByUsername: string
}

export interface CustomerDetailDto {
  customer: CustomerDto
  interactions: InteractionDto[]
}
```

- [ ] **Step 2: Update getCustomer function return type**

Change line 25-28 in `src/frontend-vue/src/api/customers.ts`:

```typescript
export async function getCustomer(id: number): Promise<CustomerDetailDto> {
  const response = await client.get<CustomerDetailDto>(`/customers/${id}`)
  return response.data
}
```

- [ ] **Step 3: Run type check to verify changes**

Run: `cd src/frontend-vue && npm run type-check`
Expected: PASS with no type errors

- [ ] **Step 4: Commit**

```bash
git add src/frontend-vue/src/api/customers.ts
git commit -m "feat: update getCustomer to return CustomerDetailDto with interactions"
```

---

### Task 2: Create interactions store with byCustomer, fetchByCustomer, create

**Files:**
- Create: `src/frontend-vue/src/stores/interactions.ts`

**Interfaces:**
- Consumes: `src/frontend-vue/src/api/interactions.ts` (next task)
- Produces: `useInteractionsStore` with `byCustomer`, `fetchByCustomer`, `create` functions

- [ ] **Step 1: Create interactions store file**

Create `src/frontend-vue/src/stores/interactions.ts`:

```typescript
import { defineStore } from 'pinia'
import { ref } from 'vue'
import * as interactionsApi from '@/api/interactions'
import type { InteractionDto } from '@/api/interactions'

export const useInteractionsStore = defineStore('interactions', () => {
  const byCustomer = ref<Record<number, InteractionDto[]>>({})
  const loading = ref(false)

  async function fetchByCustomer(customerId: number) {
    loading.value = true
    try {
      byCustomer.value[customerId] = await interactionsApi.getByCustomer(customerId)
    } finally {
      loading.value = false
    }
  }

  async function create(customerId: number, data: Omit<InteractionDto, 'id' | 'customerId' | 'loggedAt' | 'loggedByUserId' | 'loggedByUsername'>) {
    const result = await interactionsApi.create(customerId, data)
    // Refresh interactions list for this customer
    await fetchByCustomer(customerId)
    return result
  }

  return { byCustomer, loading, fetchByCustomer, create }
})
```

- [ ] **Step 2: Run type check to verify store**

Run: `cd src/frontend-vue && npm run type-check`
Expected: PASS (may fail due to missing api module, but store structure should be valid)

- [ ] **Step 3: Commit**

```bash
git add src/frontend-vue/src/stores/interactions.ts
git commit -m "feat: add interactions store with fetchByCustomer and create"
```

---

### Task 3: Create interactions API module

**Files:**
- Create: `src/frontend-vue/src/api/interactions.ts`

**Interfaces:**
- Consumes: `src/frontend-vue/src/api/client.ts` (configured axios instance)
- Produces: `InteractionDto` interface, `getByCustomer(customerId)`, `create(customerId, data)` functions

- [ ] **Step 1: Create interactions API file**

Create `src/frontend-vue/src/api/interactions.ts`:

```typescript
import client from './client'

export interface InteractionDto {
  id: number
  customerId: number
  type: string
  note: string
  loggedAt: string
  loggedByUserId: number
  loggedByUsername: string
}

export async function getByCustomer(customerId: number): Promise<InteractionDto[]> {
  const response = await client.get<InteractionDto[]>(`/customers/${customerId}/interactions`)
  return response.data
}

export async function create(customerId: number, data: { type: string; note: string }): Promise<InteractionDto> {
  const response = await client.post<InteractionDto>('/interactions', { customerId, ...data })
  return response.data
}
```

- [ ] **Step 2: Run type check to verify API**

Run: `cd src/frontend-vue && npm run type-check`
Expected: PASS

- [ ] **Step 3: Commit**

```bash
git add src/frontend-vue/src/api/interactions.ts
git commit -m "feat: add interactions API module with getByCustomer and create"
```

---

### Task 4: Add CustomerDetailDto to customers API

**Files:**
- Modify: `src/frontend-vue/src/api/customers.ts:1-18`

**Interfaces:**
- Consumes: None
- Produces: `CustomerDetailDto` interface

- [ ] **Step 1: Import InteractionDto and add CustomerDetailDto interface**

Add to `src/frontend-vue/src/api/customers.ts` after line 2:

```typescript
import type { InteractionDto } from './interactions'
```

Add `CustomerDetailDto` interface after `PagedResult<T>` interface (after line 18):

```typescript
export interface CustomerDetailDto {
  customer: CustomerDto
  interactions: InteractionDto[]
}
```

- [ ] **Step 2: Run type check to verify imports**

Run: `cd src/frontend-vue && npm run type-check`
Expected: PASS

- [ ] **Step 3: Commit**

```bash
git add src/frontend-vue/src/api/customers.ts
git commit -m "feat: add CustomerDetailDto interface with interactions"
```

---

### Task 5: Update CustomerDetailsView to show interactions list and log-interaction dialog

**Files:**
- Modify: `src/frontend-vue/src/views/CustomerDetailsView.vue:1-42`

**Interfaces:**
- Consumes: `useCustomersStore` with updated `current` as `CustomerDetailDto`, `useInteractionsStore`
- Produces: Vue component with interactions table and log-interaction dialog

- [ ] **Step 1: Update CustomerDetailsView script setup**

Replace entire script section in `src/frontend-vue/src/views/CustomerDetailsView.vue`:

```vue
<script setup lang="ts">
import { ref, computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useCustomersStore } from '@/stores/customers'
import { useInteractionsStore } from '@/stores/interactions'
import type { InteractionDto } from '@/api/interactions'
import { ElMessage } from 'element-plus'

const route = useRoute()
const router = useRouter()
const customersStore = useCustomersStore()
const interactionsStore = useInteractionsStore()
const customerId = Number(route.params.id)

const dialogVisible = ref(false)
const interactionForm = ref({
  type: '',
  note: ''
})

const interactions = computed(() => {
  return customersStore.current?.interactions || []
})

async function loadCustomer() {
  await customersStore.fetchOne(customerId)
  await interactionsStore.fetchByCustomer(customerId)
}

async function submitInteraction() {
  if (!interactionForm.value.type || !interactionForm.value.note.trim()) {
    ElMessage.error('Type and note are required')
    return
  }

  await interactionsStore.create(customerId, {
    type: interactionForm.value.type,
    note: interactionForm.value.note
  })

  ElMessage.success('Interaction logged successfully')
  dialogVisible.value = false
  interactionForm.value = { type: '', note: '' }
  await loadCustomer()
}

loadCustomer()
</script>
```

- [ ] **Step 2: Update CustomerDetailsView template**

Replace entire template section in `src/frontend-vue/src/views/CustomerDetailsView.vue`:

```vue
<template>
  <div class="customer-details">
    <h1>Customer Details</h1>
    <div v-if="customersStore.current" class="details">
      <div class="customer-info">
        <p><strong>Name:</strong> {{ customersStore.current.customer.name }}</p>
        <p><strong>Email:</strong> {{ customersStore.current.customer.email || '—' }}</p>
        <p><strong>Phone:</strong> {{ customersStore.current.customer.phone || '—' }}</p>
        <p><strong>Company:</strong> {{ customersStore.current.customer.company || '—' }}</p>
        <p><strong>Status:</strong> {{ customersStore.current.customer.status }}</p>
        <el-button @click="router.push(`/customers/${customerId}/edit`)">Edit</el-button>
        <el-button @click="router.push('/customers')">Back</el-button>
      </div>

      <div class="interactions-section">
        <div class="section-header">
          <h2>Interactions</h2>
          <el-button type="primary" @click="dialogVisible = true">Log interaction</el-button>
        </div>

        <el-table :data="interactions" stripe>
          <el-table-column prop="type" label="Type" width="120" />
          <el-table-column prop="note" label="Note" />
          <el-table-column prop="loggedByUsername" label="Logged By" width="120" />
          <el-table-column prop="loggedAt" label="Date" width="180">
            <template #default="{ row }">
              {{ new Date(row.loggedAt).toLocaleString() }}
            </template>
          </el-table-column>
        </el-table>
      </div>
    </div>
    <div v-else>Loading...</div>

    <el-dialog v-model="dialogVisible" title="Log Interaction" width="500px">
      <el-form :model="interactionForm" label-width="80px">
        <el-form-item label="Type" required>
          <el-select v-model="interactionForm.type" placeholder="Select type" style="width: 100%">
            <el-option label="Call" value="Call" />
            <el-option label="Email" value="Email" />
            <el-option label="Meeting" value="Meeting" />
            <el-option label="Note" value="Note" />
          </el-select>
        </el-form-item>
        <el-form-item label="Note" required>
          <el-input v-model="interactionForm.note" type="textarea" :rows="4" placeholder="Enter interaction note..." />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="dialogVisible = false">Cancel</el-button>
        <el-button type="primary" @click="submitInteraction">Save</el-button>
      </template>
    </el-dialog>
  </div>
</template>
```

- [ ] **Step 3: Update CustomerDetailsView styles**

Replace entire style section in `src/frontend-vue/src/views/CustomerDetailsView.vue`:

```vue
<style scoped>
.customer-details {
  padding: 20px;
  max-width: 1000px;
  margin: 0 auto;
}

.details p {
  margin: 8px 0;
}

.customer-info {
  background: #f5f5f5;
  padding: 20px;
  border-radius: 8px;
  margin-bottom: 30px;
}

.interactions-section {
  margin-top: 30px;
}

.section-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 15px;
}

.section-header h2 {
  margin: 0;
}
</style>
```

- [ ] **Step 4: Run type check to verify component**

Run: `cd src/frontend-vue && npm run type-check`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add src/frontend-vue/src/views/CustomerDetailsView.vue
git commit -m "feat: update CustomerDetailsView with interactions list and log-interaction dialog"
```

---

### Task 6: Create Playwright browser test for interactions

**Files:**
- Create: `tests/browser/interactions.spec.ts`

**Interfaces:**
- Consumes: None
- Produces: Playwright test that screenshots customer details page and logs an interaction

- [ ] **Step 1: Create Playwright test file**

Create `tests/browser/interactions.spec.ts`:

```typescript
import { test, expect } from '@playwright/test'

test('interactions: customer details shows interactions list and allows logging', async ({ page }) => {
  // Login
  await page.goto('http://localhost:5000')
  await page.fill('input[placeholder="Username"]', 'admin')
  await page.fill('input[placeholder="Password"]', 'Admin@123')
  await page.click('button:has-text("Login")')
  await page.waitForURL('**/dashboard')

  // Navigate to customer details
  await page.click('a:has-text("Customers")')
  await page.waitForURL('**/customers')
  await page.click('a:has-text("Edit")') // First customer edit button
  await page.waitForURL('**/customers/1/edit')

  // Go to details instead of edit
  await page.click('button:has-text("Back")')
  await page.waitForURL('**/customers')

  // Click customer name to view details
  await page.click('td:has-text("Customer")')
  await page.waitForURL('**/customers/1')

  // Verify interactions section exists
  await expect(page.locator('h2:has-text("Interactions")')).toBeVisible()
  await expect(page.locator('button:has-text("Log interaction")')).toBeVisible()

  // Take screenshot of interactions page
  await page.screenshot({ path: '/tmp/interactions-details-before.png' })

  // Open log interaction dialog
  await page.click('button:has-text("Log interaction")')
  await expect(page.locator('.el-dialog:has-text("Log Interaction")')).toBeVisible()

  // Fill interaction form
  await page.selectOption('select[placeholder="Select type"]', 'Call')
  await page.fill('textarea[placeholder="Enter interaction note..."]', 'Test Playwright interaction')

  // Submit form
  await page.click('button:has-text("Save")')

  // Wait for dialog to close and verify success message
  await expect(page.locator('.el-dialog:has-text("Log Interaction")')).not.toBeVisible()
  await expect(page.locator('.el-message:has-text("Interaction logged successfully")')).toBeVisible()

  // Verify interaction appears in table
  await expect(page.locator('td:has-text("Call")')).toBeVisible()
  await expect(page.locator('td:has-text("Test Playwright interaction")')).toBeVisible()

  // Take screenshot after interaction
  await page.screenshot({ path: '/tmp/interactions-details-after.png' })

  // Console check
  const logs: string[] = []
  page.on('console', msg => logs.push(msg.text()))
  await page.reload()
  expect(logs.some(log => log.includes('error'))).toBeFalsy()
})

test('interactions: form validation requires type and note', async ({ page }) => {
  // Login
  await page.goto('http://localhost:5000')
  await page.fill('input[placeholder="Username"]', 'admin')
  await page.fill('input[placeholder="Password"]', 'Admin@123')
  await page.click('button:has-text("Login")')
  await page.waitForURL('**/dashboard')

  // Navigate to customer details
  await page.click('a:has-text("Customers")')
  await page.click('td:has-text("Customer")')

  // Open log interaction dialog
  await page.click('button:has-text("Log interaction")')

  // Try to submit without filling form
  await page.click('button:has-text("Save")')

  // Verify error message
  await expect(page.locator('.el-message:has-text("Type and note are required")')).toBeVisible()

  // Close dialog
  await page.click('button:has-text("Cancel")')
})
```

- [ ] **Step 2: Run Playwright test to verify it works**

Run: `npx playwright test tests/browser/interactions.spec.ts --headed`
Expected: Tests should pass and create screenshots in `/tmp/`

- [ ] **Step 3: Commit**

```bash
git add tests/browser/interactions.spec.ts
git commit -m "test: add Playwright interactions test with screenshots and validation"
```

---

### Task 7: Run full verification suite

**Files:**
- No file changes, runs verification commands from ticket specification

**Interfaces:**
- Consumes: All previous tasks
- Produces: Successful verification results

- [ ] **Step 1: Run backend interactions tests**

Run: `dotnet test src/backend-net10/ShopApi.Tests --filter InteractionsTests`
Expected: PASS with all tests green

- [ ] **Step 2: Run smoke CRUD tests**

Run: `bash tests/smoke-crud.sh`
Expected: PASS with all smoke tests passing

- [ ] **Step 3: Run frontend type check and build**

Run: `cd src/frontend-vue && npm run type-check && npm run build`
Expected: PASS with no type errors and successful build

- [ ] **Step 4: Run Playwright interactions test**

Run: `npx playwright test tests/browser/interactions.spec.ts`
Expected: PASS with screenshots created

- [ ] **Step 5: Verify console logs are clean**

Check Playwright console output for any errors:
Expected: No console errors during test execution

- [ ] **Step 6: Final commit with verification evidence**

```bash
git commit --allow-empty -m "test: T05 verification complete - all tests passing"
```

---

## Self-Review

**1. Spec coverage:**
- ✅ `GET /api/customers/{id}/interactions` returns `200 [InteractionDto]` - covered by interactions API module
- ✅ `POST /api/interactions` with JWT claims - covered by interactions API create function
- ✅ `InteractionDto` validation - handled by Element Plus form validation
- ✅ `InteractionsController` `[Authorize]` - already implemented in backend
- ✅ Vue `CustomerDetailsView` with interactions list and dialog - covered by updated view
- ✅ `interactionsStore` with `byCustomer, fetchByCustomer, create` - covered by store implementation
- ✅ `interactions.ts` Axios module - covered by API module
- ✅ `InteractionsTests` xUnit - already implemented in backend
- ✅ Playwright `interactions.spec.ts` - covered by browser tests

**2. Placeholder scan:**
- No placeholders found - all code and commands are explicit

**3. Type consistency:**
- `InteractionDto` interface consistent across API and store
- `CustomerDetailDto` properly extends existing customer structure
- Function signatures match backend API contracts

All requirements from T05 specification are covered in the plan.