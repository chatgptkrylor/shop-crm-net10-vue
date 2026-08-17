import client from './client'

export interface LoginRequest {
  username: string
  password: string
}

export interface LoginResponse {
  username: string
  role: string
}

export interface UserDto {
  userId: number
  username: string
  role: string
}

export async function login(data: LoginRequest): Promise<LoginResponse> {
  const response = await client.post<LoginResponse>('/account/login', data)
  return response.data
}

export async function logout(): Promise<void> {
  await client.post('/account/logout')
}

export async function me(): Promise<UserDto> {
  const response = await client.get<UserDto>('/account/me')
  return response.data
}