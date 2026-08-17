import client from './client'
import type { StatusCountDto } from './dashboard'

export interface ReportDto {
  statusCounts: StatusCountDto[]
  totalCustomers: number
}

export async function getReports(): Promise<ReportDto> {
  const response = await client.get<ReportDto>('/reports')
  return response.data
}
