import { useMemo, useState } from 'react'
import {
  DownOutlined,
  LogoutOutlined,
  MenuOutlined,
  UserOutlined,
} from '@ant-design/icons'
import {
  Avatar,
  Button,
  Drawer,
  Dropdown,
  Grid,
  Layout,
  Menu,
  Space,
  Typography,
  type MenuProps,
} from 'antd'
import { Outlet, useLocation, useNavigate } from 'react-router-dom'
import { useAuth } from '../contexts/AuthContext'
import { MENU_ROUTES } from '../routes/routeConfig'
import { ROLE_LABELS } from '../utils/constants'

const { Header, Sider, Content } = Layout

export default function AppLayout() {
  const { user, logout } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()
  const screens = Grid.useBreakpoint()
  const isDesktop = Boolean(screens.lg)
  const [collapsed, setCollapsed] = useState(false)
  const [drawerOpen, setDrawerOpen] = useState(false)

  const menuItems = useMemo(
    () =>
      MENU_ROUTES.filter(
        (route) => user && route.roles.includes(user.role),
      ).map((route) => ({
        key: route.path,
        label: route.menu.label,
        icon: route.menu.icon,
      })),
    [user],
  )

  const selectedKey =
    menuItems
      .map((item) => String(item.key))
      .find(
        (key) =>
          location.pathname === key || location.pathname.startsWith(`${key}/`),
      ) ?? ''

  const onMenuClick: MenuProps['onClick'] = ({ key }) => {
    navigate(key)
    setDrawerOpen(false)
  }

  const accountItems: MenuProps['items'] = [
    {
      key: 'role',
      disabled: true,
      label: user ? ROLE_LABELS[user.role] : '',
    },
    { type: 'divider' },
    {
      key: 'logout',
      icon: <LogoutOutlined />,
      label: 'Đăng xuất',
      danger: true,
      onClick: () => {
        logout()
        navigate('/login', { replace: true })
      },
    },
  ]

  const brand = (
    <button className="brand" type="button" onClick={() => navigate('/')}>
      <span className="brand-mark">GP</span>
      {(!collapsed || !isDesktop) && (
        <span className="brand-copy">
          <strong>GIA PHÚ</strong>
          <small>Water Delivery</small>
        </span>
      )}
    </button>
  )

  const menu = (
    <Menu
      mode="inline"
      theme="dark"
      selectedKeys={[selectedKey]}
      items={menuItems}
      onClick={onMenuClick}
      className="main-menu"
    />
  )

  return (
    <Layout className="app-shell">
      {isDesktop ? (
        <Sider
          width={252}
          collapsible
          collapsed={collapsed}
          onCollapse={setCollapsed}
          theme="dark"
          className="app-sider"
        >
          {brand}
          {menu}
        </Sider>
      ) : (
        <Drawer
          placement="left"
          width={280}
          open={drawerOpen}
          onClose={() => setDrawerOpen(false)}
          styles={{ body: { padding: 0, background: '#073b4c' } }}
          closable={false}
        >
          {brand}
          {menu}
        </Drawer>
      )}
      <Layout>
        <Header className="app-header">
          <div>
            {!isDesktop && (
              <Button
                type="text"
                aria-label="Mở menu"
                icon={<MenuOutlined />}
                onClick={() => setDrawerOpen(true)}
              />
            )}
          </div>
          <Dropdown menu={{ items: accountItems }} trigger={['click']}>
            <Button type="text" className="account-button">
              <Space>
                <Avatar icon={<UserOutlined />} className="account-avatar" />
                <span className="account-copy">
                  <Typography.Text strong>{user?.fullName}</Typography.Text>
                  <Typography.Text type="secondary">
                    {user ? ROLE_LABELS[user.role] : ''}
                  </Typography.Text>
                </span>
                <DownOutlined />
              </Space>
            </Button>
          </Dropdown>
        </Header>
        <Content className="app-content">
          <Outlet />
        </Content>
      </Layout>
    </Layout>
  )
}
