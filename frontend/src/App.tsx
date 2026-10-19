import { Suspense } from 'react'
import { Navigate, Route, Routes } from 'react-router-dom'
import { Spin } from 'antd'
import AppLayout from './components/AppLayout'
import ProtectedRoute from './components/ProtectedRoute'
import { useAuth } from './contexts/AuthContext'
import ForbiddenPage from './pages/ForbiddenPage'
import LoginPage from './pages/LoginPage'
import NotFoundPage from './pages/NotFoundPage'
import { APP_ROUTES } from './routes/routeConfig'
import { ROLE_HOME } from './utils/constants'

function HomeRedirect() {
  const { user } = useAuth()
  return <Navigate to={user ? ROLE_HOME[user.role] : '/login'} replace />
}

export default function App() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route element={<ProtectedRoute />}>
        <Route element={<AppLayout />}>
          <Route index element={<HomeRedirect />} />
          {APP_ROUTES.map((route) => {
            const Page = route.component
            return (
              <Route
                key={route.path}
                element={<ProtectedRoute roles={route.roles} />}
              >
                <Route
                  path={route.path}
                  element={
                    <Suspense
                      fallback={
                        <div className="page-loader">
                          <Spin size="large" tip="Đang tải trang..." />
                        </div>
                      }
                    >
                      <Page />
                    </Suspense>
                  }
                />
              </Route>
            )
          })}
          <Route path="/403" element={<ForbiddenPage />} />
          <Route path="*" element={<NotFoundPage />} />
        </Route>
      </Route>
      <Route path="*" element={<Navigate to="/login" replace />} />
    </Routes>
  )
}
