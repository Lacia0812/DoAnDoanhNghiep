import { useEffect, useState, type FormEvent } from 'react'
import { Banner } from '@astryxdesign/core/Banner'
import { Button } from '@astryxdesign/core/Button'
import { CheckboxInput } from '@astryxdesign/core/CheckboxInput'
import { Grid } from '@astryxdesign/core/Grid'
import { Heading } from '@astryxdesign/core/Heading'
import { HStack } from '@astryxdesign/core/HStack'
import { Icon } from '@astryxdesign/core/Icon'
import { Text } from '@astryxdesign/core/Text'
import { TextInput } from '@astryxdesign/core/TextInput'
import { Theme } from '@astryxdesign/core/theme'
import { VStack } from '@astryxdesign/core/VStack'
import {
  ArrowRightIcon,
  CheckCircleIcon,
  CircleStackIcon,
  CubeIcon,
  LockClosedIcon,
  ShoppingBagIcon,
  TruckIcon,
  UserIcon,
} from '@heroicons/react/24/outline'
import { Navigate, useLocation, useNavigate } from 'react-router-dom'
import { defaultThemeSlug, themes } from '../astryx-themes'
import { useAuth } from '../contexts/AuthContext'
import { getApiErrorMessage } from '../services/api'
import { ROLE_HOME } from '../utils/constants'

const demoAccounts = [
  { label: 'Quản trị', username: 'admin', password: 'Admin@123' },
  { label: 'Bán hàng', username: 'sales', password: 'Sales@123' },
  { label: 'Kho', username: 'warehouse', password: 'Warehouse@123' },
  { label: 'Giao hàng', username: 'delivery', password: 'Delivery@123' },
]

const operatingHighlights = [
  {
    icon: ShoppingBagIcon,
    title: 'Đơn hàng',
    description: 'Theo dõi xuyên suốt',
  },
  {
    icon: CircleStackIcon,
    title: 'Tồn kho',
    description: 'Số liệu tức thời',
  },
  {
    icon: TruckIcon,
    title: 'Giao nhận',
    description: 'Đúng người, đúng đơn',
  },
]

