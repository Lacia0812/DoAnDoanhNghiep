import { Tag } from 'antd'
import type { DeliveryStatus, OrderStatus, PaymentStatus } from '../types'
import {
  DELIVERY_STATUS_COLORS,
  DELIVERY_STATUS_LABELS,
  ORDER_STATUS_COLORS,
  ORDER_STATUS_LABELS,
  PAYMENT_STATUS_LABELS,
} from '../utils/constants'

export const OrderStatusTag = ({ status }: { status: OrderStatus }) => (
  <Tag color={ORDER_STATUS_COLORS[status] ?? 'default'}>
    {ORDER_STATUS_LABELS[status] ?? status}
  </Tag>
)

export const DeliveryStatusTag = ({ status }: { status: DeliveryStatus }) => (
  <Tag color={DELIVERY_STATUS_COLORS[status] ?? 'default'}>
    {DELIVERY_STATUS_LABELS[status] ?? status}
  </Tag>
)

export const PaymentStatusTag = ({ status }: { status: PaymentStatus }) => (
  <Tag color={status === 'Paid' ? 'success' : 'warning'}>
    {PAYMENT_STATUS_LABELS[status] ?? status}
  </Tag>
)
