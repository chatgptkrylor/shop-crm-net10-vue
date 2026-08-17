import client from './client'
import type { InteractionDto } from './dashboard'

export interface CreateInteractionRequest {
  customerId: number
  type: string
  note: string
}

export async function getCustomerInteractions(customerId: number): Promise<InteractionDto[]> {
  const response = await client.get<InteractionDto[]>(`/customers/${customerId}/interactions`)
  return response.data
}

export async function createInteraction(data: CreateInteractionRequest): Promise<InteractionDto> {
  const response = await client.post<InteractionDto>('/interactions', data)
  return response.data
}
