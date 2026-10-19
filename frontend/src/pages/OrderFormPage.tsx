import { useEffect, useMemo, useState } from 'react'
import {
  Alert,
  Button,
  Card,
  Col,
  DatePicker,
  Divider,
  Form,
  Input,
  InputNumber,
  Modal,
  Row,
  Select,
  Space,
  Spin,
  Typography,
  message,
} from 'antd'
import {
  ArrowLeftOutlined,
  DeleteOutlined,
  PlusOutlined,
  SaveOutlined,
  UserAddOutlined,
} from '@ant-design/icons'
import dayjs, { type Dayjs } from 'dayjs'
import { useNavigate, useParams } from 'react-router-dom'
import PageHeader from '../components/PageHeader'
import {
  customerApi,
  getApiErrorMessage,
  orderApi,
  productApi,
} from '../services/api'
import type {
  Customer,
  OrderLinePayload,
  OrderPayload,
  PaymentMethod,
  Product,
} from '../types'
import { PAYMENT_METHOD_LABELS } from '../utils/constants'
import {
  createWalkInCustomerCode,
  WALK_IN_CUSTOMER_NAME,
} from '../utils/customer'
import { formatCurrency, formatNumber } from '../utils/format'
import { calculateOrderTotal, hasDuplicateProducts } from '../utils/order'

interface FormValues {
  customerId: number
  expectedDeliveryDate?: Dayjs
  deliveryAddress: string
  paymentMethod: PaymentMethod
  note?: string
  items: OrderLinePayload[]
}

interface WalkInCustomerFormValues {
  customerName: string
  phone: string
  address: string
}

