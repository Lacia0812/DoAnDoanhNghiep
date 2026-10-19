export const WALK_IN_CUSTOMER_NAME = 'Khách vãng lai'

export const createWalkInCustomerCode = (timestamp = Date.now()) =>
  `KVL-${timestamp}`
