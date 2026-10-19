import { useCallback, useEffect, useState } from 'react'
import {
  Button,
  Card,
  DatePicker,
  Form,
  Input,
  Modal,
  Select,
  Space,
  Table,
  Tooltip,
  Typography,
  message,
  type TableColumnsType,
} from 'antd'
import {
  CarOutlined,
  CheckCircleOutlined,
  CloseCircleOutlined,
  EditOutlined,
  EyeOutlined,
  InboxOutlined,
  PlusOutlined,
  ReloadOutlined,
  SearchOutlined,
} from '@ant-design/icons'
import dayjs, { type Dayjs } from 'dayjs'
import { useNavigate } from 'react-router-dom'
import PageHeader from '../components/PageHeader'
import { OrderStatusTag, PaymentStatusTag } from '../components/StatusTag'
import { useAuth } from '../contexts/AuthContext'
import { getApiErrorMessage, orderApi, userApi } from '../services/api'
import type { Order, OrderStatus, User } from '../types'
import {
  ORDER_STATUS_LABELS,
  PAGE_SIZE_OPTIONS,
  PAYMENT_METHOD_LABELS,
} from '../utils/constants'
import { formatCurrency, formatDateTime } from '../utils/format'
import { getOrderActionKeys } from '../utils/orderActions'

type ActionName = 'confirm' | 'prepare' | 'ready' | 'cancel'

