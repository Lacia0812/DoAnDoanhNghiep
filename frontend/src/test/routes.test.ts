import { describe, expect, it } from 'vitest'
import {
  APP_ROUTES,
  MENU_ROUTES,
  findRouteConfig,
} from '../routes/routeConfig'
import { ROLE_HOME } from '../utils/constants'

describe('phân quyền route và trang mặc định', () => {
  it('điều hướng mỗi vai trò tới đúng trang nghiệp vụ', () => {
    expect(ROLE_HOME).toEqual({
      Admin: '/dashboard',
      Sales: '/orders',
      Warehouse: '/inventory',
      Delivery: '/deliveries',
    })
  })

  it('không cho Delivery truy cập các màn hình nội bộ khác', () => {
    expect(findRouteConfig('/orders')?.roles).not.toContain('Delivery')
    expect(findRouteConfig('/inventory')?.roles).not.toContain('Delivery')
    expect(findRouteConfig('/users')?.roles).not.toContain('Delivery')
    expect(findRouteConfig('/deliveries')?.roles).toEqual(['Delivery'])
  })

  it('Admin chỉ xem kho và không thay vai trò vận hành giao hàng', () => {
    expect(findRouteConfig('/inventory')?.roles).toContain('Admin')
    expect(findRouteConfig('/deliveries')?.roles).not.toContain('Admin')
  })

  it('mọi mục menu lấy trực tiếp từ cùng cấu hình route được bảo vệ', () => {
    expect(MENU_ROUTES.length).toBe(8)
    expect(MENU_ROUTES.every((menuRoute) => APP_ROUTES.includes(menuRoute))).toBe(
      true,
    )
    expect(MENU_ROUTES.map((route) => route.path)).toEqual([
      '/dashboard',
      '/products',
      '/customers',
      '/orders',
      '/inventory',
      '/finance',
      '/deliveries',
      '/users',
    ])
  })

  it('route chi tiết và biểu mẫu có quyền riêng nhưng không tạo menu trùng', () => {
    expect(findRouteConfig('/orders/create')?.roles).toEqual(['Sales'])
    expect(findRouteConfig('/orders/:id/edit')?.roles).toEqual(['Sales'])
    expect(findRouteConfig('/orders/:id')?.roles).toEqual([
      'Admin',
      'Sales',
      'Warehouse',
    ])
    expect(MENU_ROUTES.some((route) => route.path.includes(':id'))).toBe(false)
  })
})
