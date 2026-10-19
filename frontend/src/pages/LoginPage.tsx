import { useEffect, useState } from 'react'
import {
  Button,
  Checkbox,
  Form,
  Input,
  Typography,
  message,
} from 'antd'
import {
  ArrowRightOutlined,
  CarOutlined,
  CheckCircleFilled,
  DatabaseOutlined,
  LockOutlined,
  ShoppingCartOutlined,
  UserOutlined,
} from '@ant-design/icons'
import { Navigate, useLocation, useNavigate } from 'react-router-dom'
import { useAuth } from '../contexts/AuthContext'
import { getApiErrorMessage } from '../services/api'
import type { LoginRequest } from '../types'
import { ROLE_HOME } from '../utils/constants'

interface LoginFormValues extends LoginRequest {
  remember: boolean
}

const demoAccounts = [
  { label: 'Quản trị', username: 'admin', password: 'Admin@123' },
  { label: 'Bán hàng', username: 'sales', password: 'Sales@123' },
  { label: 'Kho', username: 'warehouse', password: 'Warehouse@123' },
  { label: 'Giao hàng', username: 'delivery', password: 'Delivery@123' },
]

export default function LoginPage() {
  const { login, isAuthenticated, user, loading } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()
  const [submitting, setSubmitting] = useState(false)
  const [form] = Form.useForm<LoginFormValues>()

  useEffect(() => {
    const remembered = localStorage.getItem('gia-phu-remembered-username')
    if (remembered) form.setFieldsValue({ username: remembered, remember: true })
  }, [form])

  if (!loading && isAuthenticated && user) {
    return <Navigate to={ROLE_HOME[user.role]} replace />
  }

  const submit = async (values: LoginFormValues) => {
    setSubmitting(true)
    try {
      const loggedInUser = await login(values)
      if (values.remember) {
        localStorage.setItem('gia-phu-remembered-username', values.username)
      } else {
        localStorage.removeItem('gia-phu-remembered-username')
      }
      message.success(`Xin chào, ${loggedInUser.fullName}!`)
      const requestedPath = (
        location.state as { from?: { pathname?: string } } | null
      )?.from?.pathname
      navigate(requestedPath || ROLE_HOME[loggedInUser.role], { replace: true })
    } catch (error) {
      message.error(
        getApiErrorMessage(
          error,
          'Tên đăng nhập hoặc mật khẩu không chính xác.',
        ),
      )
    } finally {
      setSubmitting(false)
    }
  }

  const selectDemoAccount = (username: string, password: string) => {
    form.setFieldsValue({ username, password })
  }

  return (
    <main className="login-page">
      <section className="login-showcase" aria-label="Giới thiệu hệ thống">
        <div className="login-showcase-grid" />
        <div className="login-orb login-orb-one" />
        <div className="login-orb login-orb-two" />
        <header className="login-brand-header">
          <div className="login-logo-mark" aria-hidden="true">
            <span>GP</span>
          </div>
          <div>
            <strong>GIA PHÚ</strong>
            <small>WATER DELIVERY</small>
          </div>
        </header>

        <div className="login-showcase-content">
          <div className="login-eyebrow">
            <CheckCircleFilled /> Hệ thống đang hoạt động
          </div>
          <Typography.Title>
            Quản lý giao nước.
            <br />
            <span>Gọn từng đơn.</span>
          </Typography.Title>
          <Typography.Paragraph>
            Một nơi để đội ngũ Gia Phú theo dõi bán hàng, kho, giao nhận và
            công nợ xuyên suốt.
          </Typography.Paragraph>

          <div className="login-benefits">
            <div>
              <ShoppingCartOutlined />
              <span>
                <strong>Đơn hàng</strong>
                <small>Rõ trạng thái</small>
              </span>
            </div>
            <div>
              <DatabaseOutlined />
              <span>
                <strong>Tồn kho</strong>
                <small>Cập nhật tức thời</small>
              </span>
            </div>
            <div>
              <CarOutlined />
              <span>
                <strong>Giao nhận</strong>
                <small>Đúng người, đúng đơn</small>
              </span>
            </div>
          </div>
        </div>

        <footer className="login-showcase-footer">
          <span>Quản lý tập trung</span>
          <i />
          <span>Vận hành minh bạch</span>
        </footer>
      </section>

      <section className="login-panel">
        <div className="login-form-wrap">
          <div className="login-mobile-brand" aria-label="Gia Phú Water Delivery">
            <span className="login-logo-mark">GP</span>
            <span>
              <strong>GIA PHÚ</strong>
              <small>WATER DELIVERY</small>
            </span>
          </div>

          <header className="login-form-header">
            <span className="login-form-kicker">CỔNG NHÂN VIÊN</span>
            <Typography.Title level={1}>Chào mừng trở lại</Typography.Title>
            <Typography.Paragraph>
              Đăng nhập để tiếp tục công việc của bạn hôm nay.
            </Typography.Paragraph>
          </header>

          <Form
            form={form}
            layout="vertical"
            size="large"
            className="login-form"
            requiredMark="optional"
            initialValues={{ remember: false }}
            onFinish={submit}
          >
            <Form.Item
              name="username"
              label="Tên đăng nhập"
              rules={[
                { required: true, message: 'Vui lòng nhập tên đăng nhập.' },
              ]}
            >
              <Input
                prefix={<UserOutlined />}
                placeholder="Nhập tên đăng nhập"
                autoComplete="username"
              />
            </Form.Item>
            <Form.Item
              name="password"
              label="Mật khẩu"
              rules={[{ required: true, message: 'Vui lòng nhập mật khẩu.' }]}
            >
              <Input.Password
                prefix={<LockOutlined />}
                placeholder="Nhập mật khẩu"
                autoComplete="current-password"
              />
            </Form.Item>
            <div className="login-form-options">
              <Form.Item name="remember" valuePropName="checked" noStyle>
                <Checkbox>Ghi nhớ tài khoản</Checkbox>
              </Form.Item>
              <span>Liên hệ quản trị viên nếu quên mật khẩu</span>
            </div>
            <Button
              type="primary"
              htmlType="submit"
              loading={submitting}
              block
              className="login-submit"
              icon={<ArrowRightOutlined />}
              iconPosition="end"
            >
              Đăng nhập
            </Button>
          </Form>

          <div className="login-demo">
            <div className="login-demo-heading">
              <span>Tài khoản trình diễn</span>
              <small>Chọn để điền nhanh</small>
            </div>
            <div className="login-demo-grid">
              {demoAccounts.map((account) => (
                <button
                  key={account.username}
                  type="button"
                  onClick={() =>
                    selectDemoAccount(account.username, account.password)
                  }
                >
                  <span>{account.label}</span>
                  <code>{account.username}</code>
                </button>
              ))}
            </div>
          </div>

          <footer className="login-form-footer">
            © 2026 Gia Phú Water Delivery
          </footer>
        </div>
      </section>
    </main>
  )
}
