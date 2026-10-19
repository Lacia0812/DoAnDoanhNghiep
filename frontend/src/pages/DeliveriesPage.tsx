import { useCallback, useEffect, useState } from 'react'
import {
  Alert,
  Button,
  Card,
  Checkbox,
  Descriptions,
  Drawer,
  Form,
  Input,
  InputNumber,
  List,
  Modal,
  Pagination,
  Select,
  Space,
  Spin,
  Table,
  Typography,
  message,
} from 'antd'
import {
  CarOutlined,
  CheckCircleOutlined,
  EnvironmentOutlined,
  EyeOutlined,
  PhoneOutlined,
  ReloadOutlined,
  StopOutlined,
} from '@ant-design/icons'
import PageHeader from '../components/PageHeader'
import { DeliveryStatusTag, PaymentStatusTag } from '../components/StatusTag'
import { deliveryApi, getApiErrorMessage } from '../services/api'
import type { Delivery, DeliveryStatus, OrderItem } from '../types'
import {
  DELIVERY_STATUS_LABELS,
  PAYMENT_METHOD_LABELS,
} from '../utils/constants'
import { formatCurrency, formatDateTime } from '../utils/format'

export default function DeliveriesPage() {
  const [deliveries, setDeliveries] = useState<Delivery[]>([])
  const [loading, setLoading] = useState(false)
  const [page, setPage] = useState(1)
  const [pageSize, setPageSize] = useState(9)
  const [total, setTotal] = useState(0)
  const [status, setStatus] = useState<DeliveryStatus>()
  const [search, setSearch] = useState('')
  const [selected, setSelected] = useState<Delivery | null>(null)
  const [detailLoading, setDetailLoading] = useState(false)
  const [actionDelivery, setActionDelivery] = useState<Delivery | null>(null)
  const [actionType, setActionType] = useState<'complete' | 'fail' | null>(null)
  const [actionLoading, setActionLoading] = useState(false)
  const [completeForm] = Form.useForm<{
    markAsPaid: boolean
    note?: string
    containerReturns?: Record<string, number>
  }>()
  const [failForm] = Form.useForm<{ failureReason: string; note?: string }>()

  const loadDeliveries = useCallback(async () => {
    setLoading(true)
    try {
      const result = await deliveryApi.mine({
        page,
        pageSize,
        search: search || undefined,
        status,
      })
      setDeliveries(result.items)
      setTotal(result.totalCount)
    } catch (error) {
      message.error(getApiErrorMessage(error, 'Không thể tải đơn được giao.'))
    } finally {
      setLoading(false)
    }
  }, [page, pageSize, search, status])

  useEffect(() => {
    loadDeliveries()
  }, [loadDeliveries])

  const showDetail = async (delivery: Delivery) => {
    setSelected(delivery)
    setDetailLoading(true)
    try {
      setSelected(await deliveryApi.get(delivery.id))
    } catch (error) {
      message.error(getApiErrorMessage(error, 'Không thể tải chi tiết giao hàng.'))
    } finally {
      setDetailLoading(false)
    }
  }

  const start = (delivery: Delivery) => {
    Modal.confirm({
      title: 'Bắt đầu giao đơn hàng?',
      content: `Đơn ${delivery.orderCode} sẽ chuyển sang trạng thái đang giao.`,
      okText: 'Bắt đầu giao',
      cancelText: 'Bỏ qua',
      async onOk() {
        setActionLoading(true)
        try {
          await deliveryApi.start(delivery.id)
          message.success('Đã bắt đầu giao hàng.')
          await loadDeliveries()
          if (selected?.id === delivery.id) {
            setSelected(await deliveryApi.get(delivery.id))
          }
        } catch (error) {
          message.error(getApiErrorMessage(error, 'Không thể bắt đầu giao hàng.'))
          throw error
        } finally {
          setActionLoading(false)
        }
      },
    })
  }

  const openComplete = async (delivery: Delivery) => {
    setActionLoading(true)
    try {
      const detail = delivery.order ? delivery : await deliveryApi.get(delivery.id)
      setActionDelivery(detail)
      setActionType('complete')
      completeForm.resetFields()
      completeForm.setFieldsValue({
        markAsPaid: false,
        containerReturns: Object.fromEntries(
          (detail.order?.details ?? [])
            .filter((item) => item.requiresContainer)
            .map((item) => [String(item.id), 0]),
        ),
      })
    } catch (error) {
      message.error(getApiErrorMessage(error, 'Không thể tải chi tiết giao hàng.'))
    } finally {
      setActionLoading(false)
    }
  }

  const openFail = (delivery: Delivery) => {
    setActionDelivery(delivery)
    setActionType('fail')
    failForm.resetFields()
  }

  const submitComplete = async () => {
    if (!actionDelivery) return
    try {
      const values = await completeForm.validateFields()
      setActionLoading(true)
      await deliveryApi.complete(
        actionDelivery.id,
        values.markAsPaid,
        Object.entries(values.containerReturns ?? {}).map(
          ([orderDetailId, quantity]) => ({
            orderDetailId: Number(orderDetailId),
            quantity: Number(quantity ?? 0),
          }),
        ),
        values.note,
      )
      message.success('Xác nhận giao hàng thành công.')
      setActionDelivery(null)
      setActionType(null)
      await loadDeliveries()
      if (selected?.id === actionDelivery.id) {
        setSelected(await deliveryApi.get(actionDelivery.id))
      }
    } catch (error) {
      if ((error as { errorFields?: unknown }).errorFields) return
      message.error(getApiErrorMessage(error, 'Không thể hoàn tất giao hàng.'))
    } finally {
      setActionLoading(false)
    }
  }

  const submitFail = async () => {
    if (!actionDelivery) return
    try {
      const values = await failForm.validateFields()
      setActionLoading(true)
      await deliveryApi.fail(
        actionDelivery.id,
        values.failureReason,
        values.note,
      )
      message.success('Đã ghi nhận giao hàng thất bại và hoàn lại tồn kho.')
      setActionDelivery(null)
      setActionType(null)
      await loadDeliveries()
      if (selected?.id === actionDelivery.id) {
        setSelected(await deliveryApi.get(actionDelivery.id))
      }
    } catch (error) {
      if ((error as { errorFields?: unknown }).errorFields) return
      message.error(
        getApiErrorMessage(error, 'Không thể ghi nhận giao hàng thất bại.'),
      )
    } finally {
      setActionLoading(false)
    }
  }

  const actionButtons = (delivery: Delivery) => (
    <Space wrap>
      <Button icon={<EyeOutlined />} onClick={() => showDetail(delivery)}>
        Chi tiết
      </Button>
      {delivery.status === 'Assigned' && (
        <Button
          type="primary"
          icon={<CarOutlined />}
          loading={actionLoading}
          onClick={() => start(delivery)}
        >
          Bắt đầu giao
        </Button>
      )}
      {delivery.status === 'Delivering' && (
        <>
          <Button
            type="primary"
            icon={<CheckCircleOutlined />}
            onClick={() => openComplete(delivery)}
          >
            Giao thành công
          </Button>
          <Button
            danger
            icon={<StopOutlined />}
            onClick={() => openFail(delivery)}
          >
            Giao thất bại
          </Button>
        </>
      )}
    </Space>
  )

  const deliveryOrder = selected?.order

  return (
    <>
      <PageHeader
        title="Đơn giao của tôi"
        subtitle="Cập nhật tiến độ giao hàng nhanh chóng và chính xác."
        actions={
          <Button
            icon={<ReloadOutlined />}
            onClick={loadDeliveries}
            loading={loading}
          >
            Làm mới
          </Button>
        }
      />
      <Card className="content-card delivery-toolbar-card">
        <Space wrap>
          <Input.Search
            allowClear
            enterButton="Tìm kiếm"
            placeholder="Mã đơn hoặc khách hàng"
            onSearch={(value) => {
              setPage(1)
              setSearch(value.trim())
            }}
            style={{ width: 280 }}
          />
          <Select
            allowClear
            placeholder="Tất cả trạng thái"
            value={status}
            onChange={(value) => {
              setPage(1)
              setStatus(value)
            }}
            options={Object.entries(DELIVERY_STATUS_LABELS).map(
              ([value, label]) => ({ value, label }),
            )}
            style={{ width: 190 }}
          />
        </Space>
      </Card>

      <Spin spinning={loading}>
        <List
          grid={{ gutter: 16, xs: 1, sm: 1, md: 2, xl: 3 }}
          dataSource={deliveries}
          locale={{ emptyText: 'Chưa có đơn giao phù hợp.' }}
          renderItem={(delivery) => {
            const order = delivery.order
            return (
              <List.Item>
                <Card
                  className={`delivery-card delivery-${delivery.status.toLowerCase()}`}
                  title={
                    <Space>
                      <CarOutlined />
                      <Typography.Text strong>
                        {delivery.orderCode}
                      </Typography.Text>
                    </Space>
                  }
                  extra={<DeliveryStatusTag status={delivery.status} />}
                >
                  <div className="delivery-customer">
                    <Typography.Title level={5}>
                      {order?.customerName ?? 'Khách hàng'}
                    </Typography.Title>
                    {order?.deliveryAddress && (
                      <p>
                        <EnvironmentOutlined /> {order.deliveryAddress}
                      </p>
                    )}
                    {order?.customerPhone && (
                      <p>
                        <PhoneOutlined /> {order.customerPhone}
                      </p>
                    )}
                  </div>
                  <Descriptions column={1} size="small">
                    <Descriptions.Item label="Tổng tiền">
                      <Typography.Text strong>
                        {formatCurrency(order?.totalAmount)}
                      </Typography.Text>
                    </Descriptions.Item>
                    <Descriptions.Item label="Phân công">
                      {formatDateTime(delivery.assignedAt)}
                    </Descriptions.Item>
                    {delivery.startedAt && (
                      <Descriptions.Item label="Bắt đầu">
                        {formatDateTime(delivery.startedAt)}
                      </Descriptions.Item>
                    )}
                  </Descriptions>
                  <div className="delivery-actions">{actionButtons(delivery)}</div>
                </Card>
              </List.Item>
            )
          }}
        />
      </Spin>
      {total > 0 && (
        <div className="delivery-pagination">
          <Pagination
            current={page}
            pageSize={pageSize}
            total={total}
            showSizeChanger
            pageSizeOptions={['6', '9', '12', '24']}
            showTotal={(count) => `${count} đơn giao`}
            onChange={(nextPage, nextSize) => {
              setPage(nextSize !== pageSize ? 1 : nextPage)
              setPageSize(nextSize)
            }}
          />
        </div>
      )}

      <Drawer
        title={`Chi tiết giao hàng · ${selected?.orderCode ?? ''}`}
        width={760}
        open={Boolean(selected)}
        onClose={() => setSelected(null)}
        extra={selected && actionButtons(selected)}
      >
        <Spin spinning={detailLoading}>
          {selected && (
            <>
              <Descriptions bordered column={{ xs: 1, sm: 2 }} size="small">
                <Descriptions.Item label="Trạng thái">
                  <DeliveryStatusTag status={selected.status} />
                </Descriptions.Item>
                <Descriptions.Item label="Nhân viên">
                  {selected.deliveryUserName}
                </Descriptions.Item>
                <Descriptions.Item label="Khách hàng">
                  {deliveryOrder?.customerName ?? '—'}
                </Descriptions.Item>
                <Descriptions.Item label="Thanh toán">
                  {deliveryOrder ? (
                    <PaymentStatusTag status={deliveryOrder.paymentStatus} />
                  ) : (
                    '—'
                  )}
                </Descriptions.Item>
                <Descriptions.Item label="Địa chỉ" span={2}>
                  {deliveryOrder?.deliveryAddress ?? '—'}
                </Descriptions.Item>
                <Descriptions.Item label="Phương thức">
                  {deliveryOrder
                    ? PAYMENT_METHOD_LABELS[deliveryOrder.paymentMethod]
                    : '—'}
                </Descriptions.Item>
                <Descriptions.Item label="Tổng tiền">
                  <Typography.Text strong>
                    {formatCurrency(
                      deliveryOrder?.totalAmount,
                    )}
                  </Typography.Text>
                </Descriptions.Item>
              </Descriptions>
              {selected.failureReason && (
                <Alert
                  type="error"
                  showIcon
                  message="Lý do giao thất bại"
                  description={selected.failureReason}
                  className="modal-alert"
                />
              )}
              <Typography.Title level={5} className="drawer-section-title">
                Sản phẩm
              </Typography.Title>
              <Table<OrderItem>
                rowKey={(record) => record.id ?? record.productId}
                dataSource={deliveryOrder?.details ?? []}
                pagination={false}
                scroll={{ x: 600 }}
                columns={[
                  { title: 'Sản phẩm', dataIndex: 'productName' },
                  {
                    title: 'Số lượng',
                    dataIndex: 'quantity',
                    align: 'right',
                    width: 100,
                  },
                  {
                    title: 'Đơn giá',
                    dataIndex: 'unitPrice',
                    align: 'right',
                    width: 140,
                    render: formatCurrency,
                  },
                  {
                    title: 'Thành tiền',
                    dataIndex: 'totalPrice',
                    align: 'right',
                    width: 150,
                    render: formatCurrency,
                  },
                ]}
              />
            </>
          )}
        </Spin>
      </Drawer>

      <Modal
        title={`Xác nhận giao thành công · ${actionDelivery?.orderCode ?? ''}`}
        open={actionType === 'complete'}
        onCancel={() => {
          setActionType(null)
          setActionDelivery(null)
        }}
        onOk={submitComplete}
        okText="Hoàn tất giao hàng"
        cancelText="Hủy"
        confirmLoading={actionLoading}
        destroyOnHidden
      >
        <Alert
          type="success"
          showIcon
          message="Đơn hàng sẽ được ghi nhận vào doanh thu."
          className="modal-alert"
        />
        <Form
          form={completeForm}
          layout="vertical"
          initialValues={{ markAsPaid: false }}
        >
          {(actionDelivery?.order?.details ?? [])
            .filter((item) => item.requiresContainer)
            .map((item) => (
              <Form.Item
                key={item.id}
                name={['containerReturns', String(item.id)]}
                label={`Vỏ khách trả · ${item.productName}`}
                rules={[
                  { required: true, message: 'Vui lòng nhập số vỏ khách trả.' },
                  {
                    type: 'number',
                    min: 0,
                    max: item.quantity,
                    message: `Số vỏ phải từ 0 đến ${item.quantity}.`,
                  },
                ]}
              >
                <InputNumber
                  min={0}
                  max={item.quantity}
                  precision={0}
                  addonAfter={`/ ${item.quantity} vỏ`}
                  style={{ width: '100%' }}
                />
              </Form.Item>
            ))}
          {(actionDelivery?.order?.details ?? []).some(
            (item) => item.requiresContainer,
          ) && (
            <Alert
              type="info"
              showIcon
              message="Tiền cọc được tính cho số bình không có vỏ đổi trả."
              className="modal-alert"
            />
          )}
          <Form.Item name="markAsPaid" valuePropName="checked">
            <Checkbox>Xác nhận khách hàng đã thanh toán</Checkbox>
          </Form.Item>
          <Form.Item name="note" label="Ghi chú">
            <Input.TextArea rows={3} maxLength={500} showCount />
          </Form.Item>
        </Form>
      </Modal>

      <Modal
        title={`Ghi nhận giao thất bại · ${actionDelivery?.orderCode ?? ''}`}
        open={actionType === 'fail'}
        onCancel={() => {
          setActionType(null)
          setActionDelivery(null)
        }}
        onOk={submitFail}
        okText="Xác nhận thất bại"
        okButtonProps={{ danger: true }}
        cancelText="Hủy"
        confirmLoading={actionLoading}
        destroyOnHidden
      >
        <Alert
          type="warning"
          showIcon
          message="Sản phẩm trong đơn sẽ được tự động hoàn lại kho. Thao tác này không thể hoàn tác."
          className="modal-alert"
        />
        <Form form={failForm} layout="vertical">
          <Form.Item
            name="failureReason"
            label="Lý do giao thất bại"
            rules={[
              { required: true, message: 'Vui lòng nhập lý do thất bại.' },
              { min: 3, message: 'Lý do cần ít nhất 3 ký tự.' },
              { max: 300, message: 'Lý do tối đa 300 ký tự.' },
            ]}
          >
            <Input.TextArea
              rows={3}
              placeholder="VD: Khách hàng không có mặt..."
              showCount
              maxLength={300}
            />
          </Form.Item>
          <Form.Item name="note" label="Ghi chú thêm">
            <Input.TextArea rows={2} maxLength={500} showCount />
          </Form.Item>
        </Form>
      </Modal>
    </>
  )
}