export default function LoginPage() {
  const { login, isAuthenticated, user, loading } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()
  const [username, setUsername] = useState('')
  const [password, setPassword] = useState('')
  const [remember, setRemember] = useState(false)
  const [submitting, setSubmitting] = useState(false)
  const [hasSubmitted, setHasSubmitted] = useState(false)
  const [errorMessage, setErrorMessage] = useState<string | null>(null)

  useEffect(() => {
    const remembered = localStorage.getItem('gia-phu-remembered-username')
    if (remembered) {
      setUsername(remembered)
      setRemember(true)
    }
  }, [])

  if (!loading && isAuthenticated && user) {
    return <Navigate to={ROLE_HOME[user.role]} replace />
  }

  const submit = async (event?: FormEvent<HTMLFormElement>) => {
    event?.preventDefault()
    setHasSubmitted(true)
    setErrorMessage(null)

    const normalizedUsername = username.trim()
    if (!normalizedUsername || !password) return

    setSubmitting(true)
    try {
      const loggedInUser = await login({
        username: normalizedUsername,
        password,
      })

      if (remember) {
        localStorage.setItem('gia-phu-remembered-username', normalizedUsername)
      } else {
        localStorage.removeItem('gia-phu-remembered-username')
      }

      const requestedPath = (
        location.state as { from?: { pathname?: string } } | null
      )?.from?.pathname
      navigate(requestedPath || ROLE_HOME[loggedInUser.role], { replace: true })
    } catch (error) {
      setErrorMessage(
        getApiErrorMessage(
          error,
          'Tên đăng nhập hoặc mật khẩu không chính xác.',
        ),
      )
    } finally {
      setSubmitting(false)
    }
  }

  const selectDemoAccount = (account: (typeof demoAccounts)[number]) => {
    setUsername(account.username)
    setPassword(account.password)
    setErrorMessage(null)
    setHasSubmitted(false)
  }

  return (
    <Theme theme={themes[defaultThemeSlug]}>
      <main className="gp-login-page">
        <Grid
          columns={{ minWidth: 360, max: 2, repeat: 'fit' }}
          gap={0}
          minHeight="100vh"
          className="gp-login-grid"
        >
          <section
            className="gp-login-story"
            aria-label="Giới thiệu hệ thống Gia Phú"
          >
            <VStack
              height="100%"
              gap={8}
              padding={8}
              className="gp-login-story-stack"
            >
              <header className="gp-login-brand">
                <HStack gap={3} vAlign="center">
                  <HStack
                    className="gp-login-logo"
                    hAlign="center"
                    vAlign="center"
                    aria-hidden="true"
                  >
                    <Icon icon={CubeIcon} size="lg" />
                  </HStack>
                  <VStack gap={0}>
                    <Text type="large" weight="bold" color="inherit">
                      GIA PHÚ
                    </Text>
                    <Text type="supporting" color="inherit">
                      WATER DELIVERY
                    </Text>
                  </VStack>
                </HStack>
              </header>

              <VStack
                gap={6}
                width="100%"
                maxWidth="42rem"
                className="gp-login-story-content"
              >
                <HStack gap={2} vAlign="center" className="gp-login-status">
                  <Icon icon={CheckCircleIcon} color="success" size="sm" />
                  <Text type="supporting" weight="semibold" color="inherit">
                    Hệ thống đang hoạt động
                  </Text>
                </HStack>

                <VStack gap={3}>
                  <Heading
                    level={1}
                    type="display-1"
                    color="inherit"
                    className="gp-login-story-title"
                  >
                    Vận hành giao nước, liền mạch từng đơn.
                  </Heading>
                  <Text
                    type="large"
                    color="inherit"
                    className="gp-login-story-description"
                  >
                    Một không gian chung để đội ngũ Gia Phú quản lý bán hàng,
                    kho, giao nhận và công nợ rõ ràng mỗi ngày.
                  </Text>
                </VStack>

                <Grid
                  columns={{ minWidth: 150, max: 3, repeat: 'fit' }}
                  gap={3}
                  className="gp-login-highlights"
                >
                  {operatingHighlights.map((item) => (
                    <HStack
                      key={item.title}
                      gap={3}
                      vAlign="center"
                      className="gp-login-highlight"
                    >
                      <Icon icon={item.icon} color="inherit" size="lg" />
                      <VStack gap={0}>
                        <Text weight="bold" color="inherit">
                          {item.title}
                        </Text>
                        <Text type="supporting" color="inherit">
                          {item.description}
                        </Text>
                      </VStack>
                    </HStack>
                  ))}
                </Grid>
              </VStack>

              <footer className="gp-login-story-footer">
                <Text type="supporting" color="inherit">
                  Quản lý tập trung · Vận hành minh bạch · Phục vụ tận tâm
                </Text>
              </footer>
            </VStack>
          </section>

          <section className="gp-login-auth" aria-label="Đăng nhập nhân viên">
            <VStack
              gap={6}
              width="100%"
              maxWidth="28rem"
              className="gp-login-auth-content"
            >
              <header>
                <VStack gap={2}>
                  <Text
                    type="supporting"
                    weight="bold"
                    color="accent"
                    className="gp-login-kicker"
                  >
                    CỔNG NHÂN VIÊN
                  </Text>
                  <Heading level={1} type="display-2">
                    Chào mừng trở lại
                  </Heading>
                  <Text color="secondary">
                    Đăng nhập để tiếp tục công việc của bạn hôm nay.
                  </Text>
                </VStack>
              </header>

              {errorMessage ? (
                <Banner
                  status="error"
                  title="Đăng nhập chưa thành công"
                  description={errorMessage}
                  container="card"
                />
              ) : null}

              <form onSubmit={submit} noValidate>
                <VStack gap={4}>
                  <TextInput
                    label="Tên đăng nhập"
                    value={username}
                    onChange={(value) => {
                      setUsername(value)
                      setErrorMessage(null)
                    }}
                    placeholder="Nhập tên đăng nhập"
                    startIcon={<Icon icon={UserIcon} />}
                    autoComplete="username"
                    hasAutoFocus
                    aria-required="true"
                    size="lg"
                    width="100%"
                    status={
                      hasSubmitted && !username.trim()
                        ? {
                            type: 'error',
                            message: 'Vui lòng nhập tên đăng nhập.',
                          }
                        : undefined
                    }
                  />

                  <TextInput
                    label="Mật khẩu"
                    type="password"
                    value={password}
                    onChange={(value) => {
                      setPassword(value)
                      setErrorMessage(null)
                    }}
                    placeholder="Nhập mật khẩu"
                    startIcon={<Icon icon={LockClosedIcon} />}
                    autoComplete="current-password"
                    aria-required="true"
                    size="lg"
                    width="100%"
                    status={
                      hasSubmitted && !password
                        ? {
                            type: 'error',
                            message: 'Vui lòng nhập mật khẩu.',
                          }
                        : undefined
                    }
                  />

                  <HStack
                    gap={3}
                    hAlign="between"
                    vAlign="center"
                    wrap="wrap"
                    className="gp-login-options"
                  >
                    <CheckboxInput
                      label="Ghi nhớ tài khoản"
                      value={remember}
                      onChange={setRemember}
                      size="sm"
                    />
                    <Text type="supporting" color="secondary">
                      Quên mật khẩu? Liên hệ quản trị viên
                    </Text>
                  </HStack>

                  <Button
                    label="Đăng nhập"
                    type="submit"
                    variant="primary"
                    size="lg"
                    width="100%"
                    isLoading={submitting}
                    endContent={<Icon icon={ArrowRightIcon} />}
                  />
                </VStack>
              </form>

              <section className="gp-login-demo" aria-label="Tài khoản trình diễn">
                <VStack gap={3}>
                  <HStack
                    gap={2}
                    hAlign="between"
                    vAlign="center"
                    className="gp-login-demo-heading"
                  >
                    <Text type="label" weight="semibold">
                      Tài khoản trình diễn
                    </Text>
                    <Text type="supporting" color="secondary">
                      Chọn để điền nhanh
                    </Text>
                  </HStack>
                  <Grid
                    columns={{ minWidth: 145, max: 2, repeat: 'fit' }}
                    gap={2}
                    className="gp-login-demo-grid"
                  >
                    {demoAccounts.map((account) => (
                      <Button
                        key={account.username}
                        label={`${account.label} · ${account.username}`}
                        variant="secondary"
                        size="sm"
                        width="100%"
                        onClick={() => selectDemoAccount(account)}
                      />
                    ))}
                  </Grid>
                </VStack>
              </section>

              <footer className="gp-login-auth-footer">
                <Text type="supporting" color="secondary" justify="center">
                  © 2026 Gia Phú Water Delivery
                </Text>
              </footer>
            </VStack>
          </section>
        </Grid>
      </main>
    </Theme>
  )
}
