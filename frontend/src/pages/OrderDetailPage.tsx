import { useCallback, useEffect, useState } from 'react'
import {
  Alert,
  Button,
  Card,
  Col,
  Descriptions,
  Divider,
  Form,
  Input,
  InputNumber,
  Modal,
  Result,
  Row,
  Select,
  Space,
  Spin,
  Steps,
  Table,
  Timeline,
  Typography,
  message,
  type TableColumnsType,
} from 'antd'
import {
  ArrowLeftOutlined,
  CarOutlined,
  CheckCircleOutlined,
  CloseCircleOutlined,
  DollarOutlined,
  DownloadOutlined,
  EditOutlined,
  InboxOutlined,
} from '@ant-design/icons'
import { useNavigate, useParams } from 'react-router-dom'
import PageHeader from '../components/PageHeader'
import { OrderStatusTag, PaymentStatusTag } from '../components/StatusTag'
import { useAuth } from '../contexts/AuthContext'
import { getApiErrorMessage, orderApi, userApi } from '../services/api'
import type { Order, OrderItem, OrderStatus, PaymentMethod, User } from '../types'
import {
  ORDER_STATUS_LABELS,
  PAYMENT_METHOD_LABELS,
} from '../utils/constants'
import { formatCurrency, formatDate, formatDateTime, formatNumber } from '../utils/format'

const progressStatuses: OrderStatus[] = [
  'New',
  'Confirmed',
  'Preparing',
  'ReadyForDelivery',
  'Delivering',
  'Delivered',
]

