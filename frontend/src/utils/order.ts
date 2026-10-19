import type { Product } from '../types'

export interface PricedOrderLine {
  productId?: number
  quantity?: number
}

export const calculateOrderTotal = (
  lines: PricedOrderLine[],
  products: Pick<Product, 'id' | 'price'>[],
) =>
  lines.reduce((total, line) => {
    const product = products.find((item) => item.id === line.productId)
    return total + (product?.price ?? 0) * (line.quantity ?? 0)
  }, 0)

export const hasDuplicateProducts = (lines: PricedOrderLine[]) => {
  const ids = lines
    .map((line) => line.productId)
    .filter((value): value is number => typeof value === 'number')
  return new Set(ids).size !== ids.length
}
