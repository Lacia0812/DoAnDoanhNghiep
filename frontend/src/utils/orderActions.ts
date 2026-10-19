import type { OrderStatus, Role } from '../types'

export type OrderActionKey =
  | 'view'
  | 'edit'
  | 'confirm'
  | 'cancel'
  | 'prepare'
  | 'ready'
  | 'assign'

export const getOrderActionKeys = (
  role: Role,
  status: OrderStatus,
): OrderActionKey[] => {
  const actions: OrderActionKey[] = ['view']

  if (role === 'Sales' && status === 'New') {
    actions.push('edit', 'confirm', 'cancel')
  }
  if (role === 'Warehouse' && status === 'Confirmed') {
    actions.push('prepare')
  }
  if (role === 'Warehouse' && status === 'Preparing') {
    actions.push('ready')
  }
  if (
    (role === 'Admin' || role === 'Sales') &&
    status === 'ReadyForDelivery'
  ) {
    actions.push('assign')
  }

  return actions
}
