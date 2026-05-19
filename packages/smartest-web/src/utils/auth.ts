import { TOKEN_KEY } from './authSession'

const USER_KEY = 'smartest_user'

/** @deprecated Préférer useAuth + clé `token` ; conserve la compatibilité lecture. */
export const saveSession = (token: string, user: object) => {
  localStorage.setItem(TOKEN_KEY, token)
  localStorage.setItem(USER_KEY, JSON.stringify(user))
}

export const getToken = (): string | null => {
  return localStorage.getItem(TOKEN_KEY) ?? localStorage.getItem('smartest_token')
}

export const getUser = () => {
  const u = localStorage.getItem(USER_KEY)
  return u ? JSON.parse(u) : null
}

export const clearSession = () => {
  localStorage.removeItem(TOKEN_KEY)
  localStorage.removeItem('smartest_token')
  localStorage.removeItem(USER_KEY)
}

export const isTokenValid = (token: string): boolean => {
  try {
    const payload = JSON.parse(atob(token.split('.')[1]))
    return payload.exp * 1000 > Date.now()
  } catch {
    return false
  }
}
