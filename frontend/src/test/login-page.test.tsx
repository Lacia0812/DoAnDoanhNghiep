import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import LoginPage from '../pages/LoginPage'

const auth = {
  login: vi.fn(),
  isAuthenticated: false,
  user: null,
  loading: false,
}

vi.mock('../contexts/AuthContext', () => ({
  useAuth: () => auth,
}))

describe('màn hình đăng nhập Astryx', () => {
  beforeEach(() => {
    auth.login.mockReset()
    localStorage.clear()
  })

  it('điền đúng tài khoản trình diễn khi người dùng chọn vai trò', async () => {
    const user = userEvent.setup()
    render(
      <MemoryRouter>
        <LoginPage />
      </MemoryRouter>,
    )

    await user.click(
      screen.getByRole('button', { name: 'Quản trị · admin' }),
    )

    expect(screen.getByLabelText('Tên đăng nhập')).toHaveValue('admin')
    expect(screen.getByLabelText('Mật khẩu')).toHaveValue('Admin@123')
  })

  it('hiển thị validation tiếng Việt và không gọi API khi form trống', async () => {
    const user = userEvent.setup()
    render(
      <MemoryRouter>
        <LoginPage />
      </MemoryRouter>,
    )

    await user.click(screen.getByRole('button', { name: 'Đăng nhập' }))

    expect(screen.getByText('Vui lòng nhập tên đăng nhập.')).toBeVisible()
    expect(screen.getByText('Vui lòng nhập mật khẩu.')).toBeVisible()
    expect(auth.login).not.toHaveBeenCalled()
  })
})
