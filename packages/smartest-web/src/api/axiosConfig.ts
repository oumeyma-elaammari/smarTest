import axios, { type AxiosError, type InternalAxiosRequestConfig } from 'axios'
import toast from 'react-hot-toast'
import { resolveHttpApiBase } from '../config/runtimeBackend'
import useAuth from '../hooks/useAuth'
import { clearAuthStorage, readAccessToken } from '../utils/authSession'
import { getUserErrorMessage } from '../utils/userErrorMessage'

type RetryableConfig = InternalAxiosRequestConfig & { _retry?: boolean }

const api = axios.create({
    baseURL: resolveHttpApiBase(),
    headers: { 'Content-Type': 'application/json' },
    timeout: 15_000,
})

function resolveAccessToken(): string | null {
    return readAccessToken() ?? useAuth.getState().token
}

function syncApiDefaultAuth(token: string | null): void {
    if (token) {
        api.defaults.headers.common.Authorization = `Bearer ${token}`
    } else {
        delete api.defaults.headers.common.Authorization
    }
}

function clearSessionAndRedirectToLogin(): void {
    clearAuthStorage()
    syncApiDefaultAuth(null)
    useAuth.setState({
        token: null,
        role: null,
        nom: null,
        email: null,
        userId: null,
        isAuthenticated: false,
    })
    try {
        const path = `${globalThis.location.pathname}${globalThis.location.search}`
        const redirect =
            path && path !== '/login' && !path.startsWith('/login?')
                ? `?redirect=${encodeURIComponent(path)}`
                : ''
        globalThis.location.href = `/login${redirect}`
    } catch {
        globalThis.location.href = '/login'
    }
}

let refreshInFlight: Promise<string | null> | null = null

async function refreshAccessToken(): Promise<string | null> {
    const current = resolveAccessToken()
    if (!current) return null

    if (!refreshInFlight) {
        refreshInFlight = axios
            .post<{ token: string }>(`${resolveHttpApiBase()}/auth/refresh`, { token: current }, {
                headers: { 'Content-Type': 'application/json' },
                timeout: 15_000,
            })
            .then((res) => {
                const data = res.data
                const newToken = data?.token?.trim()
                if (!newToken) return null
                const state = useAuth.getState()
                useAuth.getState().login({
                    token: newToken,
                    role: data.role ?? state.role ?? '',
                    nom: data.nom ?? state.nom ?? '',
                    email: data.email ?? state.email ?? '',
                    userId: data.userId ?? (state.userId ? Number(state.userId) : undefined),
                })
                syncApiDefaultAuth(newToken)
                return newToken
            })
            .catch(() => null)
            .finally(() => {
                refreshInFlight = null
            })
    }
    return refreshInFlight
}

syncApiDefaultAuth(readAccessToken())

function setAuthHeader(config: InternalAxiosRequestConfig, token: string): void {
    const value = `Bearer ${token}`
    if (typeof config.headers.set === 'function') {
        config.headers.set('Authorization', value)
    } else {
        config.headers.Authorization = value
    }
}

api.interceptors.request.use((config: InternalAxiosRequestConfig) => {
    const token = resolveAccessToken()
    if (token) {
        setAuthHeader(config, token)
    }
    return config
})

api.interceptors.response.use(
    (response) => response,
    async (error: unknown) => {
        const ax: AxiosError | null = axios.isAxiosError(error) ? error : null
        const status = ax?.response?.status
        const originalRequest = ax?.config as RetryableConfig | undefined
        const url = originalRequest?.url ?? ''

        const isAuthRoute =
            url.includes('/auth/login') ||
            url.includes('/auth/register') ||
            url.includes('/auth/refresh') ||
            url.includes('/auth/forgot-password') ||
            url.includes('/auth/reset-password') ||
            url.includes('/auth/verify-email')

        if (status === 429) {
            toast.error(getUserErrorMessage(error, 'Trop de requetes. Reessayez dans quelques instants.'))
        }

        if (status === 401 && originalRequest && !isAuthRoute && !originalRequest._retry) {
            originalRequest._retry = true
            const newToken = await refreshAccessToken()
            if (newToken) {
                setAuthHeader(originalRequest, newToken)
                return api(originalRequest)
            }
            clearSessionAndRedirectToLogin()
        }

        if (status === 403 && ax) {
            const friendly = getUserErrorMessage(error, 'Accès refusé')
            if (friendly) {
                ax.message = friendly
            }
            const hasToken = !!resolveAccessToken()
            if (!hasToken) {
                clearSessionAndRedirectToLogin()
            } else {
                console.warn('403 sur', url, '—', friendly)
            }
        }

        return Promise.reject(error)
    },
)

export default api
