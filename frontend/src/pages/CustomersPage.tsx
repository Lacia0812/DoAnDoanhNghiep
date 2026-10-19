import { useCallback, useEffect, useState } from 'react'
import {
  Button,
  Card,
  Col,
  Drawer,
  Form,
  Input,
  Modal,
  Popconfirm,
  Row,
  Select,
  Space,
  Table,
  Tag,
  Tooltip,
  Typography,
  message,
  type TableColumnsType,
} from 'antd'
import {
  DeleteOutlined,
  EditOutlined,
  EyeOutlined,
  PlusOutlined,
  ReloadOutlined,
  SearchOutlined,
} from '@ant-design/icons'
import PageHeader from '../components/PageHeader'
import { OrderStatusTag } from '../components/StatusTag'
import { customerApi, getApiErrorMessage } from '../services/api'
import type { Customer, CustomerPayload, Order } from '../types'
import {
  CUSTOMER_TYPE_OPTIONS,
  PAGE_SIZE_OPTIONS,
  getCustomerTypeLabel,
} from '../utils/constants'
import { formatCurrency, formatDateTime } from '../utils/format'

export default function CustomersPage() {
  const [form] = Form.useForm<CustomerPayload>()
  const [customers, setCustomers] = useState<Customer[]>([])
  const [loading, setLoading] = useState(false)
  const [saving, setSaving] = useState(false)
  const [editing, setEditing] = useState<Customer | null>(null)
  const [modalOpen, setModalOpen] = useState(false)
  const [searchDraft, setSearchDraft] = useState('')
  const [search, setSearch] = useState('')
  const [customerType, setCustomerType] = useState<string>()
  const [page, setPage] = useState(1)
  const [pageSize, setPageSize] = useState(10)
  const [total, setTotal] = useState(0)
  const [historyCustomer, setHistoryCustomer] = useState<Customer | null>(null)
  const [history, setHistory] = useState<Order[]>([])
  const [historyLoading, setHistoryLoading] = useState(false)

  const loadCustomers = useCallback(async () => {
    setLoading(true)
    try {
      const result = await customerApi.list({
        page,
        pageSize,
        search: search || undefined,
        customerType,
      })
      setCustomers(result.items)
      setTotal(result.totalCount)
    } catch (error) {
      message.error(getApiErrorMessage(error, 'Không thể tải khách hàng.'))
    } finally {
      setLoading(false)
    }
  }, [customerType, page, pageSize, search])

  useEffect(() => {
    loadCustomers()
  }, [loadCustomers])

  const openCreate = () => {
    setEditing(null)
    form.resetFields()
    form.setFieldValue('customerType', 'Individual')
    setModalOpen(true)
  }

  const openEdit = (customer: Customer) => {
    setEditing(customer)
    form.setFieldsValue({
      customerCode: customer.customerCode,
      customerName: customer.customerName,
      phone: customer.phone,
      email: customer.email ?? undefined,
      address: customer.address,
      customerType: customer.customerType,
      note: customer.note ?? undefined,
    })
    setModalOpen(true)
  }

  const submit = async () => {
    try {
      const values = await form.validateFields()
      const payload = {
        ...values,
        email: values.email?.trim() || undefined,
        note: values.note?.trim() || undefined,
      }
      setSaving(true)
      if (editing) {
        await customerApi.update(editing.id, {
          ...payload,
          isActive: editing.isActive,
        })
        message.success('Cập nhật khách hàng thành công.')
      } else {
        await customerApi.create(payload)
        message.success('Thêm khách hàng thành công.')
      }
      setModalOpen(false)
      await loadCustomers()
    } catch (error) {
      if ((error as { errorFields?: unknown }).errorFields) return
      message.error(getApiErrorMessage(error, 'Không thể lưu khách hàng.'))
    } finally {
      setSaving(false)
    }
  }

  const remove = async (id: number) => {
    try {
      await customerApi.remove(id)
      message.success('Đã ngừng hoạt động khách hàng.')
      await loadCustomers()
    } catch (error) {
      message.error(getApiErrorMessage(error, 'Không thể xóa khách hàng.'))
    }
  }

  const showHistory = async (customer: Customer) => {
    setHistoryCustomer(customer)
    setHistory([])
    setHistoryLoading(true)
    try {
      const result = await customerApi.orders(customer.id, {
        page: 1,
        pageSize: 100,
      })
      setHistory(result.items)
    } catch (error) {
      message.error(getApiErrorMessage(error, 'Không thể tải lịch sử đơn hàng.'))
    } finally {
      setHistoryLoading(false)
    }
  }

  const columns: TableColumnsType<Customer> = [
    {
      title: 'Mã',
      dataIndex: 'customerCode',
      width: 120,
      fixed: 'left',
      render: (value) => <Typography.Text strong>{value}</Typography.Text>,
    },
    {
      title: 'Khách hàng',
      dataIndex: 'customerName',
      width: 220,
      render: (value, record) => (
        <div>
          <Typography.Text strong>{value}</Typography.Text>
          {record.email && <div className="table-secondary">{record.email}</div>}
        </div>
      ),
    },
    { title: 'Số điện thoại', dataIndex: 'phone', width: 140 },
    {
      title: 'Loại',
      dataIndex: 'customerType',
      width: 130,
      render: getCustomerTypeLabel,
    },
    {
      title: 'Vỏ đang giữ',
      dataIndex: 'containerBalance',
      width: 120,
      align: 'right',
      render: (value) => `${value ?? 0} vỏ`,
    },
    {
      title: 'Công nợ',
      dataIndex: 'outstandingDebt',
      width: 150,
      align: 'right',
      render: (value) => (
        <Typography.Text strong type={value > 0 ? 'danger' : undefined}>
          {formatCurrency(value ?? 0)}
        </Typography.Text>
      ),
    },
    {
      title: 'Địa chỉ',
      dataIndex: 'address',
      width: 300,
      ellipsis: true,
    },
    {
      title: 'Trạng thái',
      dataIndex: 'isActive',
      width: 120,
      render: (value) =>
        value ? <Tag color="success">Hoạt động</Tag> : <Tag>Ngừng hoạt động</Tag>,
    },
    {
      title: 'Ngày tạo',
      dataIndex: 'createdAt',
      width: 160,
      render: formatDateTime,
    },
    {
      title: 'Thao tác',
      key: 'actions',
      width: 150,
      fixed: 'right',
      render: (_, record) => (
        <Space>
          <Tooltip title="Lịch sử đơn hàng">
            <Button
              type="text"
              icon={<EyeOutlined />}
              onClick={() => showHistory(record)}
            />
          </Tooltip>
          <Tooltip title="Sửa">
            <Button
              type="text"
              icon={<EditOutlined />}
              onClick={() => openEdit(record)}
            />
          </Tooltip>
          <Popconfirm
            title="Ngừng hoạt động khách hàng?"
            description="Khách hàng sẽ không còn xuất hiện khi tạo đơn."
            okText="Xóa"
            cancelText="Bỏ qua"
            okButtonProps={{ danger: true }}
            onConfirm={() => remove(record.id)}
          >
            <Tooltip title="Xóa">
              <Button type="text" danger icon={<DeleteOutlined />} />
            </Tooltip>
          </Popconfirm>
        </Space>
      ),
    },
  ]

  return (
    <>
      <PageHeader
        title="Khách hàng"
        subtitle="Quản lý thông tin và theo dõi lịch sử đặt nước."
        actions={
          <Button type="primary" icon={<PlusOutlined />} onClick={openCreate}>
            Thêm khách hàng
          </Button>
        }
      />
      <Card className="content-card">
        <div className="table-toolbar">
          <Space wrap>
            <Input
              allowClear
              prefix={<SearchOutlined />}
              value={searchDraft}
              placeholder="Tên, mã hoặc số điện thoại"
              onChange={(event) => setSearchDraft(event.target.value)}
              onPressEnter={() => {
                setPage(1)
                setSearch(searchDraft.trim())
              }}
              style={{ width: 270 }}
            />
            <Select
              allowClear
              placeholder="Tất cả loại khách hàng"
              options={CUSTOMER_TYPE_OPTIONS}
              value={customerType}
              onChange={(value) => {
                setPage(1)
                setCustomerType(value)
              }}
              style={{ width: 200 }}
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
          <Button icon={<ReloadOutlined />} onClick={loadCustomers}>
            Làm mới
          </Button>
        </div>
        <Table
          rowKey="id"
          columns={columns}
          dataSource={customers}
          loading={loading}
          scroll={{ x: 1550 }}
          pagination={{
            current: page,
            pageSize,
            total,
            showSizeChanger: true,
            pageSizeOptions: PAGE_SIZE_OPTIONS,
            showTotal: (count) => `${count} khách hàng`,
            onChange: (nextPage, nextSize) => {
              setPage(nextSize !== pageSize ? 1 : nextPage)
              setPageSize(nextSize)
            },
          }}
        />
      </Card>

      <Modal
        title={editing ? 'Cập nhật khách hàng' : 'Thêm khách hàng'}
        open={modalOpen}
        onCancel={() => setModalOpen(false)}
        onOk={submit}
        okText={editing ? 'Lưu thay đổi' : 'Thêm khách hàng'}
        cancelText="Hủy"
        confirmLoading={saving}
        width={720}
        destroyOnHidden
      >
        <Form form={form} layout="vertical" requiredMark="optional">
          <Row gutter={16}>
            <Col xs={24} md={12}>
              <Form.Item
                name="customerCode"
                label="Mã khách hàng"
                rules={[
                  { required: true, message: 'Vui lòng nhập mã khách hàng.' },
                  { max: 30, message: 'Mã tối đa 30 ký tự.' },
                ]}
              >
                <Input placeholder="Nhập mã khách hàng" disabled={Boolean(editing)} />
              </Form.Item>
            </Col>
            <Col xs={24} md={12}>
              <Form.Item
                name="customerType"
                label="Loại khách hàng"
                rules={[{ required: true, message: 'Vui lòng chọn loại.' }]}
              >
                <Select options={CUSTOMER_TYPE_OPTIONS} />
              </Form.Item>
            </Col>
          </Row>
          <Form.Item
            name="customerName"
            label="Tên khách hàng / đơn vị"
            rules={[
              { required: true, message: 'Vui lòng nhập tên khách hàng.' },
              { max: 150, message: 'Tên tối đa 150 ký tự.' },
            ]}
          >
            <Input placeholder="Nhập tên khách hàng hoặc đơn vị" />
          </Form.Item>
          <Row gutter={16}>
            <Col xs={24} md={12}>
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
                <Input placeholder="Nhập số điện thoại" />
              </Form.Item>
            </Col>
            <Col xs={24} md={12}>
              <Form.Item
                name="email"
                label="Email"
                rules={[{ type: 'email', message: 'Email không hợp lệ.' }]}
              >
                <Input placeholder="Nhập email (nếu có)" />
              </Form.Item>
            </Col>
          </Row>
          <Form.Item
            name="address"
            label="Địa chỉ"
            rules={[{ required: true, message: 'Vui lòng nhập địa chỉ.' }]}
          >
            <Input.TextArea rows={2} placeholder="Địa chỉ giao hàng mặc định" />
          </Form.Item>
          <Form.Item name="note" label="Ghi chú">
            <Input.TextArea rows={2} maxLength={500} showCount />
          </Form.Item>
        </Form>
      </Modal>

      <Drawer
        title={`Lịch sử đơn hàng · ${historyCustomer?.customerName ?? ''}`}
        width={720}
        open={Boolean(historyCustomer)}
        onClose={() => setHistoryCustomer(null)}
      >
        {historyCustomer && (
          <Row gutter={12} className="page-alert">
            <Col span={12}>
              <Card size="small">
                <Typography.Text type="secondary">Vỏ đang giữ</Typography.Text>
                <Typography.Title level={4}>
                  {historyCustomer.containerBalance ?? 0} vỏ
                </Typography.Title>
              </Card>
            </Col>
            <Col span={12}>
              <Card size="small">
                <Typography.Text type="secondary">Công nợ</Typography.Text>
                <Typography.Title level={4} type="danger">
                  {formatCurrency(historyCustomer.outstandingDebt ?? 0)}
                </Typography.Title>
              </Card>
            </Col>
          </Row>
        )}
        <Table
          rowKey="id"
          loading={historyLoading}
          dataSource={history}
          scroll={{ x: 650 }}
          pagination={{ pageSize: 8 }}
          columns={[
            {
              title: 'Mã đơn',
              dataIndex: 'orderCode',
              width: 130,
              render: (value) => <Typography.Text strong>{value}</Typography.Text>,
            },
            {
              title: 'Ngày đặt',
              dataIndex: 'orderDate',
              width: 160,
              render: formatDateTime,
            },
            {
              title: 'Tổng tiền',
              dataIndex: 'totalAmount',
              align: 'right',
              render: formatCurrency,
            },
            {
              title: 'Trạng thái',
              dataIndex: 'orderStatus',
              render: (status) => <OrderStatusTag status={status} />,
            },
          ]}
        />
      </Drawer>
    </>
  )
}
