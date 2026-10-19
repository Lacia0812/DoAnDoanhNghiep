import { Navigate, Outlet, useLocation } from 'react-router-dom'
import { Spin } from 'antd'
import { useAuth } from '../contexts/AuthContext'
import type { Role } from '../types'

interface ProtectedRouteProps {
  roles?: Role[]
}

export default function ProtectedRoute({ roles }: ProtectedRouteProps) {
  const { isAuthenticated, loading, user } = useAuth()
  const location = useLocation()

  if (loading) {
    return (
      <div className="fullscreen-loader">
        <Spin size="large" tip="Đang xác thực..." />
      </div>
    )
  }

  if (!isAuthenticated) {
    return <Navigate to="/login" replace state={{ from: location }} />
  }

  if (roles?.length && user && !roles.includes(user.role)) {
    return <Navigate to="/403" replace />
  }

  return <Outlet />
}
