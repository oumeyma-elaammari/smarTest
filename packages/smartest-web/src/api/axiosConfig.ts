import axios, { AxiosError, type InternalAxiosRequestConfig } from 'axios'
import toast from 'react-hot-toast'
import { getUserErrorMessage } from '../utils/userErrorMessage'

const api = axios.create({
    baseURL: 'http://localhost:8081',
    headers: { 'Content-Type': 'application/json' },
    timeout: 15_000,
})

api.interceptors.request.use((config: InternalAxiosRequestConfig) => {
    try {
        const token = localStorage.getItem('token')
        if (token) {
            config.headers.Authorization = `Bearer ${token}`
        }
    } catch {
        /* stockage indisponible */
    }
    return config
})

api.interceptors.response.use(
    (response) => response,
    (error: AxiosError | unknown) => {
        const ax = axios.isAxiosError(error) ? error : null
        const status = ax?.response?.status
        const url = ax?.config?.url ?? ''

        const isAuthRoute =
            url.includes('/auth/login') ||
            url.includes('/auth/register') ||
            url.includes('/auth/forgot-password') ||
            url.includes('/auth/reset-password') ||
            url.includes('/auth/verify-email')

        if (status === 429) {
            toast.error(getUserErrorMessage(error, 'Trop de requetes. Reessayez dans quelques instants.'))
        }

        if (status === 401 && !isAuthRoute) {
            try {
                localStorage.clear()
            } catch {
                /* quota / navigation privée */
            }
            window.location.href = '/login'
        }

        return Promise.reject(error)
    },
)

export default api
