import { useEffect } from 'react'
import { act, render, screen, waitFor } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { beforeEach, describe, expect, it, vi } from 'vitest'

const mocks = vi.hoisted(() => ({
  profile: vi.fn(),
  login: vi.fn(),
}))

vi.mock('../services/api', () => ({
  TOKEN_STORAGE_KEY: 'gia-phu-auth-token',
  authApi: {
    profile: mocks.profile,
    login: mocks.login,
  },
}))

import { AuthProvider, useAuth } from '../contexts/AuthContext'

const user = {
  id: 1,
  username: 'admin',
  fullName: 'Quản trị viên',
  role: 'Admin' as const,
  isActive: true,
}

function AuthProbe({ loginNow = false }: { loginNow?: boolean }) {
  const auth = useAuth()
  useEffect(() => {
    if (loginNow) {
      void auth.login({ username: 'admin', password: 'Admin@123' })
    }
  }, [auth.login, loginNow])
  if (auth.loading) return <span>đang tải</span>
  return (
    <span>
      {auth.isAuthenticated ? `${auth.user?.fullName}|${auth.token}` : 'khách'}
    </span>
  )
}

describe('AuthContext', () => {
  beforeEach(() => {
    localStorage.clear()
    mocks.profile.mockReset()
    mocks.login.mockReset()
  })

  it('khôi phục phiên bằng token và gọi profile khi khởi động', async () => {
    localStorage.setItem('gia-phu-auth-token', 'stored-token')
    mocks.profile.mockResolvedValue(user)

    render(
      <MemoryRouter>
        <AuthProvider>
          <AuthProbe />
        </AuthProvider>
      </MemoryRouter>,
    )

    expect(screen.getByText('đang tải')).toBeInTheDocument()
    expect(await screen.findByText('Quản trị viên|stored-token')).toBeInTheDocument()
    expect(mocks.profile).toHaveBeenCalledTimes(1)
  })

  it('lưu token và người dùng sau khi đăng nhập', async () => {
    mocks.profile.mockResolvedValue(user)
    mocks.login.mockResolvedValue({
      token: 'new-token',
      expiresAt: '2030-01-01T00:00:00Z',
      user,
    })

    render(
      <MemoryRouter>
        <AuthProvider>
          <AuthProbe loginNow />
        </AuthProvider>
      </MemoryRouter>,
    )

    expect(await screen.findByText('Quản trị viên|new-token')).toBeInTheDocument()
    expect(localStorage.getItem('gia-phu-auth-token')).toBe('new-token')
  })

  it('xóa phiên khi nhận sự kiện 401 toàn cục', async () => {
    localStorage.setItem('gia-phu-auth-token', 'stored-token')
    mocks.profile.mockResolvedValue(user)

    render(
      <MemoryRouter>
        <AuthProvider>
          <AuthProbe />
        </AuthProvider>
      </MemoryRouter>,
    )
    await screen.findByText('Quản trị viên|stored-token')

    act(() => {
      window.dispatchEvent(new CustomEvent('auth:unauthorized'))
    })

    await waitFor(() => expect(screen.getByText('khách')).toBeInTheDocument())
    expect(localStorage.getItem('gia-phu-auth-token')).toBeNull()
  })
})
