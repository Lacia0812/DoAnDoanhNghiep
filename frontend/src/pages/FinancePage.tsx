import { useCallback, useEffect, useState } from 'react'
import {
  Button,
  Card,
  Col,
  Form,
  Input,
  InputNumber,
  Modal,
  Row,
  Select,
  Space,
  Statistic,
  Table,
  Tabs,
  Tag,
  Typography,
  message,
  type TableColumnsType,
} from 'antd'
import { DollarOutlined, MinusCircleOutlined, PlusCircleOutlined, ReloadOutlined, SearchOutlined } from '@ant-design/icons'
import PageHeader from '../components/PageHeader'
import { PaymentStatusTag } from '../components/StatusTag'
import { customerApi, financeApi, getApiErrorMessage, orderApi, productApi } from '../services/api'
import { useAuth } from '../contexts/AuthContext'
import type {
  ContainerTransaction,
  Debt,
  DebtSummary,
  PaymentMethod,
  Customer,
  Product,
} from '../types'
import { PAGE_SIZE_OPTIONS, PAYMENT_METHOD_LABELS } from '../utils/constants'
import { formatCurrency, formatDateTime } from '../utils/format'

interface PaymentFormValues {
  amount: number
  paymentMethod: PaymentMethod
  note?: string
}

interface ContainerFormValues {
  customerId: number
  productId: number
  quantity: number
  note: string
}

