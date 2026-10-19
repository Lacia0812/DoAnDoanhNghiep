import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
  type PropsWithChildren,
} from 'react'
import { useNavigate } from 'react-router-dom'
import { authApi, TOKEN_STORAGE_KEY } from '../services/api'
import type { LoginRequest, User } from '../types'

interface AuthContextValue {
  user: User | null
  token: string | null
  loading: boolean
  isAuthenticated: boolean
  login: (credentials: LoginRequest) => Promise<User>
  logout: () => void
}

const AuthContext = createContext<AuthContextValue | undefined>(undefined)

export function AuthProvider({ children }: PropsWithChildren) {
  const navigate = useNavigate()
  const [token, setToken] = useState<string | null>(() =>
    localStorage.getItem(TOKEN_STORAGE_KEY),
  )
  const [user, setUser] = useState<User | null>(null)
  const [loading, setLoading] = useState(Boolean(token))

  const logout = useCallback(() => {
    localStorage.removeItem(TOKEN_STORAGE_KEY)
    setToken(null)
    setUser(null)
    setLoading(false)
  }, [])

  useEffect(() => {
    const handleUnauthorized = () => {
      logout()
      navigate('/login', { replace: true })
    }
    window.addEventListener('auth:unauthorized', handleUnauthorized)
    return () =>
      window.removeEventListener('auth:unauthorized', handleUnauthorized)
  }, [logout, navigate])

  useEffect(() => {
    if (!token) {
      setLoading(false)
      return
    }

    let active = true
    setLoading(true)
    authApi
      .profile()
      .then((profile) => {
        if (active) setUser(profile)
      })
      .catch(() => {
        if (active) logout()
      })
      .finally(() => {
        if (active) setLoading(false)
      })

    return () => {
      active = false
    }
  }, [logout, token])

  const login = useCallback(async (credentials: LoginRequest) => {
    const response = await authApi.login(credentials)
    localStorage.setItem(TOKEN_STORAGE_KEY, response.token)
    setToken(response.token)
    setUser(response.user)
    setLoading(false)
    return response.user
  }, [])

  const value = useMemo<AuthContextValue>(
    () => ({
      user,
      token,
      loading,
      isAuthenticated: Boolean(token && user),
      login,
      logout,
    }),
    [loading, login, logout, token, user],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export const useAuth = () => {
  const context = useContext(AuthContext)
  if (!context) {
    throw new Error('useAuth phải được sử dụng bên trong AuthProvider')
  }
  return context
}