export default function OrderDetailPage() {
  const { id } = useParams()
  const orderId = Number(id)
  const navigate = useNavigate()
  const { user } = useAuth()
  const [order, setOrder] = useState<Order | null>(null)
  const [loading, setLoading] = useState(true)
  const [actionLoading, setActionLoading] = useState(false)
  const [assignOpen, setAssignOpen] = useState(false)
  const [paymentOpen, setPaymentOpen] = useState(false)
  const [deliveryUsers, setDeliveryUsers] = useState<User[]>([])
  const [assignForm] = Form.useForm<{ deliveryUserId: number }>()
  const [paymentForm] = Form.useForm<{
    amount: number
    paymentMethod: PaymentMethod
    note?: string
  }>()

  const loadOrder = useCallback(async () => {
    if (!Number.isFinite(orderId)) return
    setLoading(true)
    try {
      setOrder(await orderApi.get(orderId))
    } catch (error) {
      message.error(getApiErrorMessage(error, 'Không thể tải chi tiết đơn hàng.'))
      setOrder(null)
    } finally {
      setLoading(false)
    }
  }, [orderId])

  useEffect(() => {
    loadOrder()
  }, [loadOrder])

  const runAction = (
    action: 'confirm' | 'prepare' | 'ready' | 'cancel',
    title: string,
    content: string,
  ) => {
    if (!order) return
    Modal.confirm({
      title,
      content,
      okText: 'Xác nhận',
      cancelText: 'Bỏ qua',
      okButtonProps: action === 'cancel' ? { danger: true } : undefined,
      async onOk() {
        setActionLoading(true)
        try {
          await orderApi[action](order.id)
          message.success('Cập nhật trạng thái đơn hàng thành công.')
          await loadOrder()
        } catch (error) {
          message.error(getApiErrorMessage(error, 'Không thể cập nhật đơn hàng.'))
          throw error
        } finally {
          setActionLoading(false)
        }
      },
    })
  }

  const openAssign = async () => {
    if (!order) return
    setAssignOpen(true)
    assignForm.setFieldValue('deliveryUserId', order.deliveryUserId ?? undefined)
    try {
      setDeliveryUsers(await userApi.deliveryUsers())
    } catch (error) {
      message.error(
        getApiErrorMessage(error, 'Không thể tải nhân viên giao hàng.'),
      )
    }
  }

  const assign = async () => {
    if (!order) return
    try {
      const values = await assignForm.validateFields()
      setActionLoading(true)
      await orderApi.assignDelivery(order.id, values.deliveryUserId)
      message.success('Phân công giao hàng thành công.')
      setAssignOpen(false)
      await loadOrder()
    } catch (error) {
      if ((error as { errorFields?: unknown }).errorFields) return
      message.error(getApiErrorMessage(error, 'Không thể phân công giao hàng.'))
    } finally {
      setActionLoading(false)
    }
  }

  const downloadInvoice = async () => {
    if (!order) return
    setActionLoading(true)
    try {
      const blob = await orderApi.invoice(order.id)
      const url = URL.createObjectURL(blob)
      const link = document.createElement('a')
      link.href = url
      link.download = `hoa-don-${order.orderCode}.pdf`
      document.body.appendChild(link)
      link.click()
      link.remove()
      URL.revokeObjectURL(url)
    } catch (error) {
      message.error(getApiErrorMessage(error, 'Không thể tải hóa đơn PDF.'))
    } finally {
      setActionLoading(false)
    }
  }

  const openPayment = () => {
    if (!order) return
    paymentForm.setFieldsValue({
      amount: order.remainingAmount,
      paymentMethod: order.paymentMethod,
      note: undefined,
    })
    setPaymentOpen(true)
  }

  const recordPayment = async () => {
    if (!order) return
    try {
      const values = await paymentForm.validateFields()
      setActionLoading(true)
      await orderApi.recordPayment(
        order.id,
        values.amount,
        values.paymentMethod,
        values.note,
      )
      message.success('Đã ghi nhận khoản thanh toán.')
      setPaymentOpen(false)
      await loadOrder()
    } catch (error) {
      if ((error as { errorFields?: unknown }).errorFields) return
      message.error(getApiErrorMessage(error, 'Không thể ghi nhận thanh toán.'))
    } finally {
      setActionLoading(false)
    }
  }

  if (loading) {
    return (
      <div className="page-loader">
        <Spin size="large" tip="Đang tải đơn hàng..." />
      </div>
    )
  }

  if (!order) {
    return (
      <Result
        status="404"
        title="Không tìm thấy đơn hàng"
        subTitle="Đơn hàng không tồn tại hoặc bạn không có quyền xem."
        extra={
          <Button type="primary" onClick={() => navigate('/orders')}>
            Về danh sách đơn
          </Button>
        }
      />
    )
  }

  const isFinal =
    order.orderStatus === 'Cancelled' ||
    order.orderStatus === 'DeliveryFailed'
  const currentStep = Math.max(0, progressStatuses.indexOf(order.orderStatus))
  const canAssign =
    (user?.role === 'Admin' || user?.role === 'Sales') &&
    order.orderStatus === 'ReadyForDelivery'

  const itemColumns: TableColumnsType<OrderItem> = [
    {
      title: 'Mã sản phẩm',
      dataIndex: 'productCode',
      width: 140,
    },
    {
      title: 'Sản phẩm',
      dataIndex: 'productName',
      render: (value, record) => (
        <div>
          <Typography.Text strong>{value}</Typography.Text>
          {record.requiresContainer && (
            <div className="table-secondary">
              Đã trả {formatNumber(record.returnedContainerQuantity)}/{formatNumber(record.quantity)} vỏ
              {record.containerDepositAmount > 0 &&
                ` · Cọc ${formatCurrency(record.containerDepositAmount)}`}
            </div>
          )}
        </div>
      ),
    },
    {
      title: 'Số lượng',
      dataIndex: 'quantity',
      width: 120,
      align: 'right',
      render: (value) => formatNumber(value),
    },
    {
      title: 'Đơn giá',
      dataIndex: 'unitPrice',
      width: 160,
      align: 'right',
      render: formatCurrency,
    },
    {
      title: 'Thành tiền',
      dataIndex: 'totalPrice',
      width: 170,
      align: 'right',
      render: (value) => (
        <Typography.Text strong>{formatCurrency(value)}</Typography.Text>
      ),
    },
  ]

  return (
    <>
      <PageHeader
        title={`Đơn hàng ${order.orderCode}`}
        subtitle={`Tạo bởi ${order.createdByName} lúc ${formatDateTime(order.orderDate)}`}
        actions={
          <Space wrap>
            <Button icon={<ArrowLeftOutlined />} onClick={() => navigate('/orders')}>
              Danh sách
            </Button>
            {(user?.role === 'Admin' || user?.role === 'Sales') && (
              <Button
                icon={<DownloadOutlined />}
                loading={actionLoading}
                onClick={downloadInvoice}
              >
                Hóa đơn PDF
              </Button>
            )}
            {(user?.role === 'Admin' || user?.role === 'Sales') &&
              order.orderStatus === 'Delivered' &&
              order.remainingAmount > 0 && (
                <Button icon={<DollarOutlined />} onClick={openPayment}>
                  Thu tiền
                </Button>
              )}
            {user?.role === 'Sales' && order.orderStatus === 'New' && (
              <Button
                icon={<EditOutlined />}
                onClick={() => navigate(`/orders/${order.id}/edit`)}
              >
                Chỉnh sửa
              </Button>
            )}
            {user?.role === 'Sales' && order.orderStatus === 'New' && (
              <Button
                type="primary"
                icon={<CheckCircleOutlined />}
                loading={actionLoading}
                onClick={() =>
                  runAction(
                    'confirm',
                    'Xác nhận đơn hàng?',
                    'Hệ thống sẽ kiểm tra tồn kho hiện tại.',
                  )
                }
              >
                Xác nhận đơn
              </Button>
            )}
            {user?.role === 'Sales' && order.orderStatus === 'New' && (
              <Button
                danger
                icon={<CloseCircleOutlined />}
                onClick={() =>
                  runAction(
                    'cancel',
                    'Hủy đơn hàng?',
                    'Đơn đã hủy không thể tiếp tục xử lý.',
                  )
                }
              >
                Hủy đơn
              </Button>
            )}
            {user?.role === 'Warehouse' &&
              order.orderStatus === 'Confirmed' && (
                <Button
                  type="primary"
                  icon={<InboxOutlined />}
                  loading={actionLoading}
                  onClick={() =>
                    runAction(
                      'prepare',
                      'Bắt đầu chuẩn bị hàng?',
                      'Tồn kho sẽ được trừ trong một giao dịch an toàn.',
                    )
                  }
                >
                  Chuẩn bị hàng
                </Button>
              )}
            {user?.role === 'Warehouse' &&
              order.orderStatus === 'Preparing' && (
                <Button
                  type="primary"
                  icon={<CheckCircleOutlined />}
                  loading={actionLoading}
                  onClick={() =>
                    runAction(
                      'ready',
                      'Đã chuẩn bị xong?',
                      'Đơn sẽ sẵn sàng để phân công giao.',
                    )
                  }
                >
                  Sẵn sàng giao
                </Button>
              )}
            {canAssign && (
              <Button
                type="primary"
                icon={<CarOutlined />}
                onClick={openAssign}
              >
                {order.deliveryUserId ? 'Đổi người giao' : 'Phân công giao'}
              </Button>
            )}
          </Space>
        }
      />

      {isFinal ? (
        <Alert
          type={order.orderStatus === 'Cancelled' ? 'warning' : 'error'}
          showIcon
          message={`Đơn hàng ${ORDER_STATUS_LABELS[order.orderStatus].toLowerCase()}`}
          description={
            order.delivery?.failureReason
              ? `Lý do: ${order.delivery.failureReason}`
              : undefined
          }
          className="page-alert"
        />
      ) : (
        <Card className="content-card status-progress">
          <Steps
            current={currentStep}
            responsive
            items={progressStatuses.map((status) => ({
              title: ORDER_STATUS_LABELS[status],
            }))}
          />
        </Card>
      )}

      <Row gutter={20}>
        <Col xs={24} xl={16}>
          <Card title="Thông tin đơn hàng" className="content-card">
            <Descriptions column={{ xs: 1, sm: 2 }} bordered size="small">
              <Descriptions.Item label="Khách hàng">
                <Typography.Text strong>{order.customerName}</Typography.Text>
              </Descriptions.Item>
              <Descriptions.Item label="Trạng thái">
                <OrderStatusTag status={order.orderStatus} />
              </Descriptions.Item>
              <Descriptions.Item label="Ngày đặt">
                {formatDateTime(order.orderDate)}
              </Descriptions.Item>
              <Descriptions.Item label="Ngày giao dự kiến">
                {formatDate(order.expectedDeliveryDate)}
              </Descriptions.Item>
              <Descriptions.Item label="Địa chỉ giao" span={2}>
                {order.deliveryAddress}
              </Descriptions.Item>
              <Descriptions.Item label="Phương thức">
                {PAYMENT_METHOD_LABELS[order.paymentMethod]}
              </Descriptions.Item>
              <Descriptions.Item label="Thanh toán">
                <PaymentStatusTag status={order.paymentStatus} />
              </Descriptions.Item>
              <Descriptions.Item label="Tiền hàng">
                {formatCurrency(order.productAmount)}
              </Descriptions.Item>
              <Descriptions.Item label="Tiền cọc vỏ">
                {formatCurrency(order.depositAmount)}
              </Descriptions.Item>
              <Descriptions.Item label="Đã thanh toán">
                <Typography.Text type="success">
                  {formatCurrency(order.paidAmount)}
                </Typography.Text>
              </Descriptions.Item>
              <Descriptions.Item label="Còn phải thu">
                <Typography.Text strong type={order.remainingAmount > 0 ? 'danger' : undefined}>
                  {formatCurrency(order.remainingAmount)}
                </Typography.Text>
              </Descriptions.Item>
              <Descriptions.Item label="Ghi chú" span={2}>
                {order.note || 'Không có'}
              </Descriptions.Item>
            </Descriptions>
          </Card>

          <Card title="Chi tiết sản phẩm" className="content-card">
            <Table
              rowKey={(record) => record.id ?? record.productId}
              columns={itemColumns}
              dataSource={order.details}
              pagination={false}
              scroll={{ x: 750 }}
              summary={() => (
                <Table.Summary.Row>
                  <Table.Summary.Cell index={0} colSpan={4} align="right">
                    <Typography.Text strong>Tổng cộng</Typography.Text>
                  </Table.Summary.Cell>
                  <Table.Summary.Cell index={4} align="right">
                    <Typography.Title level={5} className="summary-money">
                      {formatCurrency(order.totalAmount)}
                    </Typography.Title>
                  </Table.Summary.Cell>
                </Table.Summary.Row>
              )}
            />
          </Card>
          {order.payments.length > 0 && (
            <Card title="Lịch sử thanh toán" className="content-card">
              <Table
                rowKey="id"
                dataSource={order.payments}
                pagination={false}
                scroll={{ x: 650 }}
                columns={[
                  { title: 'Thời gian', dataIndex: 'paidAt', render: formatDateTime },
                  {
                    title: 'Số tiền',
                    dataIndex: 'amount',
                    align: 'right' as const,
                    render: formatCurrency,
                  },
                  {
                    title: 'Phương thức',
                    dataIndex: 'paymentMethod',
                    render: (value: PaymentMethod) => PAYMENT_METHOD_LABELS[value],
                  },
                  { title: 'Người ghi nhận', dataIndex: 'recordedByName' },
                  { title: 'Ghi chú', dataIndex: 'note', render: (value?: string) => value || '—' },
                ]}
              />
            </Card>
          )}
        </Col>
        <Col xs={24} xl={8}>
          <Card title="Thông tin giao hàng" className="content-card">
            {order.delivery ? (
              <>
                <Descriptions column={1} size="small">
                  <Descriptions.Item label="Nhân viên">
                    <Typography.Text strong>
                      {order.delivery.deliveryUserName}
                    </Typography.Text>
                  </Descriptions.Item>
                  <Descriptions.Item label="Phân công lúc">
                    {formatDateTime(order.delivery.assignedAt)}
                  </Descriptions.Item>
                </Descriptions>
                <Divider />
                <Timeline
                  items={[
                    {
                      color: 'blue',
                      children: `Đã phân công · ${formatDateTime(
                        order.delivery.assignedAt,
                      )}`,
                    },
                    {
                      color: order.delivery.startedAt ? 'blue' : 'gray',
                      children: order.delivery.startedAt
                        ? `Bắt đầu giao · ${formatDateTime(
                            order.delivery.startedAt,
                          )}`
                        : 'Chưa bắt đầu giao',
                    },
                    {
                      color:
                        order.delivery.status === 'Completed'
                          ? 'green'
                          : order.delivery.status === 'Failed'
                            ? 'red'
                            : 'gray',
                      children: order.delivery.completedAt
                        ? `Hoàn tất · ${formatDateTime(
                            order.delivery.completedAt,
                          )}`
                        : 'Chưa hoàn tất',
                    },
                  ]}
                />
                {order.delivery.failureReason && (
                  <Alert
                    type="error"
                    showIcon
                    message="Lý do giao thất bại"
                    description={order.delivery.failureReason}
                  />
                )}
              </>
            ) : (
              <Alert
                type="info"
                showIcon
                message="Chưa phân công người giao"
                description="Có thể phân công sau khi kho đánh dấu đơn sẵn sàng giao."
              />
            )}
          </Card>
        </Col>
      </Row>

      <Modal
        title={`${order.deliveryUserId ? 'Đổi' : 'Phân công'} nhân viên giao hàng`}
        open={assignOpen}
        onCancel={() => setAssignOpen(false)}
        onOk={assign}
        okText="Xác nhận phân công"
        cancelText="Hủy"
        confirmLoading={actionLoading}
        destroyOnHidden
      >
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

      <Modal
        title={`Thu tiền đơn ${order.orderCode}`}
        open={paymentOpen}
        onCancel={() => setPaymentOpen(false)}
        onOk={recordPayment}
        okText="Ghi nhận thanh toán"
        cancelText="Hủy"
        confirmLoading={actionLoading}
        destroyOnHidden
      >
        <Alert
          type="info"
          showIcon
          message={`Còn phải thu ${formatCurrency(order.remainingAmount)}`}
          className="modal-alert"
        />
        <Form form={paymentForm} layout="vertical">
          <Form.Item
            name="amount"
            label="Số tiền thu"
            rules={[
              { required: true, message: 'Vui lòng nhập số tiền.' },
              {
                type: 'number',
                min: 1,
                max: order.remainingAmount,
                message: `Số tiền phải từ 1 đến ${formatCurrency(order.remainingAmount)}.`,
              },
            ]}
          >
            <InputNumber
              min={1}
              max={order.remainingAmount}
              precision={0}
              addonAfter="đ"
              style={{ width: '100%' }}
            />
          </Form.Item>
          <Form.Item
            name="paymentMethod"
            label="Phương thức"
            rules={[{ required: true, message: 'Vui lòng chọn phương thức.' }]}
          >
            <Select
              options={Object.entries(PAYMENT_METHOD_LABELS).map(([value, label]) => ({
                value,
                label,
              }))}
            />
          </Form.Item>
          <Form.Item name="note" label="Ghi chú">
            <Input.TextArea rows={2} maxLength={500} showCount />
          </Form.Item>
        </Form>
      </Modal>
    </>
  )
}