export default function FinancePage() {
  const [form] = Form.useForm<PaymentFormValues>()
  const [containerForm] = Form.useForm<ContainerFormValues>()
  const { user } = useAuth()
  const [debts, setDebts] = useState<Debt[]>([])
  const [transactions, setTransactions] = useState<ContainerTransaction[]>([])
  const [summary, setSummary] = useState<DebtSummary>()
  const [loading, setLoading] = useState(false)
  const [saving, setSaving] = useState(false)
  const [selectedDebt, setSelectedDebt] = useState<Debt | null>(null)
  const [searchDraft, setSearchDraft] = useState('')
  const [search, setSearch] = useState('')
  const [page, setPage] = useState(1)
  const [pageSize, setPageSize] = useState(10)
  const [total, setTotal] = useState(0)
  const [containerPage, setContainerPage] = useState(1)
  const [containerTotal, setContainerTotal] = useState(0)
  const [containerAction, setContainerAction] = useState<'return' | 'adjust'>()
  const [customers, setCustomers] = useState<Customer[]>([])
  const [products, setProducts] = useState<Product[]>([])

  const loadDebts = useCallback(async () => {
    setLoading(true)
    try {
      const [result, summaryResult] = await Promise.all([
        financeApi.debts({ page, pageSize, search: search || undefined }),
        financeApi.debtSummary(),
      ])
      setDebts(result.items)
      setTotal(result.totalCount)
      setSummary(summaryResult)
    } catch (error) {
      message.error(getApiErrorMessage(error, 'Không thể tải công nợ.'))
    } finally {
      setLoading(false)
    }
  }, [page, pageSize, search])

  const loadContainers = useCallback(async () => {
    try {
      const result = await financeApi.containerTransactions({
        page: containerPage,
        pageSize,
      })
      setTransactions(result.items)
      setContainerTotal(result.totalCount)
    } catch (error) {
      message.error(getApiErrorMessage(error, 'Không thể tải lịch sử vỏ bình.'))
    }
  }, [containerPage, pageSize])

  useEffect(() => {
    loadDebts()
  }, [loadDebts])
  useEffect(() => {
    loadContainers()
  }, [loadContainers])

  const openPayment = (debt: Debt) => {
    setSelectedDebt(debt)
    form.setFieldsValue({
      amount: debt.remainingAmount,
      paymentMethod: 'BankTransfer',
      note: undefined,
    })
  }

  const submitPayment = async () => {
    if (!selectedDebt) return
    try {
      const values = await form.validateFields()
      setSaving(true)
      await orderApi.recordPayment(
        selectedDebt.orderId,
        values.amount,
        values.paymentMethod,
        values.note?.trim(),
      )
      message.success('Đã ghi nhận thanh toán.')
      setSelectedDebt(null)
      await loadDebts()
    } catch (error) {
      if ((error as { errorFields?: unknown }).errorFields) return
      message.error(getApiErrorMessage(error, 'Không thể ghi nhận thanh toán.'))
    } finally {
      setSaving(false)
    }
  }

  const openContainerAction = async (action: 'return' | 'adjust') => {
    try {
      setSaving(true)
      const [customerResult, productResult] = await Promise.all([customerApi.active(), productApi.active()])
      setCustomers(customerResult.items)
      setProducts(productResult.items.filter((item) => item.requiresContainer))
      containerForm.resetFields()
      setContainerAction(action)
    } catch (error) {
      message.error(getApiErrorMessage(error, 'Không thể tải dữ liệu vỏ bình.'))
    } finally { setSaving(false) }
  }

  const submitContainerAction = async () => {
    if (!containerAction) return
    try {
      const values = await containerForm.validateFields()
      setSaving(true)
      if (containerAction === 'return') {
        await financeApi.returnContainers({ customerId: values.customerId, productId: values.productId, quantity: values.quantity, note: values.note?.trim() })
        message.success('Đã ghi nhận khách trả vỏ.')
      } else {
        await financeApi.adjustContainers({ customerId: values.customerId, productId: values.productId, quantityChange: values.quantity, reason: values.note.trim() })
        message.success('Đã điều chỉnh số dư vỏ.')
      }
      setContainerAction(undefined)
      await loadContainers()
    } catch (error) {
      if ((error as { errorFields?: unknown }).errorFields) return
      message.error(getApiErrorMessage(error, 'Không thể cập nhật vỏ bình.'))
    } finally { setSaving(false) }
  }

  const debtColumns: TableColumnsType<Debt> = [
    {
      title: 'Đơn hàng',
      dataIndex: 'orderCode',
      width: 150,
      render: (value) => <Typography.Text strong>{value}</Typography.Text>,
    },
    {
      title: 'Khách hàng',
      dataIndex: 'customerName',
      width: 240,
      render: (value, record) => (
        <div>
          <Typography.Text strong>{value}</Typography.Text>
          <div className="table-secondary">{record.customerCode} · {record.customerPhone}</div>
        </div>
      ),
    },
    { title: 'Ngày giao', dataIndex: 'completedAt', width: 170, render: formatDateTime },
    { title: 'Tổng tiền', dataIndex: 'totalAmount', align: 'right', render: formatCurrency },
    { title: 'Đã trả', dataIndex: 'paidAmount', align: 'right', render: formatCurrency },
    {
      title: 'Còn nợ',
      dataIndex: 'remainingAmount',
      align: 'right',
      render: (value) => <Typography.Text type="danger" strong>{formatCurrency(value)}</Typography.Text>,
    },
    { title: 'Trạng thái', dataIndex: 'paymentStatus', render: (value) => <PaymentStatusTag status={value} /> },
    {
      title: 'Thao tác',
      fixed: 'right',
      width: 140,
      render: (_, record) => (
        <Button type="primary" icon={<DollarOutlined />} onClick={() => openPayment(record)}>
          Thu tiền
        </Button>
      ),
    },
  ]

  const containerColumns: TableColumnsType<ContainerTransaction> = [
    { title: 'Thời gian', dataIndex: 'createdAt', width: 170, render: formatDateTime },
    { title: 'Loại', dataIndex: 'transactionType', width: 125, render: (value) => <Tag color={value === 'Delivery' ? 'blue' : value === 'Return' ? 'green' : 'orange'}>{value === 'Delivery' ? 'Theo đơn' : value === 'Return' ? 'Trả vỏ' : 'Điều chỉnh'}</Tag> },
    { title: 'Đơn hàng', dataIndex: 'orderCode', width: 150, render: (value) => value || 'Ngoài đơn' },
    { title: 'Khách hàng', dataIndex: 'customerName', width: 230 },
    { title: 'Sản phẩm', dataIndex: 'productName', width: 220 },
    { title: 'Vỏ giao', dataIndex: 'deliveredQuantity', align: 'right', width: 90 },
    { title: 'Thay đổi', dataIndex: 'quantityChange', align: 'right', width: 100, render: (value) => <Typography.Text type={value < 0 ? 'success' : undefined} strong>{value > 0 ? `+${value}` : value}</Typography.Text> },
    { title: 'Nợ trước', dataIndex: 'balanceBefore', align: 'right', width: 90 },
    {
      title: 'Nợ sau',
      dataIndex: 'balanceAfter',
      align: 'right',
      width: 90,
      render: (value) => <Typography.Text strong>{value}</Typography.Text>,
    },
    { title: 'Người ghi nhận', dataIndex: 'createdByName', width: 180 },
  ]

  return (
    <>
      <PageHeader
        title="Công nợ và vỏ bình"
        subtitle="Theo dõi khoản phải thu, lịch sử thanh toán và số vỏ khách đang giữ."
        actions={<Space wrap>
          <Button icon={<MinusCircleOutlined />} onClick={() => openContainerAction('return')}>Khách trả vỏ</Button>
          {user?.role === 'Admin' && <Button icon={<PlusCircleOutlined />} onClick={() => openContainerAction('adjust')}>Điều chỉnh vỏ</Button>}
        </Space>}
      />
      <Row gutter={[16, 16]} className="summary-grid">
        <Col xs={24} md={8}><Card><Statistic title="Tổng công nợ" value={summary?.totalDebt ?? 0} formatter={(value) => formatCurrency(Number(value))} /></Card></Col>
        <Col xs={12} md={8}><Card><Statistic title="Đơn còn nợ" value={summary?.debtOrderCount ?? 0} /></Card></Col>
        <Col xs={12} md={8}><Card><Statistic title="Khách còn nợ" value={summary?.debtCustomerCount ?? 0} /></Card></Col>
      </Row>
      <Card className="content-card">
        <Tabs
          items={[
            {
              key: 'debts',
              label: 'Công nợ',
              children: (
                <>
                  <div className="table-toolbar">
                    <Space wrap>
                      <Input
                        allowClear
                        prefix={<SearchOutlined />}
                        value={searchDraft}
                        placeholder="Mã đơn, khách hàng, số điện thoại"
                        onChange={(event) => setSearchDraft(event.target.value)}
                        onPressEnter={() => { setPage(1); setSearch(searchDraft.trim()) }}
                        style={{ width: 310 }}
                      />
                      <Button icon={<SearchOutlined />} onClick={() => { setPage(1); setSearch(searchDraft.trim()) }}>Tìm kiếm</Button>
                    </Space>
                    <Button icon={<ReloadOutlined />} onClick={loadDebts}>Làm mới</Button>
                  </div>
                  <Table
                    rowKey="orderId"
                    loading={loading}
                    columns={debtColumns}
                    dataSource={debts}
                    scroll={{ x: 1250 }}
                    pagination={{
                      current: page, pageSize, total, showSizeChanger: true,
                      pageSizeOptions: PAGE_SIZE_OPTIONS,
                      onChange: (next, size) => { setPage(size !== pageSize ? 1 : next); setPageSize(size) },
                    }}
                  />
                </>
              ),
            },
            {
              key: 'containers',
              label: 'Lịch sử vỏ bình',
              children: (
                <Table
                  rowKey="id"
                  columns={containerColumns}
                  dataSource={transactions}
                  scroll={{ x: 1250 }}
                  pagination={{
                    current: containerPage, pageSize, total: containerTotal,
                    showSizeChanger: true, pageSizeOptions: PAGE_SIZE_OPTIONS,
                    onChange: (next, size) => { setContainerPage(size !== pageSize ? 1 : next); setPageSize(size) },
                  }}
                />
              ),
            },
          ]}
        />
      </Card>
      <Modal
        title={`Ghi nhận thanh toán · ${selectedDebt?.orderCode ?? ''}`}
        open={Boolean(selectedDebt)}
        onCancel={() => setSelectedDebt(null)}
        onOk={submitPayment}
        okText="Xác nhận thu tiền"
        cancelText="Hủy"
        confirmLoading={saving}
        destroyOnHidden
      >
        <Form form={form} layout="vertical">
          <Form.Item
            name="amount"
            label="Số tiền"
            rules={[{ required: true, message: 'Vui lòng nhập số tiền.' }]}
          >
            <InputNumber min={1} max={selectedDebt?.remainingAmount} precision={0} addonAfter="₫" style={{ width: '100%' }} />
          </Form.Item>
          <Form.Item name="paymentMethod" label="Phương thức" rules={[{ required: true }]}>
            <Select options={Object.entries(PAYMENT_METHOD_LABELS).map(([value, label]) => ({ value, label }))} />
          </Form.Item>
          <Form.Item name="note" label="Ghi chú">
            <Input.TextArea rows={3} maxLength={500} showCount />
          </Form.Item>
        </Form>
      </Modal>
      <Modal
        title={containerAction === 'return' ? 'Ghi nhận khách trả vỏ' : 'Điều chỉnh số dư vỏ'}
        open={Boolean(containerAction)}
        onCancel={() => setContainerAction(undefined)}
        onOk={submitContainerAction}
        okText="Lưu phiếu"
        cancelText="Hủy"
        confirmLoading={saving}
        destroyOnHidden
      >
        <Form form={containerForm} layout="vertical">
          <Form.Item name="customerId" label="Khách hàng" rules={[{ required: true, message: 'Vui lòng chọn khách hàng.' }]}>
            <Select showSearch optionFilterProp="label" options={customers.map((item) => ({ value: item.id, label: `${item.customerCode} · ${item.customerName} · đang giữ ${item.containerBalance} vỏ` }))} />
          </Form.Item>
          <Form.Item name="productId" label="Loại vỏ / sản phẩm" rules={[{ required: true, message: 'Vui lòng chọn sản phẩm.' }]}>
            <Select showSearch optionFilterProp="label" options={products.map((item) => ({ value: item.id, label: `${item.productCode} · ${item.productName}` }))} />
          </Form.Item>
          <Form.Item
            name="quantity"
            label={containerAction === 'return' ? 'Số vỏ trả' : 'Mức điều chỉnh (+ tăng nợ, - giảm nợ)'}
            rules={[{ required: true, message: 'Vui lòng nhập số lượng.' }, ...(containerAction === 'adjust' ? [{ validator: (_: unknown, value: number) => value === 0 ? Promise.reject(new Error('Mức điều chỉnh phải khác 0.')) : Promise.resolve() }] : [])]}
          >
            <InputNumber precision={0} min={containerAction === 'return' ? 1 : -1000000} max={1000000} style={{ width: '100%' }} />
          </Form.Item>
          <Form.Item name="note" label={containerAction === 'adjust' ? 'Lý do điều chỉnh' : 'Ghi chú'} rules={containerAction === 'adjust' ? [{ required: true, min: 3, message: 'Vui lòng nhập lý do tối thiểu 3 ký tự.' }] : undefined}>
            <Input.TextArea rows={3} maxLength={500} showCount />
          </Form.Item>
        </Form>
      </Modal>
    </>
  )
}
