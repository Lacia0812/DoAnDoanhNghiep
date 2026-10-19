import { Button, Result } from 'antd'
import { useNavigate } from 'react-router-dom'
import { useAuth } from '../contexts/AuthContext'
import { ROLE_HOME } from '../utils/constants'

export default function ForbiddenPage() {
  const navigate = useNavigate()
  const { user } = useAuth()
  return (
    <Result
      status="403"
      title="403"
      subTitle="Bạn không có quyền truy cập trang này."
      extra={
        <Button
          type="primary"
          onClick={() => navigate(user ? ROLE_HOME[user.role] : '/login')}
        >
          Về trang chính
        </Button>
      }
    />
  )
}
