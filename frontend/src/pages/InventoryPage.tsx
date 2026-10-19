import { useCallback, useEffect, useState } from 'react'
import {
  Alert,
  Button,
  Card,
  Form,
  Input,
  InputNumber,
  Modal,
  Select,
  Space,
  Table,
  Tabs,
  Tag,
  Tooltip,
  Typography,
  message,
  type TableColumnsType,
} from 'antd'
import {
  InboxOutlined,
  PlusOutlined,
  ReloadOutlined,
  SearchOutlined,
  WarningOutlined,
} from '@ant-design/icons'
import PageHeader from '../components/PageHeader'
import { useAuth } from '../contexts/AuthContext'
import {
  getApiErrorMessage,
  inventoryApi,
  productApi,
} from '../services/api'
import type {
  InventoryItem,
  InventoryTransaction,
  TransactionType,
} from '../types'
import {
  PAGE_SIZE_OPTIONS,
  TRANSACTION_TYPE_LABELS,
} from '../utils/constants'
import { formatCurrency, formatDateTime, formatNumber } from '../utils/format'

export default function InventoryPage() {
  const { user } = useAuth()
  const [stock, setStock] = useState<InventoryItem[]>([])
  const [transactions, setTransactions] = useState<InventoryTransaction[]>([])
  const [loading, setLoading] = useState(false)
  const [activeTab, setActiveTab] = useState('stock')
  const [stockSearch, setStockSearch] = useState('')
  const [transactionSearch, setTransactionSearch] = useState('')
  const [transactionType, setTransactionType] = useState<TransactionType>()
  const [stockPage, setStockPage] = useState(1)
  const [stockPageSize, setStockPageSize] = useState(10)
  const [stockTotal, setStockTotal] = useState(0)
  const [transactionPage, setTransactionPage] = useState(1)
  const [transactionPageSize, setTransactionPageSize] = useState(10)
  const [transactionTotal, setTransactionTotal] = useState(0)
  const [addStockProduct, setAddStockProduct] = useState<InventoryItem | null>(null)
  const [saving, setSaving] = useState(false)
  const [addStockForm] = Form.useForm<{ quantity: number; note?: string }>()

  const loadStock = useCallback(async () => {
    setLoading(true)
    try {
      const result = await inventoryApi.list({
        page: stockPage,
        pageSize: stockPageSize,
        search: stockSearch || undefined,
      })
      setStock(result.items)
      setStockTotal(result.totalCount)
    } catch (error) {
      message.error(getApiErrorMessage(error, 'Không thể tải tồn kho.'))
    } finally {
      setLoading(false)
    }
  }, [stockPage, stockPageSize, stockSearch])

  const loadTransactions = useCallback(async () => {
    setLoading(true)
    try {
      const result = await inventoryApi.transactions({
        page: transactionPage,
        pageSize: transactionPageSize,
        search: transactionSearch || undefined,
        transactionType,
      })
      setTransactions(result.items)
      setTransactionTotal(result.totalCount)
    } catch (error) {
      message.error(getApiErrorMessage(error, 'Không thể tải lịch sử kho.'))
    } finally {
      setLoading(false)
    }
  }, [
    transactionPage,
    transactionPageSize,
    transactionSearch,
    transactionType,
  ])

  useEffect(() => {
    if (activeTab === 'stock') loadStock()
    else loadTransactions()
  }, [activeTab, loadStock, loadTransactions])

  const addStock = async () => {
    if (!addStockProduct) return
    try {
      const values = await addStockForm.validateFields()
      setSaving(true)
      await productApi.addStock(
        addStockProduct.id,
        values.quantity,
        values.note,
      )
      message.success('Nhập thêm tồn kho thành công.')
      setAddStockProduct(null)
      addStockForm.resetFields()
      await loadStock()
    } catch (error) {
      if ((error as { errorFields?: unknown }).errorFields) return
      message.error(getApiErrorMessage(error, 'Không thể nhập thêm tồn kho.'))
    } finally {
      setSaving(false)
    }
  }

  const stockColumns: TableColumnsType<InventoryItem> = [
    {
      title: 'Mã',
      dataIndex: 'productCode',
      width: 120,
      fixed: 'left',
      render: (value) => <Typography.Text strong>{value}</Typography.Text>,
    },
    {
      title: 'Sản phẩm',
      dataIndex: 'productName',
      width: 250,
      render: (value, record) => (
        <div>
          <Typography.Text strong>{value}</Typography.Text>
          <div className="table-secondary">
            {[record.capacity, record.unit].filter(Boolean).join(' · ')}
          </div>
        </div>
      ),
    },
    { title: 'Loại', dataIndex: 'categoryName', width: 170 },
    {
      title: 'Giá bán',
      dataIndex: 'price',
      width: 140,
      align: 'right',
      render: formatCurrency,
    },
    {
      title: 'Tồn hiện tại',
      dataIndex: 'stockQuantity',
      width: 140,
      align: 'right',
      render: (value, record) => (
        <Typography.Text
          strong
          className={record.isLowStock ? 'stock-danger' : 'stock-ok'}
        >
          {formatNumber(value)} {record.unit}
        </Typography.Text>
      ),
    },
    {
      title: 'Mức tối thiểu',
      dataIndex: 'minimumStock',
      width: 140,
      align: 'right',
      render: (value, record) => `${formatNumber(value)} ${record.unit}`,
    },
    {
      title: 'Cảnh báo',
      dataIndex: 'isLowStock',
      width: 130,
      render: (value) =>
        value ? (
          <Tag color="error" icon={<WarningOutlined />}>
            Sắp hết
          </Tag>
        ) : (
          <Tag color="success">Ổn định</Tag>
        ),
    },
    ...(user?.role === 'Warehouse'
      ? ([
          {
            title: 'Thao tác',
            key: 'actions',
            fixed: 'right' as const,
            width: 120,
            render: (_: unknown, record: InventoryItem) => (
              <Tooltip title="Nhập thêm hàng">
                <Button
                  type="primary"
                  ghost
                  size="small"
                  icon={<PlusOutlined />}
                  onClick={() => {
                    addStockForm.resetFields()
                    setAddStockProduct(record)
                  }}
                >
                  Nhập kho
                </Button>
              </Tooltip>
            ),
          },
        ] as TableColumnsType<InventoryItem>)
      : []),
  ]

  const transactionColumns: TableColumnsType<InventoryTransaction> = [
    {
      title: 'Thời gian',
      dataIndex: 'createdAt',
      width: 165,
      fixed: 'left',
      render: formatDateTime,
    },
    {
      title: 'Sản phẩm',
      dataIndex: 'productName',
      width: 250,
      render: (value, record) => (
        <div>
          <Typography.Text strong>{value}</Typography.Text>
          <div className="table-secondary">{record.productCode}</div>
        </div>
      ),
    },
    {
      title: 'Loại',
      dataIndex: 'transactionType',
      width: 120,
      render: (value: TransactionType) => (
        <Tag color={value === 'StockIn' ? 'success' : 'orange'}>
          {TRANSACTION_TYPE_LABELS[value] ?? value}
        </Tag>
      ),
    },
    {
      title: 'Thay đổi',
      dataIndex: 'quantity',
      width: 110,
      align: 'right',
      render: (value, record) => (
        <Typography.Text
          strong
          type={record.transactionType === 'StockIn' ? 'success' : 'warning'}
        >
          {record.transactionType === 'StockIn' ? '+' : '−'}
          {formatNumber(value)}
        </Typography.Text>
      ),
    },
    {
      title: 'Trước',
      dataIndex: 'quantityBefore',
      width: 100,
      align: 'right',
    },
    {
      title: 'Sau',
      dataIndex: 'quantityAfter',
      width: 100,
      align: 'right',
    },
    {
      title: 'Đơn liên quan',
      dataIndex: 'orderCode',
      width: 140,
      render: (value) => value || '—',
    },
    {
      title: 'Người thực hiện',
      dataIndex: 'createdByName',
      width: 180,
    },
    {
      title: 'Ghi chú',
      dataIndex: 'note',
      width: 240,
      ellipsis: true,
      render: (value) => value || '—',
    },
  ]

  return (
    <>
      <PageHeader
        title="Kho hàng"
        subtitle="Theo dõi tồn hiện tại và toàn bộ lịch sử nhập xuất."
        actions={
          <Button
            icon={<ReloadOutlined />}
            onClick={activeTab === 'stock' ? loadStock : loadTransactions}
          >
            Làm mới
          </Button>
        }
      />
      {user?.role === 'Admin' && (
        <Alert
          type="info"
          showIcon
          message="Quản trị viên có quyền xem kho. Nghiệp vụ nhập và chuẩn bị hàng do nhân viên kho thực hiện."
          className="page-alert"
        />
      )}
      <Card className="content-card">
        <Tabs
          activeKey={activeTab}
          onChange={setActiveTab}
          items={[
            {
              key: 'stock',
              label: (
                <Space>
                  <InboxOutlined />
                  Tồn kho hiện tại
                </Space>
              ),
              children: (
                <>
                  <div className="table-toolbar">
                    <Input.Search
                      allowClear
                      placeholder="Tìm mã hoặc tên sản phẩm"
                      enterButton="Tìm kiếm"
                      onSearch={(value) => {
                        setStockPage(1)
                        setStockSearch(value.trim())
                      }}
                      style={{ maxWidth: 380 }}
                    />
                  </div>
                  <Table
                    rowKey="id"
                    columns={stockColumns}
                    dataSource={stock}
                    loading={loading}
                    rowClassName={(record) =>
                      record.isLowStock ? 'low-stock-row' : ''
                    }
                    scroll={{ x: 1150 }}
                    pagination={{
                      current: stockPage,
                      pageSize: stockPageSize,
                      total: stockTotal,
                      showSizeChanger: true,
                      pageSizeOptions: PAGE_SIZE_OPTIONS,
                      showTotal: (count) => `${count} sản phẩm`,
                      onChange: (nextPage, nextSize) => {
                        setStockPage(
                          nextSize !== stockPageSize ? 1 : nextPage,
                        )
                        setStockPageSize(nextSize)
                      },
                    }}
                  />
                </>
              ),
            },
            {
              key: 'transactions',
              label: 'Lịch sử thay đổi',
              children: (
                <>
                  <div className="table-toolbar">
                    <Space wrap>
                      <Input
                        allowClear
                        prefix={<SearchOutlined />}
                        placeholder="Tìm sản phẩm hoặc mã đơn"
                        value={transactionSearch}
                        onChange={(event) =>
                          setTransactionSearch(event.target.value)
                        }
                        onPressEnter={() => {
                          setTransactionPage(1)
                          loadTransactions()
                        }}
                        style={{ width: 260 }}
                      />
                      <Select
                        allowClear
                        placeholder="Tất cả loại thay đổi"
                        value={transactionType}
                        options={[
                          { value: 'StockIn', label: 'Nhập kho' },
                          { value: 'StockOut', label: 'Xuất kho' },
                        ]}
                        onChange={(value) => {
                          setTransactionPage(1)
                          setTransactionType(value)
                        }}
                        style={{ width: 190 }}
                      />
                    </Space>
                  </div>
                  <Table
                    rowKey="id"
                    columns={transactionColumns}
                    dataSource={transactions}
                    loading={loading}
                    scroll={{ x: 1400 }}
                    pagination={{
                      current: transactionPage,
                      pageSize: transactionPageSize,
                      total: transactionTotal,
                      showSizeChanger: true,
                      pageSizeOptions: PAGE_SIZE_OPTIONS,
                      showTotal: (count) => `${count} giao dịch`,
                      onChange: (nextPage, nextSize) => {
                        setTransactionPage(
                          nextSize !== transactionPageSize ? 1 : nextPage,
                        )
                        setTransactionPageSize(nextSize)
                      },
                    }}
                  />
                </>
              ),
            },
          ]}
        />
      </Card>

      <Modal
        title={`Nhập kho · ${addStockProduct?.productName ?? ''}`}
        open={Boolean(addStockProduct)}
        onCancel={() => setAddStockProduct(null)}
        onOk={addStock}
        okText="Xác nhận nhập"
        cancelText="Hủy"
        confirmLoading={saving}
        destroyOnHidden
      >
        {addStockProduct && (
          <Alert
            type="info"
            showIcon
            message={`Tồn hiện tại: ${formatNumber(addStockProduct.stockQuantity)} ${addStockProduct.unit}`}
            className="modal-alert"
          />
        )}
        <Form form={addStockForm} layout="vertical" requiredMark="optional">
          <Form.Item
            name="quantity"
            label="Số lượng nhập thêm"
            rules={[
              { required: true, message: 'Vui lòng nhập số lượng.' },
              {
                type: 'integer',
                min: 1,
                message: 'Số lượng phải là số nguyên lớn hơn 0.',
              },
            ]}
          >
            <InputNumber
              min={1}
              precision={0}
              style={{ width: '100%' }}
              addonAfter={addStockProduct?.unit}
            />
          </Form.Item>
          <Form.Item name="note" label="Ghi chú">
            <Input.TextArea rows={3} maxLength={500} showCount />
          </Form.Item>
        </Form>
      </Modal>
    </>
  )
}
