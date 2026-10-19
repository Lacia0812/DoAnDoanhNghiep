import { useCallback, useEffect, useState } from 'react'
import {
  Button,
  Card,
  Col,
  Form,
  Input,
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
  KeyOutlined,
  PlusOutlined,
  ReloadOutlined,
  SearchOutlined,
} from '@ant-design/icons'
import PageHeader from '../components/PageHeader'
import {
  getApiErrorMessage,
  userApi,
  type UserCreatePayload,
  type UserUpdatePayload,
} from '../services/api'
import type { Role, User } from '../types'
import { PAGE_SIZE_OPTIONS, ROLE_LABELS, ROLE_OPTIONS } from '../utils/constants'
import { formatDateTime } from '../utils/format'

type UserFormValues = UserCreatePayload & { isActive: boolean }

export default function UsersPage() {
  const [form] = Form.useForm<UserFormValues>()
  const [passwordForm] = Form.useForm<{
    newPassword: string
    confirmPassword: string
  }>()
  const [users, setUsers] = useState<User[]>([])
  const [loading, setLoading] = useState(false)
  const [saving, setSaving] = useState(false)
  const [modalOpen, setModalOpen] = useState(false)
  const [editing, setEditing] = useState<User | null>(null)
  const [passwordUser, setPasswordUser] = useState<User | null>(null)
  const [searchDraft, setSearchDraft] = useState('')
  const [search, setSearch] = useState('')
  const [role, setRole] = useState<Role>()
  const [isActive, setIsActive] = useState<boolean | undefined>()
  const [page, setPage] = useState(1)
  const [pageSize, setPageSize] = useState(10)
  const [total, setTotal] = useState(0)

  const loadUsers = useCallback(async () => {
    setLoading(true)
    try {
      const result = await userApi.list({
        page,
        pageSize,
        search: search || undefined,
        role,
        isActive,
      })
      setUsers(result.items)
      setTotal(result.totalCount)
    } catch (error) {
      message.error(getApiErrorMessage(error, 'Không thể tải danh sách tài khoản.'))
    } finally {
      setLoading(false)
    }
  }, [isActive, page, pageSize, role, search])

  useEffect(() => {
    loadUsers()
  }, [loadUsers])

  const openCreate = () => {
    setEditing(null)
    form.resetFields()
    form.setFieldsValue({ role: 'Sales', isActive: true })
    setModalOpen(true)
  }

  const openEdit = (user: User) => {
    setEditing(user)
    form.resetFields()
    form.setFieldsValue({
      fullName: user.fullName,
      username: user.username,
      role: user.role,
      isActive: user.isActive,
      password: '',
    })
    setModalOpen(true)
  }

  const submit = async () => {
    try {
      const values = await form.validateFields()
      setSaving(true)
      if (editing) {
        const payload: UserUpdatePayload = {
          fullName: values.fullName,
          username: values.username,
          role: values.role,
          isActive: values.isActive,
        }
        await userApi.update(editing.id, payload)
        message.success('Cập nhật tài khoản thành công.')
      } else {
        const payload: UserCreatePayload = {
          fullName: values.fullName,
          username: values.username,
          password: values.password,
          role: values.role,
        }
        await userApi.create(payload)
        message.success('Tạo tài khoản thành công.')
      }
      setModalOpen(false)
      await loadUsers()
    } catch (error) {
      if ((error as { errorFields?: unknown }).errorFields) return
      message.error(getApiErrorMessage(error, 'Không thể lưu tài khoản.'))
    } finally {
      setSaving(false)
    }
  }

  const remove = async (id: number) => {
    try {
      await userApi.remove(id)
      message.success('Đã khóa tài khoản.')
      await loadUsers()
    } catch (error) {
      message.error(getApiErrorMessage(error, 'Không thể khóa tài khoản.'))
    }
  }

  const changePassword = async () => {
    if (!passwordUser) return
    try {
      const values = await passwordForm.validateFields()
      setSaving(true)
      await userApi.changePassword(passwordUser.id, values.newPassword)
      message.success('Đổi mật khẩu thành công.')
      setPasswordUser(null)
      passwordForm.resetFields()
    } catch (error) {
      if ((error as { errorFields?: unknown }).errorFields) return
      message.error(getApiErrorMessage(error, 'Không thể đổi mật khẩu.'))
    } finally {
      setSaving(false)
    }
  }

  const columns: TableColumnsType<User> = [
    {
      title: 'Họ và tên',
      dataIndex: 'fullName',
      width: 230,
      fixed: 'left',
      render: (value) => <Typography.Text strong>{value}</Typography.Text>,
    },
    { title: 'Tên đăng nhập', dataIndex: 'username', width: 170 },
    {
      title: 'Vai trò',
      dataIndex: 'role',
      width: 190,
      render: (value: Role) => (
        <Tag color={value === 'Admin' ? 'purple' : 'blue'}>
          {ROLE_LABELS[value] ?? value}
        </Tag>
      ),
    },
    {
      title: 'Trạng thái',
      dataIndex: 'isActive',
      width: 140,
      render: (value) =>
        value ? <Tag color="success">Hoạt động</Tag> : <Tag>Đã khóa</Tag>,
    },
    {
      title: 'Ngày tạo',
      dataIndex: 'createdAt',
      width: 170,
      render: formatDateTime,
    },
    {
      title: 'Thao tác',
      key: 'actions',
      width: 150,
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
          <Tooltip title="Đổi mật khẩu">
            <Button
              type="text"
              icon={<KeyOutlined />}
              onClick={() => {
                passwordForm.resetFields()
                setPasswordUser(record)
              }}
            />
          </Tooltip>
          {record.isActive && (
            <Popconfirm
              title="Khóa tài khoản này?"
              description="Người dùng sẽ không thể tiếp tục đăng nhập."
              okText="Khóa"
              cancelText="Bỏ qua"
              okButtonProps={{ danger: true }}
              onConfirm={() => remove(record.id)}
            >
              <Tooltip title="Khóa tài khoản">
                <Button type="text" danger icon={<DeleteOutlined />} />
              </Tooltip>
            </Popconfirm>
          )}
        </Space>
      ),
    },
  ]

  return (
    <>
      <PageHeader
        title="Tài khoản"
        subtitle="Quản lý người dùng và vai trò trong hệ thống."
        actions={
          <Button type="primary" icon={<PlusOutlined />} onClick={openCreate}>
            Thêm tài khoản
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
              placeholder="Họ tên hoặc tên đăng nhập"
              onChange={(event) => setSearchDraft(event.target.value)}
              onPressEnter={() => {
                setPage(1)
                setSearch(searchDraft.trim())
              }}
              style={{ width: 260 }}
            />
            <Select
              allowClear
              value={role}
              placeholder="Tất cả vai trò"
              options={ROLE_OPTIONS}
              onChange={(value) => {
                setPage(1)
                setRole(value)
              }}
              style={{ width: 200 }}
            />
            <Select
              allowClear
              value={isActive}
              placeholder="Tất cả trạng thái"
              options={[
                { value: true, label: 'Hoạt động' },
                { value: false, label: 'Đã khóa' },
              ]}
              onChange={(value) => {
                setPage(1)
                setIsActive(value)
              }}
              style={{ width: 170 }}
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
          <Button icon={<ReloadOutlined />} onClick={loadUsers}>
            Làm mới
          </Button>
        </div>
        <Table
          rowKey="id"
          columns={columns}
          dataSource={users}
          loading={loading}
          scroll={{ x: 1050 }}
          pagination={{
            current: page,
            pageSize,
            total,
            showSizeChanger: true,
            pageSizeOptions: PAGE_SIZE_OPTIONS,
            showTotal: (count) => `${count} tài khoản`,
            onChange: (nextPage, nextSize) => {
              setPage(nextSize !== pageSize ? 1 : nextPage)
              setPageSize(nextSize)
            },
          }}
        />
      </Card>

      <Modal
        title={editing ? 'Cập nhật tài khoản' : 'Thêm tài khoản'}
        open={modalOpen}
        onCancel={() => setModalOpen(false)}
        onOk={submit}
        okText={editing ? 'Lưu thay đổi' : 'Tạo tài khoản'}
        cancelText="Hủy"
        confirmLoading={saving}
        width={620}
        destroyOnHidden
      >
        <Form form={form} layout="vertical" requiredMark="optional">
          <Form.Item
            name="fullName"
            label="Họ và tên"
            rules={[
              { required: true, message: 'Vui lòng nhập họ và tên.' },
              { max: 100, message: 'Họ tên tối đa 100 ký tự.' },
            ]}
          >
            <Input />
          </Form.Item>
          <Row gutter={16}>
            <Col xs={24} md={12}>
              <Form.Item
                name="username"
                label="Tên đăng nhập"
                rules={[
                  { required: true, message: 'Vui lòng nhập tên đăng nhập.' },
                  {
                    pattern: /^[a-zA-Z0-9._-]+$/,
                    message: 'Chỉ dùng chữ, số, dấu chấm, gạch ngang.',
                  },
                ]}
              >
                <Input autoComplete="off" />
              </Form.Item>
            </Col>
            <Col xs={24} md={12}>
              <Form.Item
                name="role"
                label="Vai trò"
                rules={[{ required: true, message: 'Vui lòng chọn vai trò.' }]}
              >
                <Select options={ROLE_OPTIONS} />
              </Form.Item>
            </Col>
          </Row>
          {!editing && (
            <Form.Item
              name="password"
              label="Mật khẩu"
              rules={[
                { required: true, message: 'Vui lòng nhập mật khẩu.' },
                { min: 6, message: 'Mật khẩu có ít nhất 6 ký tự.' },
              ]}
            >
              <Input.Password autoComplete="new-password" />
            </Form.Item>
          )}
          {editing && (
            <Form.Item
              name="isActive"
              label="Cho phép đăng nhập"
              valuePropName="checked"
            >
              <Switch checkedChildren="Có" unCheckedChildren="Không" />
            </Form.Item>
          )}
        </Form>
      </Modal>

      <Modal
        title={`Đổi mật khẩu · ${passwordUser?.fullName ?? ''}`}
        open={Boolean(passwordUser)}
        onCancel={() => setPasswordUser(null)}
        onOk={changePassword}
        okText="Đổi mật khẩu"
        cancelText="Hủy"
        confirmLoading={saving}
        destroyOnHidden
      >
        <Form form={passwordForm} layout="vertical" requiredMark="optional">
          <Form.Item
            name="newPassword"
            label="Mật khẩu mới"
            rules={[
              { required: true, message: 'Vui lòng nhập mật khẩu mới.' },
              { min: 6, message: 'Mật khẩu có ít nhất 6 ký tự.' },
            ]}
          >
            <Input.Password autoComplete="new-password" />
          </Form.Item>
          <Form.Item
            name="confirmPassword"
            label="Xác nhận mật khẩu"
            dependencies={['newPassword']}
            rules={[
              { required: true, message: 'Vui lòng nhập lại mật khẩu.' },
              ({ getFieldValue }) => ({
                validator(_, value) {
                  return !value || getFieldValue('newPassword') === value
                    ? Promise.resolve()
                    : Promise.reject(new Error('Mật khẩu xác nhận không khớp.'))
                },
              }),
            ]}
          >
            <Input.Password autoComplete="new-password" />
          </Form.Item>
        </Form>
      </Modal>
    </>
  )
}
