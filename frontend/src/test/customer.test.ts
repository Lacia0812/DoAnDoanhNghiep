import { describe, expect, it } from 'vitest'
import {
  createWalkInCustomerCode,
  WALK_IN_CUSTOMER_NAME,
} from '../utils/customer'

describe('tiện ích khách vãng lai', () => {
  it('tạo mã ổn định từ timestamp với tiền tố KVL và độ dài hợp lệ', () => {
    const timestamp = 1_720_000_000_000
    const code = createWalkInCustomerCode(timestamp)

    expect(code).toBe('KVL-1720000000000')
    expect(createWalkInCustomerCode(timestamp)).toBe(code)
    expect(code).toMatch(/^KVL-/)
    expect(code.length).toBeLessThanOrEqual(30)
  })

  it('cung cấp tên mặc định cho khách vãng lai', () => {
    expect(WALK_IN_CUSTOMER_NAME).toBe('Khách vãng lai')
  })
})
