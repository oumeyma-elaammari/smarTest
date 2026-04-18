import axios from 'axios'

const api = axios.create({
    baseURL: 'http://localhost:8081',
    headers: { 'Content-Type': 'application/json' },
})

api.interceptors.request.use((config) => {
    const token = localStorage.getItem('token')
    if (token) config.headers.Authorization = `Bearer ${token}`
    return config
})

api.interceptors.response.use(
    (response) => response,
    (error) => {
   
        const url = error.config?.url || ''
        const isAuthRoute =
            url.includes('/auth/login') ||
            url.includes('/auth/register') ||
            url.includes('/auth/forgot-password') ||
            url.includes('/auth/reset-password') ||
            url.includes('/auth/verify-email')

        if (error.response?.status === 401 && !isAuthRoute) {
            localStorage.clear()
            window.location.href = '/login'
        }

        return Promise.reject(error)
    }
)

export default api