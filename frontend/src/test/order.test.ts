import { describe, expect, it } from 'vitest'
import type { Product } from '../types'
import { calculateOrderTotal, hasDuplicateProducts } from '../utils/order'
import { getOrderActionKeys } from '../utils/orderActions'

const products: Product[] = [
  {
    id: 1,
    productCode: 'P01',
    productName: 'Bình 20 lít',
    categoryId: 1,
    categoryName: 'Bình nước',
    capacity: '20 lít',
    unit: 'Bình',
    price: 45_000,
    stockQuantity: 20,
    minimumStock: 5,
    requiresContainer: true,
    containerDeposit: 50_000,
    isLowStock: false,
    isActive: true,
    createdAt: '2026-01-01T00:00:00Z',
  },
  {
    id: 2,
    productCode: 'P02',
    productName: 'Thùng 500 ml',
    categoryId: 2,
    categoryName: 'Nước đóng chai',
    capacity: '500 ml',
    unit: 'Thùng',
    price: 90_000,
    stockQuantity: 15,
    minimumStock: 5,
    requiresContainer: false,
    containerDeposit: 0,
    isLowStock: false,
    isActive: true,
    createdAt: '2026-01-01T00:00:00Z',
  },
]

describe('tính tiền và kiểm tra dòng đơn hàng', () => {
  it('tính tổng từ giá sản phẩm hiện tại và số lượng', () => {
    expect(
      calculateOrderTotal(
        [
          { productId: 1, quantity: 2 },
          { productId: 2, quantity: 3 },
        ],
        products,
      ),
    ).toBe(360_000)
  })

  it('không tính sản phẩm không tồn tại và số lượng trống', () => {
    expect(
      calculateOrderTotal(
        [
          { productId: 999, quantity: 2 },
          { productId: 1 },
        ],
        products,
      ),
    ).toBe(0)
  })

  it('phát hiện sản phẩm bị chọn trùng', () => {
    expect(
      hasDuplicateProducts([
        { productId: 1, quantity: 1 },
        { productId: 1, quantity: 2 },
      ]),
    ).toBe(true)
    expect(
      hasDuplicateProducts([
        { productId: 1, quantity: 1 },
        { productId: 2, quantity: 2 },
      ]),
    ).toBe(false)
  })
})

describe('ánh xạ hành động đơn hàng theo vai trò và trạng thái', () => {
  it('Sales chỉ sửa, xác nhận và hủy đơn mới', () => {
    expect(getOrderActionKeys('Sales', 'New')).toEqual([
      'view',
      'edit',
      'confirm',
      'cancel',
    ])
    expect(getOrderActionKeys('Sales', 'Confirmed')).toEqual(['view'])
  })

  it('Warehouse xử lý đúng hai bước kho', () => {
    expect(getOrderActionKeys('Warehouse', 'Confirmed')).toEqual([
      'view',
      'prepare',
    ])
    expect(getOrderActionKeys('Warehouse', 'Preparing')).toEqual([
      'view',
      'ready',
    ])
  })

  it('Admin và Sales có thể phân công khi sẵn sàng giao', () => {
    expect(getOrderActionKeys('Admin', 'ReadyForDelivery')).toContain('assign')
    expect(getOrderActionKeys('Sales', 'ReadyForDelivery')).toContain('assign')
    expect(
      getOrderActionKeys('Warehouse', 'ReadyForDelivery'),
    ).not.toContain('assign')
  })

  it('không cung cấp thao tác nghiệp vụ cho đơn cuối luồng', () => {
    expect(getOrderActionKeys('Admin', 'Delivered')).toEqual(['view'])
    expect(getOrderActionKeys('Sales', 'DeliveryFailed')).toEqual(['view'])
  })
})
