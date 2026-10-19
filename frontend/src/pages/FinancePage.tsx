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
  Typography,
  message,
  type TableColumnsType,
} from 'antd'
import { DollarOutlined, ReloadOutlined, SearchOutlined } from '@ant-design/icons'
import PageHeader from '../components/PageHeader'
import { PaymentStatusTag } from '../components/StatusTag'
import { financeApi, getApiErrorMessage, orderApi } from '../services/api'
import type {
  ContainerTransaction,
  Debt,
  DebtSummary,
  PaymentMethod,
} from '../types'
import { PAGE_SIZE_OPTIONS, PAYMENT_METHOD_LABELS } from '../utils/constants'
import { formatCurrency, formatDateTime } from '../utils/format'

interface PaymentFormValues {
  amount: number
  paymentMethod: PaymentMethod
  note?: string
}

export default function FinancePage() {
  const [form] = Form.useForm<PaymentFormValues>()
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
    { title: 'Đơn hàng', dataIndex: 'orderCode', width: 150 },
    { title: 'Khách hàng', dataIndex: 'customerName', width: 230 },
    { title: 'Sản phẩm', dataIndex: 'productName', width: 220 },
    { title: 'Vỏ giao', dataIndex: 'deliveredQuantity', align: 'right', width: 90 },
    { title: 'Vỏ thu', dataIndex: 'returnedQuantity', align: 'right', width: 90 },
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
    </>
  )
}
