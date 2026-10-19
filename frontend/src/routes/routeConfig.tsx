import { lazy, type LazyExoticComponent, type ComponentType, type ReactNode } from 'react'
import {
  AppstoreOutlined,
  BarChartOutlined,
  CarOutlined,
  DollarOutlined,
  ShoppingCartOutlined,
  TeamOutlined,
  UserOutlined,
} from '@ant-design/icons'
import type { Role } from '../types'

export interface RouteMenuConfig {
  label: string
  icon: ReactNode
  order: number
}

export interface AppRouteConfig {
  path: string
  roles: Role[]
  component: LazyExoticComponent<ComponentType>
  menu?: RouteMenuConfig
}

export type MenuRouteConfig = AppRouteConfig & { menu: RouteMenuConfig }

export const APP_ROUTES: AppRouteConfig[] = [
  {
    path: '/dashboard',
    roles: ['Admin'],
    component: lazy(() => import('../pages/DashboardPage')),
    menu: {
      label: 'Tổng quan',
      icon: <BarChartOutlined />,
      order: 10,
    },
  },
  {
    path: '/products',
    roles: ['Admin'],
    component: lazy(() => import('../pages/ProductsPage')),
    menu: {
      label: 'Sản phẩm',
      icon: <AppstoreOutlined />,
      order: 20,
    },
  },
  {
    path: '/customers',
    roles: ['Admin', 'Sales'],
    component: lazy(() => import('../pages/CustomersPage')),
    menu: {
      label: 'Khách hàng',
      icon: <TeamOutlined />,
      order: 30,
    },
  },
  {
    path: '/orders',
    roles: ['Admin', 'Sales', 'Warehouse'],
    component: lazy(() => import('../pages/OrdersPage')),
    menu: {
      label: 'Đơn hàng',
      icon: <ShoppingCartOutlined />,
      order: 40,
    },
  },
  {
    path: '/orders/create',
    roles: ['Sales'],
    component: lazy(() => import('../pages/OrderFormPage')),
  },
  {
    path: '/orders/:id/edit',
    roles: ['Sales'],
    component: lazy(() => import('../pages/OrderFormPage')),
  },
  {
    path: '/orders/:id',
    roles: ['Admin', 'Sales', 'Warehouse'],
    component: lazy(() => import('../pages/OrderDetailPage')),
  },
  {
    path: '/inventory',
    roles: ['Admin', 'Warehouse'],
    component: lazy(() => import('../pages/InventoryPage')),
    menu: {
      label: 'Kho hàng',
      icon: <AppstoreOutlined />,
      order: 50,
    },
  },
  {
    path: '/finance',
    roles: ['Admin', 'Sales'],
    component: lazy(() => import('../pages/FinancePage')),
    menu: {
      label: 'Công nợ & vỏ bình',
      icon: <DollarOutlined />,
      order: 55,
    },
  },
  {
    path: '/deliveries',
    roles: ['Delivery'],
    component: lazy(() => import('../pages/DeliveriesPage')),
    menu: {
      label: 'Giao hàng',
      icon: <CarOutlined />,
      order: 60,
    },
  },
  {
    path: '/users',
    roles: ['Admin'],
    component: lazy(() => import('../pages/UsersPage')),
    menu: {
      label: 'Tài khoản',
      icon: <UserOutlined />,
      order: 70,
    },
  },
]

export const MENU_ROUTES: MenuRouteConfig[] = APP_ROUTES.filter(
  (route): route is MenuRouteConfig => Boolean(route.menu),
).sort((left, right) => left.menu.order - right.menu.order)

export const findRouteConfig = (path: string) =>
  APP_ROUTES.find((route) => route.path === path)