export default function OrderFormPage() {
  const { id } = useParams()
  const editingId = id ? Number(id) : undefined
  const navigate = useNavigate()
  const [form] = Form.useForm<FormValues>()
  const [walkInForm] = Form.useForm<WalkInCustomerFormValues>()
  const [customers, setCustomers] = useState<Customer[]>([])
  const [products, setProducts] = useState<Product[]>([])
  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)
  const [walkInModalOpen, setWalkInModalOpen] = useState(false)
  const [creatingWalkInCustomer, setCreatingWalkInCustomer] = useState(false)
  const lines = Form.useWatch('items', form) ?? []

  useEffect(() => {
    let active = true
    setLoading(true)
    Promise.all([
      customerApi.active(),
      productApi.active(),
      editingId ? orderApi.get(editingId) : Promise.resolve(null),
    ])
      .then(([customerResult, productResult, order]) => {
        if (!active) return
        setCustomers(customerResult.items)
        setProducts(productResult.items)
        if (order) {
          if (order.orderStatus !== 'New') {
            message.warning('Chỉ đơn hàng mới được phép chỉnh sửa.')
            navigate(`/orders/${order.id}`, { replace: true })
            return
          }
          form.setFieldsValue({
            customerId: order.customerId,
            expectedDeliveryDate: order.expectedDeliveryDate
              ? dayjs(order.expectedDeliveryDate)
              : undefined,
            deliveryAddress: order.deliveryAddress,
            paymentMethod: order.paymentMethod,
            note: order.note ?? undefined,
            items: order.details.map((item) => ({
              productId: item.productId,
              quantity: item.quantity,
            })),
          })
        } else {
          form.setFieldsValue({
            paymentMethod: 'CashOnDelivery',
            items: [{ productId: undefined as unknown as number, quantity: 1 }],
          })
        }
      })
      .catch((error) => {
        message.error(getApiErrorMessage(error, 'Không thể tải dữ liệu tạo đơn.'))
      })
      .finally(() => {
        if (active) setLoading(false)
      })
    return () => {
      active = false
    }
  }, [editingId, form, navigate])

  const total = useMemo(
    () => calculateOrderTotal(lines, products),
    [lines, products],
  )
  const maximumDeposit = useMemo(
    () =>
      lines.reduce((sum, line) => {
        const product = products.find((item) => item.id === line?.productId)
        if (!product?.requiresContainer) return sum
        return sum + product.containerDeposit * Number(line?.quantity ?? 0)
      }, 0),
    [lines, products],
  )

  const selectedProductIds = lines
    .map((line) => line?.productId)
    .filter((value): value is number => typeof value === 'number')

  const selectCustomer = (customerId: number) => {
    const customer = customers.find((item) => item.id === customerId)
    if (customer) form.setFieldValue('deliveryAddress', customer.address)
  }

  const openWalkInCustomerModal = () => {
    setWalkInModalOpen(true)
  }

  const createWalkInCustomer = async () => {
    try {
      const values = await walkInForm.validateFields()
      setCreatingWalkInCustomer(true)
      const customer = await customerApi.create({
        customerCode: createWalkInCustomerCode(),
        customerName: values.customerName.trim(),
        phone: values.phone.trim(),
        address: values.address.trim(),
        customerType: 'Individual',
        note: 'Khách vãng lai được tạo nhanh từ màn hình bán hàng.',
      })
      setCustomers((current) =>
        [...current, customer].sort((left, right) =>
          left.customerName.localeCompare(right.customerName, 'vi'),
        ),
      )
      form.setFieldsValue({
        customerId: customer.id,
        deliveryAddress: customer.address,
      })
      setWalkInModalOpen(false)
      walkInForm.resetFields()
      message.success('Đã tạo và chọn khách vãng lai cho đơn hàng.')
    } catch (error) {
      if (error && typeof error === 'object' && 'errorFields' in error) return
      message.error(
        getApiErrorMessage(error, 'Không thể tạo nhanh khách vãng lai.'),
      )
    } finally {
      setCreatingWalkInCustomer(false)
    }
  }

  const submit = async (values: FormValues) => {
    if (hasDuplicateProducts(values.items)) {
      message.error('Mỗi sản phẩm chỉ được chọn một lần trong đơn hàng.')
      return
    }
    const payload: OrderPayload = {
      customerId: values.customerId,
      expectedDeliveryDate: values.expectedDeliveryDate?.format('YYYY-MM-DD'),
      deliveryAddress: values.deliveryAddress.trim(),
      paymentMethod: values.paymentMethod,
      note: values.note?.trim(),
      items: values.items.map((item) => ({
        productId: item.productId,
        quantity: item.quantity,
      })),
    }
    setSaving(true)
    try {
      const order = editingId
        ? await orderApi.update(editingId, payload)
        : await orderApi.create(payload)
      message.success(
        editingId ? 'Cập nhật đơn hàng thành công.' : 'Tạo đơn hàng thành công.',
      )
      navigate(`/orders/${order.id}`, { replace: true })
    } catch (error) {
      message.error(getApiErrorMessage(error, 'Không thể lưu đơn hàng.'))
    } finally {
      setSaving(false)
    }
  }

  if (loading) {
    return (
      <div className="page-loader">
        <Spin size="large" tip="Đang tải dữ liệu..." />
      </div>
    )
  }

  return (
    <>
      <PageHeader
        title={editingId ? 'Chỉnh sửa đơn hàng' : 'Tạo đơn hàng'}
        subtitle="Đơn giá và tổng tiền sẽ được máy chủ kiểm tra và tính lại khi lưu."
        actions={
          <Button icon={<ArrowLeftOutlined />} onClick={() => navigate('/orders')}>
            Quay lại
          </Button>
        }
      />
      <Form
        form={form}
        layout="vertical"
        requiredMark="optional"
        onFinish={submit}
        scrollToFirstError
      >
        <Row gutter={20}>
          <Col xs={24} xl={16}>
            <Card title="Thông tin giao hàng" className="content-card form-card">
              <Row gutter={16}>
                <Col xs={24} md={14}>
                  <Form.Item
                    name="customerId"
                    label="Khách hàng"
                    extra={
                      <Button
                        type="link"
                        size="small"
                        htmlType="button"
                        icon={<UserAddOutlined />}
                        onClick={openWalkInCustomerModal}
                        style={{ paddingInline: 0 }}
                      >
                        Thêm khách vãng lai
                      </Button>
                    }
                    rules={[
                      { required: true, message: 'Vui lòng chọn khách hàng.' },
                    ]}
                  >
                    <Select
                      showSearch
                      optionFilterProp="label"
                      placeholder="Chọn khách hàng"
                      onChange={selectCustomer}
                      options={customers.map((item) => ({
                        value: item.id,
                        label: `${item.customerCode} · ${item.customerName} · ${item.phone}`,
                      }))}
                    />
                  </Form.Item>
                </Col>
                <Col xs={24} md={10}>
                  <Form.Item
                    name="expectedDeliveryDate"
                    label="Ngày giao dự kiến"
                  >
                    <DatePicker
                      format="DD/MM/YYYY"
                      disabledDate={(date) =>
                        date.startOf('day').isBefore(dayjs().startOf('day'))
                      }
                      style={{ width: '100%' }}
                    />
                  </Form.Item>
                </Col>
              </Row>
              <Form.Item
                name="deliveryAddress"
                label="Địa chỉ giao hàng"
                rules={[
                  { required: true, message: 'Vui lòng nhập địa chỉ giao hàng.' },
                  { max: 300, message: 'Địa chỉ tối đa 300 ký tự.' },
                ]}
              >
                <Input.TextArea rows={2} />
              </Form.Item>
              <Row gutter={16}>
                <Col xs={24} md={10}>
                  <Form.Item
                    name="paymentMethod"
                    label="Phương thức thanh toán"
                    rules={[
                      {
                        required: true,
                        message: 'Vui lòng chọn phương thức thanh toán.',
                      },
                    ]}
                  >
                    <Select
                      options={Object.entries(PAYMENT_METHOD_LABELS).map(
                        ([value, label]) => ({ value, label }),
                      )}
                    />
                  </Form.Item>
                </Col>
                <Col xs={24} md={14}>
                  <Form.Item name="note" label="Ghi chú">
                    <Input maxLength={500} />
                  </Form.Item>
                </Col>
              </Row>
            </Card>

            <Card title="Sản phẩm đặt mua" className="content-card form-card">
              <Form.List
                name="items"
                rules={[
                  {
                    validator: async (_, value: OrderLinePayload[]) => {
                      if (!value?.length) {
                        throw new Error('Đơn hàng phải có ít nhất một sản phẩm.')
                      }
                      if (hasDuplicateProducts(value)) {
                        throw new Error('Mỗi sản phẩm chỉ được chọn một lần.')
                      }
                    },
                  },
                ]}
              >
                {(fields, { add, remove }, { errors }) => (
                  <>
                    <div className="order-lines-header">
                      <span>Sản phẩm</span>
                      <span>Số lượng</span>
                      <span>Đơn giá</span>
                      <span>Thành tiền</span>
                      <span />
                    </div>
                    {fields.map((field, index) => {
                      const product = products.find(
                        (item) => item.id === lines[index]?.productId,
                      )
                      const quantity = lines[index]?.quantity ?? 0
                      return (
                        <div className="order-line" key={field.key}>
                          <Form.Item
                            {...field}
                            name={[field.name, 'productId']}
                            rules={[
                              {
                                required: true,
                                message: 'Vui lòng chọn sản phẩm.',
                              },
                            ]}
                          >
                            <Select
                              showSearch
                              optionFilterProp="label"
                              placeholder="Chọn sản phẩm"
                              options={products.map((item) => ({
                                value: item.id,
                                label: `${item.productCode} · ${item.productName} (tồn ${item.stockQuantity})${item.requiresContainer ? ` · cọc ${formatCurrency(item.containerDeposit)}/vỏ` : ''}`,
                                disabled:
                                  selectedProductIds.includes(item.id) &&
                                  item.id !== lines[index]?.productId,
                              }))}
                            />
                          </Form.Item>
                          <Form.Item
                            {...field}
                            name={[field.name, 'quantity']}
                            rules={[
                              { required: true, message: 'Nhập số lượng.' },
                              {
                                type: 'integer',
                                min: 1,
                                message: 'Phải là số nguyên dương.',
                              },
                            ]}
                          >
                            <InputNumber min={1} precision={0} />
                          </Form.Item>
                          <div className="order-line-value">
                            {product ? formatCurrency(product.price) : '—'}
                          </div>
                          <div className="order-line-value strong">
                            {product
                              ? formatCurrency(product.price * quantity)
                              : '—'}
                          </div>
                          <Button
                            danger
                            type="text"
                            aria-label="Xóa dòng sản phẩm"
                            icon={<DeleteOutlined />}
                            disabled={fields.length === 1}
                            onClick={() => remove(field.name)}
                          />
                        </div>
                      )
                    })}
                    <Form.ErrorList errors={errors} />
                    <Button
                      type="dashed"
                      icon={<PlusOutlined />}
                      onClick={() => add({ quantity: 1 })}
                      block
                      className="add-line-button"
                    >
                      Thêm sản phẩm
                    </Button>
                  </>
                )}
              </Form.List>
            </Card>
          </Col>
          <Col xs={24} xl={8}>
            <Card title="Tóm tắt đơn hàng" className="content-card order-summary">
              <div className="summary-row">
                <span>Số mặt hàng</span>
                <strong>{lines.length}</strong>
              </div>
              <div className="summary-row">
                <span>Tổng số lượng</span>
                <strong>
                  {formatNumber(
                    lines.reduce(
                      (sum, item) => sum + Number(item?.quantity ?? 0),
                      0,
                    ),
                  )}
                </strong>
              </div>
              <Divider />
              <div className="summary-row">
                <span>Tiền hàng</span>
                <strong>{formatCurrency(total)}</strong>
              </div>
              {maximumDeposit > 0 && (
                <div className="summary-row">
                  <span>Cọc tối đa nếu không trả vỏ</span>
                  <strong>{formatCurrency(maximumDeposit)}</strong>
                </div>
              )}
              <div className="summary-total">
                <span>Tạm tính tiền hàng</span>
                <strong>{formatCurrency(total)}</strong>
              </div>
              <Alert
                type="info"
                showIcon
                message="Tiền cọc chính thức được tính khi giao hàng, theo số bình khách không đổi vỏ."
              />
              <Space direction="vertical" size={12} style={{ width: '100%' }}>
                <Button
                  type="primary"
                  htmlType="submit"
                  size="large"
                  icon={<SaveOutlined />}
                  loading={saving}
                  block
                >
                  {editingId ? 'Lưu thay đổi' : 'Lưu đơn hàng'}
                </Button>
                <Button block onClick={() => navigate('/orders')}>
                  Hủy
                </Button>
              </Space>
            </Card>
          </Col>
        </Row>
      </Form>
      <Modal
        title="Thêm nhanh khách vãng lai"
        open={walkInModalOpen}
        onCancel={() => setWalkInModalOpen(false)}
        onOk={() => void createWalkInCustomer()}
        okText="Tạo và chọn"
        cancelText="Hủy"
        confirmLoading={creatingWalkInCustomer}
        cancelButtonProps={{ disabled: creatingWalkInCustomer }}
        destroyOnHidden
      >
        <Alert
          type="info"
          showIcon
          message="Khách sẽ được tạo thành một hồ sơ riêng để giữ đúng số liên hệ và lịch sử đơn hàng."
          style={{ marginBottom: 20 }}
        />
        <Form
          form={walkInForm}
          layout="vertical"
          requiredMark="optional"
          initialValues={{ customerName: WALK_IN_CUSTOMER_NAME }}
          preserve={false}
        >
          <Form.Item
            name="customerName"
            label="Tên hiển thị"
            rules={[
              {
                required: true,
                whitespace: true,
                message: 'Vui lòng nhập tên hiển thị.',
              },
              { max: 200, message: 'Tên tối đa 200 ký tự.' },
            ]}
          >
            <Input maxLength={200} />
          </Form.Item>
          <Form.Item
            name="phone"
            label="Số điện thoại"
            rules={[
              { required: true, message: 'Vui lòng nhập số điện thoại.' },
              {
                pattern: /^(0|\+84)[0-9]{8,10}$/,
                message: 'Số điện thoại không hợp lệ.',
              },
            ]}
          >
            <Input placeholder="Nhập số điện thoại" maxLength={20} />
          </Form.Item>
          <Form.Item
            name="address"
            label="Địa chỉ giao hàng"
            rules={[
              {
                required: true,
                whitespace: true,
                message: 'Vui lòng nhập địa chỉ giao hàng.',
              },
              { max: 500, message: 'Địa chỉ tối đa 500 ký tự.' },
            ]}
          >
            <Input.TextArea
              rows={3}
              maxLength={500}
              placeholder="Địa chỉ giao hàng thực tế"
            />
          </Form.Item>
        </Form>
      </Modal>
    </>
  )
}