export default function OrdersPage() {
  const navigate = useNavigate()
  const { user } = useAuth()
  const [orders, setOrders] = useState<Order[]>([])
  const [loading, setLoading] = useState(false)
  const [actionLoading, setActionLoading] = useState<number>()
  const [searchDraft, setSearchDraft] = useState('')
  const [search, setSearch] = useState('')
  const [status, setStatus] = useState<OrderStatus>()
  const [dateRange, setDateRange] = useState<[Dayjs | null, Dayjs | null] | null>(
    null,
  )
  const [page, setPage] = useState(1)
  const [pageSize, setPageSize] = useState(10)
  const [total, setTotal] = useState(0)
  const [assignOrder, setAssignOrder] = useState<Order | null>(null)
  const [deliveryUsers, setDeliveryUsers] = useState<User[]>([])
  const [assignForm] = Form.useForm<{ deliveryUserId: number }>()

  const loadOrders = useCallback(async () => {
    setLoading(true)
    try {
      const result = await orderApi.list({
        page,
        pageSize,
        search: search || undefined,
        status,
        fromDate: dateRange?.[0]?.format('YYYY-MM-DD'),
        toDate: dateRange?.[1]?.format('YYYY-MM-DD'),
      })
      setOrders(result.items)
      setTotal(result.totalCount)
    } catch (error) {
      message.error(getApiErrorMessage(error, 'Không thể tải danh sách đơn hàng.'))
    } finally {
      setLoading(false)
    }
  }, [dateRange, page, pageSize, search, status])

  useEffect(() => {
    loadOrders()
  }, [loadOrders])

  const runAction = (
    order: Order,
    action: ActionName,
    title: string,
    content: string,
  ) => {
    Modal.confirm({
      title,
      content,
      okText: 'Xác nhận',
      cancelText: 'Bỏ qua',
      okButtonProps: action === 'cancel' ? { danger: true } : undefined,
      async onOk() {
        setActionLoading(order.id)
        try {
          await orderApi[action](order.id)
          message.success('Cập nhật trạng thái đơn hàng thành công.')
          await loadOrders()
        } catch (error) {
          message.error(getApiErrorMessage(error, 'Không thể cập nhật đơn hàng.'))
          throw error
        } finally {
          setActionLoading(undefined)
        }
      },
    })
  }

  const openAssign = async (order: Order) => {
    setAssignOrder(order)
    assignForm.setFieldValue('deliveryUserId', order.deliveryUserId ?? undefined)
    try {
      const result = await userApi.deliveryUsers()
      setDeliveryUsers(result)
    } catch (error) {
      message.error(
        getApiErrorMessage(error, 'Không thể tải nhân viên giao hàng.'),
      )
    }
  }

  const assign = async () => {
    if (!assignOrder) return
    try {
      const values = await assignForm.validateFields()
      setActionLoading(assignOrder.id)
      await orderApi.assignDelivery(assignOrder.id, values.deliveryUserId)
      message.success('Phân công giao hàng thành công.')
      setAssignOrder(null)
      await loadOrders()
    } catch (error) {
      if ((error as { errorFields?: unknown }).errorFields) return
      message.error(getApiErrorMessage(error, 'Không thể phân công giao hàng.'))
    } finally {
      setActionLoading(undefined)
    }
  }

  const renderActions = (order: Order) => {
    if (!user) return null
    const actions = getOrderActionKeys(user.role, order.orderStatus)

    return (
      <Space size={2}>
        <Tooltip title="Xem chi tiết">
          <Button
            type="text"
            icon={<EyeOutlined />}
            onClick={() => navigate(`/orders/${order.id}`)}
          />
        </Tooltip>
        {actions.includes('edit') && (
          <>
            <Tooltip title="Chỉnh sửa">
              <Button
                type="text"
                icon={<EditOutlined />}
                onClick={() => navigate(`/orders/${order.id}/edit`)}
              />
            </Tooltip>
            <Tooltip title="Xác nhận đơn">
              <Button
                type="text"
                className="text-success"
                icon={<CheckCircleOutlined />}
                loading={actionLoading === order.id}
                onClick={() =>
                  runAction(
                    order,
                    'confirm',
                    'Xác nhận đơn hàng?',
                    'Hệ thống sẽ kiểm tra tồn kho hiện tại. Hàng chưa bị trừ ở bước này.',
                  )
                }
              />
            </Tooltip>
            <Tooltip title="Hủy đơn">
              <Button
                type="text"
                danger
                icon={<CloseCircleOutlined />}
                onClick={() =>
                  runAction(
                    order,
                    'cancel',
                    'Hủy đơn hàng?',
                    'Đơn đã hủy không thể tiếp tục xử lý.',
                  )
                }
              />
            </Tooltip>
          </>
        )}
        {actions.includes('prepare') && (
          <Tooltip title="Bắt đầu chuẩn bị và trừ kho">
            <Button
              type="text"
              icon={<InboxOutlined />}
              loading={actionLoading === order.id}
              onClick={() =>
                runAction(
                  order,
                  'prepare',
                  'Bắt đầu chuẩn bị hàng?',
                  'Tồn kho sẽ được kiểm tra lại và trừ theo chi tiết đơn.',
                )
              }
            />
          </Tooltip>
        )}
        {actions.includes('ready') && (
          <Tooltip title="Đánh dấu sẵn sàng giao">
            <Button
              type="text"
              className="text-success"
              icon={<CheckCircleOutlined />}
              loading={actionLoading === order.id}
              onClick={() =>
                runAction(
                  order,
                  'ready',
                  'Xác nhận đã chuẩn bị xong?',
                  'Đơn hàng sẽ chuyển sang trạng thái sẵn sàng giao.',
                )
              }
            />
          </Tooltip>
        )}
        {actions.includes('assign') && (
          <Tooltip title={order.deliveryUserId ? 'Đổi người giao' : 'Phân công giao'}>
            <Button
              type="text"
              icon={<CarOutlined />}
              onClick={() => openAssign(order)}
            />
          </Tooltip>
        )}
      </Space>
    )
  }

  const columns: TableColumnsType<Order> = [
    {
      title: 'Mã đơn',
      dataIndex: 'orderCode',
      width: 135,
      fixed: 'left',
      render: (value, record) => (
        <Button type="link" className="table-link" onClick={() => navigate(`/orders/${record.id}`)}>
          {value}
        </Button>
      ),
    },
    {
      title: 'Khách hàng',
      dataIndex: 'customerName',
      width: 220,
      render: (value, record) => (
        <div>
          <Typography.Text strong>{value}</Typography.Text>
          <div className="table-secondary">{record.deliveryAddress}</div>
        </div>
      ),
    },
    {
      title: 'Ngày đặt',
      dataIndex: 'orderDate',
      width: 160,
      render: formatDateTime,
    },
    {
      title: 'Người tạo',
      dataIndex: 'createdByName',
      width: 170,
    },
    {
      title: 'Tổng tiền',
      dataIndex: 'totalAmount',
      width: 140,
      align: 'right',
      render: (value) => (
        <Typography.Text strong>{formatCurrency(value)}</Typography.Text>
      ),
    },
    {
      title: 'Thanh toán',
      key: 'payment',
      width: 180,
      render: (_, record) => (
        <div>
          <PaymentStatusTag status={record.paymentStatus} />
          <div className="table-secondary">
            {PAYMENT_METHOD_LABELS[record.paymentMethod]}
          </div>
        </div>
      ),
    },
    {
      title: 'Trạng thái',
      dataIndex: 'orderStatus',
      width: 155,
      render: (value) => <OrderStatusTag status={value} />,
    },
    {
      title: 'Người giao',
      dataIndex: 'deliveryUserName',
      width: 170,
      render: (value) => value || 'Chưa phân công',
    },
    {
      title: 'Thao tác',
      key: 'actions',
      width: 150,
      fixed: 'right',
      render: (_, record) => renderActions(record),
    },
  ]

  return (
    <>
      <PageHeader
        title="Đơn hàng"
        subtitle={
          user?.role === 'Warehouse'
            ? 'Xử lý các đơn đã xác nhận và chuẩn bị hàng.'
            : 'Theo dõi toàn bộ tiến trình từ tiếp nhận đến giao hàng.'
        }
        actions={
          user?.role === 'Sales' && (
            <Button
              type="primary"
              icon={<PlusOutlined />}
              onClick={() => navigate('/orders/create')}
            >
              Tạo đơn hàng
            </Button>
          )
        }
      />
      <Card className="content-card">
        <div className="table-toolbar">
          <Space wrap>
            <Input
              allowClear
              value={searchDraft}
              prefix={<SearchOutlined />}
              placeholder="Mã đơn hoặc khách hàng"
              onChange={(event) => setSearchDraft(event.target.value)}
              onPressEnter={() => {
                setPage(1)
                setSearch(searchDraft.trim())
              }}
              style={{ width: 245 }}
            />
            <Select
              allowClear
              value={status}
              placeholder="Tất cả trạng thái"
              options={Object.entries(ORDER_STATUS_LABELS).map(
                ([value, label]) => ({ value, label }),
              )}
              onChange={(value) => {
                setPage(1)
                setStatus(value)
              }}
              style={{ width: 190 }}
            />
            <DatePicker.RangePicker
              value={dateRange}
              format="DD/MM/YYYY"
              placeholder={['Từ ngày', 'Đến ngày']}
              onChange={(value) => {
                setPage(1)
                setDateRange(value)
              }}
            />
            <Button
              icon={<SearchOutlined />}
              onClick={() => {
                setPage(1)
                setSearch(searchDraft.trim())
              }}
            >
              Tìm kiếm
            </Button>
          </Space>
          <Button icon={<ReloadOutlined />} onClick={loadOrders}>
            Làm mới
          </Button>
        </div>
        <Table
          rowKey="id"
          columns={columns}
          dataSource={orders}
          loading={loading}
          scroll={{ x: 1550 }}
          pagination={{
            current: page,
            pageSize,
            total,
            showSizeChanger: true,
            pageSizeOptions: PAGE_SIZE_OPTIONS,
            showTotal: (count) => `${count} đơn hàng`,
            onChange: (nextPage, nextSize) => {
              setPage(nextSize !== pageSize ? 1 : nextPage)
              setPageSize(nextSize)
            },
          }}
        />
      </Card>

      <Modal
        title={`${assignOrder?.deliveryUserId ? 'Đổi' : 'Phân công'} nhân viên giao hàng`}
        open={Boolean(assignOrder)}
        onCancel={() => setAssignOrder(null)}
        onOk={assign}
        okText="Xác nhận phân công"
        cancelText="Hủy"
        confirmLoading={actionLoading === assignOrder?.id}
        destroyOnHidden
      >
        <Typography.Paragraph type="secondary">
          Đơn hàng <strong>{assignOrder?.orderCode}</strong> ·{' '}
          {assignOrder?.customerName}
        </Typography.Paragraph>
        <Form form={assignForm} layout="vertical">
          <Form.Item
            name="deliveryUserId"
            label="Nhân viên giao hàng"
            rules={[{ required: true, message: 'Vui lòng chọn người giao.' }]}
          >
            <Select
              showSearch
              optionFilterProp="label"
              placeholder="Chọn nhân viên"
              options={deliveryUsers.map((item) => ({
                value: item.id,
                label: `${item.fullName} (${item.username})`,
              }))}
            />
          </Form.Item>
        </Form>
      </Modal>
    </>
  )
}
