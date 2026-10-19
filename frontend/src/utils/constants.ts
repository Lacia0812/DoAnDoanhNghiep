import type {
  CustomerType,
  DeliveryStatus,
  OrderStatus,
  PaymentMethod,
  PaymentStatus,
  Role,
  TransactionType,
} from '../types'

export const ROLE_LABELS: Record<Role, string> = {
  Admin: 'Quản trị viên',
  Sales: 'Nhân viên bán hàng',
  Warehouse: 'Nhân viên kho',
  Delivery: 'Nhân viên giao hàng',
}

export const ROLE_OPTIONS = Object.entries(ROLE_LABELS).map(([value, label]) => ({
  value: value as Role,
  label,
}))

export const ORDER_STATUS_LABELS: Record<OrderStatus, string> = {
  New: 'Mới',
  Confirmed: 'Đã xác nhận',
  Preparing: 'Đang chuẩn bị',
  ReadyForDelivery: 'Sẵn sàng giao',
  Delivering: 'Đang giao',
  Delivered: 'Đã giao',
  DeliveryFailed: 'Giao thất bại',
  Cancelled: 'Đã hủy',
}

export const ORDER_STATUS_COLORS: Record<OrderStatus, string> = {
  New: 'blue',
  Confirmed: 'geekblue',
  Preparing: 'gold',
  ReadyForDelivery: 'cyan',
  Delivering: 'processing',
  Delivered: 'success',
  DeliveryFailed: 'error',
  Cancelled: 'default',
}

export const DELIVERY_STATUS_LABELS: Record<DeliveryStatus, string> = {
  Pending: 'Chờ phân công',
  Assigned: 'Đã phân công',
  Delivering: 'Đang giao',
  Completed: 'Đã giao',
  Failed: 'Thất bại',
}

export const DELIVERY_STATUS_COLORS: Record<DeliveryStatus, string> = {
  Pending: 'default',
  Assigned: 'blue',
  Delivering: 'processing',
  Completed: 'success',
  Failed: 'error',
}

export const PAYMENT_METHOD_LABELS: Record<PaymentMethod, string> = {
  Cash: 'Tiền mặt',
  BankTransfer: 'Chuyển khoản',
  CashOnDelivery: 'Thanh toán khi nhận hàng',
}

export const PAYMENT_STATUS_LABELS: Record<PaymentStatus, string> = {
  Unpaid: 'Chưa thanh toán',
  Partial: 'Thanh toán một phần',
  Paid: 'Đã thanh toán',
}

export const TRANSACTION_TYPE_LABELS: Record<TransactionType, string> = {
  StockIn: 'Nhập kho',
  StockOut: 'Xuất kho',
}

export const CUSTOMER_TYPE_LABELS: Record<CustomerType, string> = {
  Individual: 'Cá nhân',
  Dealer: 'Đại lý',
  Company: 'Công ty',
  School: 'Trường học',
  Restaurant: 'Nhà hàng',
}

export const CUSTOMER_TYPE_OPTIONS = [
  { value: 'Individual', label: 'Cá nhân' },
  { value: 'Dealer', label: 'Đại lý' },
  { value: 'Company', label: 'Công ty' },
  { value: 'School', label: 'Trường học' },
  { value: 'Restaurant', label: 'Nhà hàng' },
]

export const PAGE_SIZE_OPTIONS = ['10', '20', '50', '100']

export const ROLE_HOME: Record<Role, string> = {
  Admin: '/dashboard',
  Sales: '/orders',
  Warehouse: '/inventory',
  Delivery: '/deliveries',
}

export const getCustomerTypeLabel = (value: CustomerType) =>
  CUSTOMER_TYPE_LABELS[value] ?? value
