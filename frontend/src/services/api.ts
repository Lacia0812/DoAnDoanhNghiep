import axios, { AxiosError } from 'axios'
import type {
  ApiProblem,
  Customer,
  CustomerPayload,
  DashboardData,
  DashboardSummary,
  Delivery,
  Debt,
  DebtSummary,
  ContainerTransaction,
  InventoryItem,
  InventoryTransaction,
  ListParams,
  LoginRequest,
  LoginResponse,
  Order,
  OrderPayload,
  OrderStatusPoint,
  PagedResult,
  Product,
  ProductCategory,
  ProductPayload,
  RestockSuggestion,
  RevenuePoint,
  Role,
  TopProductPoint,
  User,
} from '../types'

const API_URL = (import.meta.env.VITE_API_URL || 'http://localhost:5000/api').replace(
  /\/$/,
  '',
)

export const TOKEN_STORAGE_KEY = 'gia-phu-auth-token'

export const api = axios.create({
  baseURL: API_URL,
  timeout: 20_000,
  headers: { 'Content-Type': 'application/json' },
})

api.interceptors.request.use((config) => {
  const token = localStorage.getItem(TOKEN_STORAGE_KEY)
  if (token) {
    config.headers.Authorization = `Bearer ${token}`
  }
  return config
})

api.interceptors.response.use(
  (response) => response,
  (error: AxiosError) => {
    if (
      error.response?.status === 401 &&
      !String(error.config?.url ?? '').includes('/auth/login')
    ) {
      window.dispatchEvent(new CustomEvent('auth:unauthorized'))
    }
    return Promise.reject(error)
  },
)

export const getApiErrorMessage = (
  error: unknown,
  fallback = 'Có lỗi xảy ra. Vui lòng thử lại.',
) => {
  if (!axios.isAxiosError<ApiProblem>(error)) return fallback
  const data = error.response?.data
  if (!data) {
    return error.code === 'ECONNABORTED'
      ? 'Máy chủ phản hồi quá lâu. Vui lòng thử lại.'
      : 'Không thể kết nối đến máy chủ.'
  }
  const validationMessage = data.errors
    ? Object.values(data.errors).flat().filter(Boolean)[0]
    : undefined
  return validationMessage || data.detail || data.message || data.title || fallback
}

const getData = async <T>(request: Promise<{ data: T }>) => (await request).data

export const authApi = {
  login: (payload: LoginRequest) =>
    getData<LoginResponse>(api.post('/auth/login', payload)),
  profile: () => getData<User>(api.get('/auth/profile')),
}

export const productApi = {
  list: (params: ListParams = {}) =>
    getData<PagedResult<Product>>(api.get('/products', { params })),
  active: () =>
    getData<PagedResult<Product>>(
      api.get('/products', { params: { page: 1, pageSize: 100 } }),
    ),
  get: (id: number) => getData<Product>(api.get(`/products/${id}`)),
  create: (payload: ProductPayload) =>
    getData<Product>(api.post('/products', payload)),
  update: (
    id: number,
    payload: Omit<ProductPayload, 'initialStock'> & { isActive: boolean },
  ) =>
    getData<Product>(api.put(`/products/${id}`, payload)),
  remove: (id: number) => getData<void>(api.delete(`/products/${id}`)),
  addStock: (id: number, quantity: number, note?: string) =>
    getData<Product>(api.post(`/products/${id}/add-stock`, { quantity, note })),
}

export const categoryApi = {
  list: () => getData<ProductCategory[]>(api.get('/product-categories')),
}

export const customerApi = {
  list: (params: ListParams = {}) =>
    getData<PagedResult<Customer>>(api.get('/customers', { params })),
  active: () =>
    getData<PagedResult<Customer>>(
      api.get('/customers', { params: { page: 1, pageSize: 100 } }),
    ),
  get: (id: number) => getData<Customer>(api.get(`/customers/${id}`)),
  create: (payload: CustomerPayload) =>
    getData<Customer>(api.post('/customers', payload)),
  update: (id: number, payload: CustomerPayload & { isActive: boolean }) =>
    getData<Customer>(api.put(`/customers/${id}`, payload)),
  remove: (id: number) => getData<void>(api.delete(`/customers/${id}`)),
  orders: (id: number, params: ListParams = {}) =>
    getData<PagedResult<Order>>(api.get(`/customers/${id}/orders`, { params })),
}

