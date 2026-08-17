import client from './client'

export interface CustomerDto {
  id: number
  name: string
  email: string | null
  phone: string | null
  company: string | null
  status: string
  createdAt?: string
}

export interface PagedResult<T> {
  items: T[]
  page: number
  pageSize: number
  totalPages: number
  totalCount: number
}

export async function getCustomers(page: number = 1): Promise<PagedResult<CustomerDto>> {
  const response = await client.get<PagedResult<CustomerDto>>(`/customers?page=${page}`)
  return response.data
}

export async function getCustomer(id: number): Promise<CustomerDto> {
  const response = await client.get<CustomerDto>(`/customers/${id}`)
  return response.data
}

export async function createCustomer(data: Omit<CustomerDto, 'id' | 'createdAt'>): Promise<CustomerDto> {
  const response = await client.post<CustomerDto>('/customers', data)
  return response.data
}

export async function updateCustomer(id: number, data: Omit<CustomerDto, 'id' | 'createdAt'>): Promise<CustomerDto> {
  const response = await client.put<CustomerDto>(`/customers/${id}`, data)
  return response.data
}

export async function deleteCustomer(id: number): Promise<void> {
  await client.delete(`/customers/${id}`)
}
