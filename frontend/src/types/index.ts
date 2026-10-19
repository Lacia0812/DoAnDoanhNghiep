export type Role = 'Admin' | 'Sales' | 'Warehouse' | 'Delivery'

export type OrderStatus =
  | 'New'
  | 'Confirmed'
  | 'Preparing'
  | 'ReadyForDelivery'
  | 'Delivering'
  | 'Delivered'
  | 'DeliveryFailed'
  | 'Cancelled'

export type DeliveryStatus =
  | 'Pending'
  | 'Assigned'
  | 'Delivering'
  | 'Completed'
  | 'Failed'

export type PaymentMethod = 'Cash' | 'BankTransfer' | 'CashOnDelivery'
export type PaymentStatus = 'Unpaid' | 'Partial' | 'Paid'
export type CustomerType =
  | 'Individual'
  | 'Dealer'
  | 'Company'
  | 'School'
  | 'Restaurant'
export type TransactionType = 'StockIn' | 'StockOut'

export interface User {
  id: number
  username: string
  fullName: string
  phoneNumber?: string | null
  email?: string | null
  role: Role
  isActive: boolean
  createdAt?: string
}

export interface LoginRequest {
  username: string
  password: string
}

export interface LoginResponse {
  token: string
  expiresAt: string
  user: User
}

export interface PagedResult<T> {
  items: T[]
  totalCount: number
  page: number
  pageSize: number
  totalPages: number
}

export interface ListParams {
  page?: number
  pageSize?: number
  search?: string
  [key: string]: string | number | boolean | undefined
}

export interface ProductCategory {
  id: number
  name: string
  description?: string | null
}

export interface Product {
  id: number
  productCode: string
  productName: string
  categoryId: number
  categoryName: string
  capacity: string
  unit: string
  price: number
  requiresContainer: boolean
  containerDeposit: number
  stockQuantity: number
  minimumStock: number
  isLowStock: boolean
  isActive: boolean
  createdAt: string
}

export interface ProductPayload {
  productCode: string
  productName: string
  categoryId: number
  capacity: string
  unit: string
  price: number
  requiresContainer: boolean
  containerDeposit: number
  minimumStock: number
  initialStock?: number
}

export interface Customer {
  id: number
  customerCode: string
  customerName: string
  customerType: CustomerType
  phone: string
  address: string
  email?: string | null
  containerBalance: number
  outstandingDebt: number
  note?: string | null
  isActive: boolean
  createdAt: string
}

export interface CustomerPayload {
  customerCode: string
  customerName: string
  customerType: CustomerType
  phone: string
  address: string
  email?: string | null
  note?: string
}

export interface OrderItem {
  id?: number
  productId: number
  productCode?: string
  productName: string
  unit?: string
  quantity: number
  unitPrice: number
  totalPrice: number
  requiresContainer: boolean
  containerDeposit: number
  returnedContainerQuantity: number
  containerDepositAmount: number
}

export interface DeliverySummary {
  id: number
  deliveryUserId: number
  deliveryUserName: string
  status: DeliveryStatus
  assignedAt: string
  startedAt?: string | null
  completedAt?: string | null
  failureReason?: string | null
  note?: string | null
}

export interface Delivery extends DeliverySummary {
  orderId: number
  orderCode: string
  order?: Order | null
}

export interface Order {
  id: number
  orderCode: string
  customerId: number
  customerName: string
  customerPhone: string
  createdByUserId: number
  createdByName: string
  deliveryUserId?: number | null
  deliveryUserName?: string | null
  orderDate: string
  expectedDeliveryDate?: string | null
  deliveryAddress: string
  orderStatus: OrderStatus
  paymentMethod: PaymentMethod
  paymentStatus: PaymentStatus
  productAmount: number
  depositAmount: number
  totalAmount: number
  paidAmount: number
  remainingAmount: number
  note?: string | null
  createdAt: string
  details: OrderItem[]
  payments: Payment[]
  delivery?: DeliverySummary | null
}

export interface Payment {
  id: number
  amount: number
  paymentMethod: PaymentMethod
  paidAt: string
  recordedByUserId: number
  recordedByName: string
  note?: string | null
}

export interface Debt {
  orderId: number
  orderCode: string
  customerId: number
  customerCode: string
  customerName: string
  customerPhone: string
  completedAt: string
  totalAmount: number
  paidAmount: number
  remainingAmount: number
  paymentStatus: PaymentStatus
}

export interface DebtSummary {
  totalDebt: number
  debtOrderCount: number
  debtCustomerCount: number
}

export interface ContainerTransaction {
  id: number
  customerId: number
  customerName: string
  productId: number
  productName: string
  orderId: number
  orderCode: string
  deliveredQuantity: number
  returnedQuantity: number
  balanceBefore: number
  balanceAfter: number
  createdAt: string
  createdByName: string
  note?: string | null
}

export interface OrderLinePayload {
  productId: number
  quantity: number
}

export interface OrderPayload {
  customerId: number
  expectedDeliveryDate?: string
  deliveryAddress: string
  paymentMethod: PaymentMethod
  note?: string
  items: OrderLinePayload[]
}

export interface InventoryItem extends Product {
  averageDailySales?: number
  daysRemaining?: number | null
}

export interface InventoryTransaction {
  id: number
  productId: number
  productCode: string
  productName: string
  transactionType: TransactionType
  quantity: number
  orderId?: number | null
  orderCode?: string | null
  quantityBefore: number
  quantityAfter: number
  note?: string | null
  createdByName?: string | null
  createdAt: string
}

export interface RestockSuggestion {
  productId: number
  productCode: string
  productName: string
  stockQuantity: number
  minimumStock: number
  averageDailySales: number
  daysRemaining: number | null
  needsRestock: boolean
  isBelowMinimum: boolean
}

export interface DashboardSummary {
  totalProducts: number
  totalCustomers: number
  totalOrders: number
  newOrders: number
  deliveringOrders: number
  deliveredOrders: number
  lowStockProducts: number
  monthlyRevenue: number
}

export interface RevenuePoint {
  month: string
  label: string
  revenue: number
}

export interface OrderStatusPoint {
  status: OrderStatus
  count: number
}

export interface TopProductPoint {
  productId: number
  productName: string
  quantitySold: number
  revenue: number
}

export interface DashboardData {
  summary: DashboardSummary
  revenueByMonth: RevenuePoint[]
  ordersByStatus: OrderStatusPoint[]
  topProducts: TopProductPoint[]
  restockSuggestions: RestockSuggestion[]
}

export interface ApiProblem {
  title?: string
  detail?: string
  status?: number
  errors?: Record<string, string[]>
  message?: string
}
