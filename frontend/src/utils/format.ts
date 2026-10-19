import dayjs from 'dayjs'

export const formatCurrency = (value?: number | null) =>
  new Intl.NumberFormat('vi-VN', {
    style: 'currency',
    currency: 'VND',
    maximumFractionDigits: 0,
  }).format(value ?? 0)

export const formatNumber = (value?: number | null, maximumFractionDigits = 0) =>
  new Intl.NumberFormat('vi-VN', { maximumFractionDigits }).format(value ?? 0)

export const formatDateTime = (value?: string | null) =>
  value ? dayjs(value).format('DD/MM/YYYY HH:mm') : '—'

export const formatDate = (value?: string | null) =>
  value ? dayjs(value).format('DD/MM/YYYY') : '—'
