import { render, screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { beforeEach, describe, expect, it, vi } from 'vitest'

const mocks = vi.hoisted(() => ({
  listCustomers: vi.fn(),
  createCustomer: vi.fn(),
  listProducts: vi.fn(),
  getOrder: vi.fn(),
  createOrder: vi.fn(),
  updateOrder: vi.fn(),
}))

vi.mock('../services/api', () => ({
  customerApi: {
    active: mocks.listCustomers,
    create: mocks.createCustomer,
  },
  productApi: {
    active: mocks.listProducts,
  },
  orderApi: {
    get: mocks.getOrder,
    create: mocks.createOrder,
    update: mocks.updateOrder,
  },
  getApiErrorMessage: (_error: unknown, fallback: string) => fallback,
}))

import OrderFormPage from '../pages/OrderFormPage'

const emptyPage = {
  items: [],
  totalCount: 0,
  page: 1,
  pageSize: 100,
  totalPages: 0,
}

const createdCustomer = {
  id: 77,
  customerCode: 'KVL-CREATED',
  customerName: 'Khách vãng lai',
  phone: '0901234567',
  address: '12 Nguyễn Huệ, Quận 1',
  email: null,
  customerType: 'Individual',
  note: 'Tạo nhanh',
  isActive: true,
  createdAt: '2026-07-27T00:00:00Z',
}

describe('tạo nhanh khách vãng lai trong form đơn hàng', () => {
  beforeEach(() => {
    mocks.listCustomers.mockReset().mockResolvedValue(emptyPage)
    mocks.createCustomer.mockReset().mockResolvedValue(createdCustomer)
    mocks.listProducts.mockReset().mockResolvedValue(emptyPage)
    mocks.getOrder.mockReset()
    mocks.createOrder.mockReset()
    mocks.updateOrder.mockReset()
  })

  it('tạo hồ sơ Individual rồi chọn khách và tự điền địa chỉ vào đơn', async () => {
    const user = userEvent.setup({ delay: null })

    render(
      <MemoryRouter initialEntries={['/orders/create']}>
        <Routes>
          <Route path="/orders/create" element={<OrderFormPage />} />
        </Routes>
      </MemoryRouter>,
    )

    await screen.findByRole('heading', { name: 'Tạo đơn hàng' })
    await user.click(
      screen.getByRole('button', { name: /Thêm khách vãng lai/i }),
    )

    const dialog = await screen.findByRole('dialog', {
      name: 'Thêm nhanh khách vãng lai',
    })
    const modal = within(dialog)

    expect(modal.getByLabelText('Tên hiển thị')).toHaveValue('Khách vãng lai')
    await user.type(modal.getByLabelText('Số điện thoại'), '0901234567')
    await user.type(
      modal.getByLabelText('Địa chỉ giao hàng'),
      '12 Nguyễn Huệ, Quận 1',
    )
    await user.click(
      modal.getByRole('button', { name: /^Tạo và chọn$/ }),
    )

    await waitFor(() => expect(mocks.createCustomer).toHaveBeenCalledTimes(1))
    expect(mocks.createCustomer).toHaveBeenCalledWith({
      customerCode: expect.stringMatching(/^KVL-\d+$/),
      customerName: 'Khách vãng lai',
      phone: '0901234567',
      address: '12 Nguyễn Huệ, Quận 1',
      customerType: 'Individual',
      note: expect.any(String),
    })

    expect(
      screen.getByText('KVL-CREATED · Khách vãng lai · 0901234567'),
    ).toBeInTheDocument()
    expect(document.getElementById('deliveryAddress')).toHaveValue(
      '12 Nguyễn Huệ, Quận 1',
    )
  }, 20_000)
})
