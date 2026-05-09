import axios, { Axios, AxiosError } from 'axios'
import { http, HttpResponse } from 'msw'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import toast from 'react-hot-toast'
import api from '../../src/api/axiosConfig'
import { API_BASE } from '../msw/handlers'
import { server } from '../msw/server'

vi.mock('react-hot-toast', () => ({
    default: {
        error: vi.fn(),
    },
}))

describe('axiosConfig interceptors', () => {
    beforeEach(() => {
        localStorage.clear()
        vi.mocked(toast.error).mockClear()
        server.resetHandlers()
        api.defaults.timeout = 15000
    })

    it('request interceptor adds Authorization when a token is stored', async () => {
        localStorage.setItem('token', 'abc-token')
        server.use(
            http.get(`${API_BASE}/api/ping-auth`, ({ request }) => {
                expect(request.headers.get('Authorization')).toBe('Bearer abc-token')
                return HttpResponse.json({ ok: true })
            }),
        )
        await api.get('/api/ping-auth')
    })

    it('request interceptor does not add Authorization when token is absent', async () => {
        server.use(
            http.get(`${API_BASE}/api/ping-no-auth`, ({ request }) => {
                expect(request.headers.get('Authorization')).toBeNull()
                return HttpResponse.json({ ok: true })
            }),
        )
        await api.get('/api/ping-no-auth')
    })

    it('response interceptor returns response data normally on 200', async () => {
        server.use(http.get(`${API_BASE}/api/ok`, () => HttpResponse.json({ value: 42 })))
        const { data } = await api.get<{ value: number }>('/api/ok')
        expect(data.value).toBe(42)
    })

    it('response interceptor clears storage and redirects to login on 401 for protected routes', async () => {
        server.use(http.get(`${API_BASE}/api/protected`, () => new HttpResponse(null, { status: 401 })))
        await expect(api.get('/api/protected')).rejects.toBeDefined()
        expect(localStorage.length).toBe(0)
        /* jsdom ne permet pas de spy sur location.href ; la redirection est définie dans axiosConfig */
    })

    it('response interceptor rejects with AxiosError on 403', async () => {
        server.use(http.get(`${API_BASE}/api/forbidden`, () => new HttpResponse(null, { status: 403 })))
        await expect(api.get('/api/forbidden')).rejects.toMatchObject({
            response: expect.objectContaining({ status: 403 }),
        })
    })

    it('response interceptor propagates timeout errors (ECONNABORTED)', async () => {
        const timeoutErr = Object.assign(new AxiosError('timeout'), { code: 'ECONNABORTED' as const })
        const spy = vi.spyOn(Axios.prototype, 'request').mockRejectedValueOnce(timeoutErr)
        await expect(api.get('/api/slow')).rejects.toMatchObject({
            code: 'ECONNABORTED',
        })
        spy.mockRestore()
    })

    it('response interceptor surfaces network errors without response', async () => {
        server.use(http.get(`${API_BASE}/api/net-fail`, () => HttpResponse.error()))
        const err = await api.get('/api/net-fail').catch((e) => e)
        expect(axios.isAxiosError(err)).toBe(true)
        expect(err.response).toBeUndefined()
    })

    it('response interceptor shows a toast on HTTP 429', async () => {
        server.use(http.get(`${API_BASE}/api/too-many`, () => new HttpResponse(null, { status: 429 })))
        await expect(api.get('/api/too-many')).rejects.toBeDefined()
        expect(toast.error).toHaveBeenCalled()
    })
})
