import { useCallback, useEffect, useState } from 'react'
import {
  Button,
  Card,
  Col,
  Form,
  Input,
  InputNumber,
  Modal,
  Popconfirm,
  Row,
  Select,
  Space,
  Switch,
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
  ExclamationCircleOutlined,
  PlusOutlined,
  ReloadOutlined,
  SearchOutlined,
} from '@ant-design/icons'
import PageHeader from '../components/PageHeader'
import { categoryApi, getApiErrorMessage, productApi } from '../services/api'
import type { Product, ProductCategory, ProductPayload } from '../types'
import { PAGE_SIZE_OPTIONS } from '../utils/constants'
import { formatCurrency, formatDateTime, formatNumber } from '../utils/format'

interface ProductFormValues extends ProductPayload {}

export default function ProductsPage() {
  const [form] = Form.useForm<ProductFormValues>()
  const [products, setProducts] = useState<Product[]>([])
  const [categories, setCategories] = useState<ProductCategory[]>([])
  const [loading, setLoading] = useState(false)
  const [saving, setSaving] = useState(false)
  const [modalOpen, setModalOpen] = useState(false)
  const [editing, setEditing] = useState<Product | null>(null)
  const [searchDraft, setSearchDraft] = useState('')
  const [search, setSearch] = useState('')
  const [categoryId, setCategoryId] = useState<number | undefined>()
  const [page, setPage] = useState(1)
  const [pageSize, setPageSize] = useState(10)
  const [total, setTotal] = useState(0)

  const loadProducts = useCallback(async () => {
    setLoading(true)
    try {
      const result = await productApi.list({
        page,
        pageSize,
        search: search || undefined,
        categoryId,
      })
      setProducts(result.items)
      setTotal(result.totalCount)
    } catch (error) {
      message.error(getApiErrorMessage(error, 'Không thể tải danh sách sản phẩm.'))
    } finally {
      setLoading(false)
    }
  }, [categoryId, page, pageSize, search])

  useEffect(() => {
    loadProducts()
  }, [loadProducts])

  useEffect(() => {
    categoryApi
      .list()
      .then(setCategories)
      .catch((error) =>
        message.error(getApiErrorMessage(error, 'Không thể tải loại sản phẩm.')),
      )
  }, [])

  const openCreate = () => {
    setEditing(null)
    form.resetFields()
    form.setFieldsValue({
      unit: 'Bình',
      minimumStock: 10,
      initialStock: 0,
      requiresContainer: false,
      containerDeposit: 0,
    })
    setModalOpen(true)
  }

  const openEdit = (product: Product) => {
    setEditing(product)
    form.setFieldsValue({
      productCode: product.productCode,
      productName: product.productName,
      categoryId: product.categoryId,
      capacity: product.capacity,
      unit: product.unit,
      price: product.price,
      requiresContainer: product.requiresContainer,
      containerDeposit: product.containerDeposit,
      minimumStock: product.minimumStock,
    })
    setModalOpen(true)
  }

  const submit = async () => {
    try {
      const values = await form.validateFields()
      setSaving(true)
      if (editing) {
        const { initialStock: _initialStock, ...payload } = values
        await productApi.update(editing.id, {
          ...payload,
          isActive: editing.isActive,
        })
        message.success('Cập nhật sản phẩm thành công.')
      } else {
        await productApi.create(values)
        message.success('Thêm sản phẩm thành công.')
      }
      setModalOpen(false)
      await loadProducts()
    } catch (error) {
      if ((error as { errorFields?: unknown }).errorFields) return
      message.error(getApiErrorMessage(error, 'Không thể lưu sản phẩm.'))
    } finally {
      setSaving(false)
    }
  }

  const remove = async (id: number) => {
    try {
      await productApi.remove(id)
      message.success('Đã ngừng kinh doanh sản phẩm.')
      await loadProducts()
    } catch (error) {
      message.error(getApiErrorMessage(error, 'Không thể xóa sản phẩm.'))
    }
  }

  const columns: TableColumnsType<Product> = [
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
      width: 230,
      render: (value, record) => (
        <div>
          <Typography.Text strong>{value}</Typography.Text>
          <div className="table-secondary">
            {[record.capacity, record.unit].filter(Boolean).join(' · ')}
          </div>
        </div>
      ),
    },
    { title: 'Loại', dataIndex: 'categoryName', width: 160 },
    {
      title: 'Giá bán',
      dataIndex: 'price',
      width: 140,
      align: 'right',
      render: formatCurrency,
    },
    {
      title: 'Vỏ bình / tiền cọc',
      key: 'container',
      width: 170,
      render: (_, record) =>
        record.requiresContainer ? (
          <div>
            <Tag color="blue">Có thu hồi vỏ</Tag>
            <div className="table-secondary">Cọc {formatCurrency(record.containerDeposit)}</div>
          </div>
        ) : (
          <Typography.Text type="secondary">Không áp dụng</Typography.Text>
        ),
    },
    {
      title: 'Tồn kho',
      dataIndex: 'stockQuantity',
      width: 120,
      align: 'right',
      render: (value, record) => (
        <Space size={4}>
          <Typography.Text type={record.isLowStock ? 'danger' : undefined} strong>
            {formatNumber(value)}
          </Typography.Text>
          {record.isLowStock && (
            <Tooltip title={`Dưới mức tối thiểu ${record.minimumStock}`}>
              <ExclamationCircleOutlined className="text-danger" />
            </Tooltip>
          )}
        </Space>
      ),
    },
    {
      title: 'Tối thiểu',
      dataIndex: 'minimumStock',
      width: 110,
      align: 'right',
    },
    {
      title: 'Trạng thái',
      dataIndex: 'isActive',
      width: 120,
      render: (value) =>
        value ? <Tag color="success">Đang bán</Tag> : <Tag>Ngừng bán</Tag>,
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
      width: 110,
      fixed: 'right',
      render: (_, record) => (
        <Space>
          <Tooltip title="Sửa">
            <Button
              type="text"
              icon={<EditOutlined />}
              onClick={() => openEdit(record)}
            />
          </Tooltip>
          <Popconfirm
            title="Ngừng kinh doanh sản phẩm?"
            description="Sản phẩm sẽ không còn xuất hiện khi tạo đơn mới."
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
        title="Sản phẩm"
        subtitle="Quản lý danh mục nước uống, giá bán và ngưỡng tồn kho."
        actions={
          <Button type="primary" icon={<PlusOutlined />} onClick={openCreate}>
            Thêm sản phẩm
          </Button>
        }
      />
      <Card className="content-card">
        <div className="table-toolbar">
          <Space wrap>
            <Input
              allowClear
              value={searchDraft}
              prefix={<SearchOutlined />}
              placeholder="Tìm theo mã hoặc tên"
              onChange={(event) => setSearchDraft(event.target.value)}
              onPressEnter={() => {
                setPage(1)
                setSearch(searchDraft.trim())
              }}
              style={{ width: 260 }}
            />
            <Select
              allowClear
              placeholder="Tất cả loại sản phẩm"
              value={categoryId}
              options={categories.map((item) => ({
                value: item.id,
                label: item.name,
              }))}
              onChange={(value) => {
                setPage(1)
                setCategoryId(value)
              }}
              style={{ width: 210 }}
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
          <Button icon={<ReloadOutlined />} onClick={loadProducts}>
            Làm mới
          </Button>
        </div>
        <Table
          rowKey="id"
          loading={loading}
          columns={columns}
          dataSource={products}
          scroll={{ x: 1280 }}
          pagination={{
            current: page,
            pageSize,
            total,
            showSizeChanger: true,
            pageSizeOptions: PAGE_SIZE_OPTIONS,
            showTotal: (count) => `${count} sản phẩm`,
            onChange: (nextPage, nextSize) => {
              setPage(nextSize !== pageSize ? 1 : nextPage)
              setPageSize(nextSize)
            },
          }}
        />
      </Card>

      <Modal
        title={editing ? 'Cập nhật sản phẩm' : 'Thêm sản phẩm'}
        open={modalOpen}
        onCancel={() => setModalOpen(false)}
        onOk={submit}
        okText={editing ? 'Lưu thay đổi' : 'Thêm sản phẩm'}
        cancelText="Hủy"
        confirmLoading={saving}
        width={720}
        destroyOnHidden
      >
        <Form form={form} layout="vertical" requiredMark="optional">
          <Row gutter={16}>
            <Col xs={24} md={12}>
              <Form.Item
                name="productCode"
                label="Mã sản phẩm"
                rules={[
                  { required: true, message: 'Vui lòng nhập mã sản phẩm.' },
                  { max: 30, message: 'Mã tối đa 30 ký tự.' },
                ]}
              >
                <Input placeholder="VD: GP-20L" disabled={Boolean(editing)} />
              </Form.Item>
            </Col>
            <Col xs={24} md={12}>
              <Form.Item
                name="categoryId"
                label="Loại sản phẩm"
                rules={[{ required: true, message: 'Vui lòng chọn loại.' }]}
              >
                <Select
                  placeholder="Chọn loại"
                  options={categories.map((item) => ({
                    value: item.id,
                    label: item.name,
                  }))}
                />
              </Form.Item>
            </Col>
          </Row>
          <Row gutter={16}>
            <Col xs={24} md={12}>
              <Form.Item
                name="requiresContainer"
                label="Quản lý vỏ bình"
                valuePropName="checked"
              >
                <Switch checkedChildren="Có" unCheckedChildren="Không" />
              </Form.Item>
            </Col>
            <Form.Item noStyle shouldUpdate={(previous, current) =>
              previous.requiresContainer !== current.requiresContainer
            }>
              {({ getFieldValue }) => getFieldValue('requiresContainer') ? (
                <Col xs={24} md={12}>
                  <Form.Item
                    name="containerDeposit"
                    label="Tiền cọc mỗi vỏ"
                    rules={[{ required: true, message: 'Vui lòng nhập tiền cọc.' }]}
                  >
                    <InputNumber min={1} precision={0} addonAfter="₫" style={{ width: '100%' }} />
                  </Form.Item>
                </Col>
              ) : null}
            </Form.Item>
          </Row>
          <Form.Item
            name="productName"
            label="Tên sản phẩm"
            rules={[
              { required: true, message: 'Vui lòng nhập tên sản phẩm.' },
              { max: 150, message: 'Tên tối đa 150 ký tự.' },
            ]}
          >
            <Input placeholder="Bình nước tinh khiết Gia Phú" />
          </Form.Item>
          <Row gutter={16}>
            <Col xs={24} md={12}>
              <Form.Item
                name="capacity"
                label="Dung tích"
                rules={[{ required: true, message: 'Vui lòng nhập dung tích.' }]}
              >
                <Input placeholder="VD: 20 lít" />
              </Form.Item>
            </Col>
            <Col xs={24} md={12}>
              <Form.Item
                name="unit"
                label="Đơn vị tính"
                rules={[{ required: true, message: 'Vui lòng nhập đơn vị tính.' }]}
              >
                <Input placeholder="Bình, chai, thùng..." />
              </Form.Item>
            </Col>
          </Row>
          <Row gutter={16}>
            <Col xs={24} md={8}>
              <Form.Item
                name="price"
                label="Giá bán"
                rules={[{ required: true, message: 'Vui lòng nhập giá bán.' }]}
              >
                <InputNumber
                  min={1}
                  precision={0}
                  addonAfter="₫"
                  style={{ width: '100%' }}
                />
              </Form.Item>
            </Col>
            <Col xs={24} md={8}>
              <Form.Item
                name="minimumStock"
                label="Tồn kho tối thiểu"
                rules={[{ required: true, message: 'Vui lòng nhập mức tối thiểu.' }]}
              >
                <InputNumber min={0} precision={0} style={{ width: '100%' }} />
              </Form.Item>
            </Col>
            {!editing && (
              <Col xs={24} md={8}>
                <Form.Item
                  name="initialStock"
                  label="Tồn kho ban đầu"
                  rules={[{ required: true, message: 'Vui lòng nhập tồn ban đầu.' }]}
                >
                  <InputNumber min={0} precision={0} style={{ width: '100%' }} />
                </Form.Item>
              </Col>
            )}
          </Row>
        </Form>
      </Modal>
    </>
  )
}