export const orderApi = {
  list: (params: ListParams = {}) =>
    getData<PagedResult<Order>>(api.get('/orders', { params })),
  get: (id: number) => getData<Order>(api.get(`/orders/${id}`)),
  create: (payload: OrderPayload) =>
    getData<Order>(api.post('/orders', payload)),
  update: (id: number, payload: OrderPayload) =>
    getData<Order>(api.put(`/orders/${id}`, payload)),
  confirm: (id: number) => getData<Order>(api.post(`/orders/${id}/confirm`)),
  prepare: (id: number) => getData<Order>(api.post(`/orders/${id}/prepare`)),
  ready: (id: number) => getData<Order>(api.post(`/orders/${id}/ready`)),
  cancel: (id: number) => getData<Order>(api.post(`/orders/${id}/cancel`)),
  assignDelivery: (id: number, deliveryUserId: number) =>
    getData<Order>(
      api.post(`/orders/${id}/assign-delivery`, { deliveryUserId }),
    ),
  recordPayment: (
    id: number,
    amount: number,
    paymentMethod: string,
    note?: string,
  ) =>
    getData<Order>(
      api.post(`/orders/${id}/payments`, { amount, paymentMethod, note }),
    ),
  invoice: async (id: number) => {
    const response = await api.get<Blob>(`/orders/${id}/invoice`, {
      responseType: 'blob',
    })
    return response.data
  },
}

export const inventoryApi = {
  list: (params: ListParams = {}) =>
    getData<PagedResult<InventoryItem>>(api.get('/inventory', { params })),
  transactions: (params: ListParams = {}) =>
    getData<PagedResult<InventoryTransaction>>(
      api.get('/inventory/transactions', { params }),
    ),
}

export const deliveryApi = {
  mine: (params: ListParams = {}) =>
    getData<PagedResult<Delivery>>(api.get('/deliveries/my-orders', { params })),
  get: (id: number) => getData<Delivery>(api.get(`/deliveries/${id}`)),
  start: (id: number) =>
    getData<Delivery>(api.post(`/deliveries/${id}/start`)),
  complete: (
    id: number,
    markAsPaid: boolean,
    containerReturns: { orderDetailId: number; quantity: number }[],
    note?: string,
  ) =>
    getData<Delivery>(
      api.post(`/deliveries/${id}/complete`, {
        markAsPaid,
        containerReturns,
        note,
      }),
    ),
  fail: (id: number, failureReason: string, note?: string) =>
    getData<Delivery>(
      api.post(`/deliveries/${id}/fail`, { failureReason, note }),
    ),
}

export const financeApi = {
  debts: (params: ListParams = {}) =>
    getData<PagedResult<Debt>>(api.get('/debts', { params })),
  debtSummary: () => getData<DebtSummary>(api.get('/debts/summary')),
  containerTransactions: (params: ListParams = {}) =>
    getData<PagedResult<ContainerTransaction>>(
      api.get('/containers/transactions', { params }),
    ),
}

export interface UserCreatePayload {
  fullName: string
  username: string
  password: string
  role: Role
}

export interface UserUpdatePayload {
  fullName: string
  username: string
  role: Role
  isActive: boolean
}

export const userApi = {
  list: (params: ListParams = {}) =>
    getData<PagedResult<User>>(api.get('/users', { params })),
  deliveryUsers: () =>
    getData<User[]>(api.get('/users/delivery-staff')),
  get: (id: number) => getData<User>(api.get(`/users/${id}`)),
  create: (payload: UserCreatePayload) =>
    getData<User>(api.post('/users', payload)),
  update: (id: number, payload: UserUpdatePayload) =>
    getData<User>(api.put(`/users/${id}`, payload)),
  changePassword: (id: number, newPassword: string) =>
    getData<void>(api.put(`/users/${id}/password`, { newPassword })),
  remove: (id: number) => getData<void>(api.delete(`/users/${id}`)),
}

export const dashboardApi = {
  getAll: async (): Promise<DashboardData> => {
    const [summary, revenueByMonth, ordersByStatus, topProducts, restockSuggestions] =
      await Promise.all([
        getData<DashboardSummary>(api.get('/dashboard/summary')),
        getData<RevenuePoint[]>(api.get('/dashboard/revenue')),
        getData<OrderStatusPoint[]>(api.get('/dashboard/order-status')),
        getData<TopProductPoint[]>(api.get('/dashboard/top-products')),
        getData<RestockSuggestion[]>(api.get('/dashboard/restock-suggestions')),
      ])
    return {
      summary,
      revenueByMonth,
      ordersByStatus,
      topProducts,
      restockSuggestions,
    }
  },
}
