import client from './client'

export interface StatusCountDto {
  status: string
  count: number
}

export interface InteractionDto {
  id: number
  customerId: number
  type: string
  note: string
  loggedAt: string
  loggedByUserId: number
  loggedByUsername: string
}

export interface DashboardDto {
  totalCustomers: number
  statusCounts: StatusCountDto[]
  recentInteractions: InteractionDto[]
  username: string
}

export async function getDashboard(): Promise<DashboardDto> {
  const response = await client.get<DashboardDto>('/dashboard')
  return response.data
}