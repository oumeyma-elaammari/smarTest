import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import { MemoryRouter } from 'react-router-dom'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import Login from '../../src/pages/Login'
import { API_BASE } from '../../src/mocks/handlers'
import { server } from '../../src/mocks/server'

const navigateMock = vi.fn()

vi.mock('react-router-dom', async (importOriginal) => {
    const mod = await importOriginal<typeof import('react-router-dom')>()
    return {
        ...mod,
        useNavigate: () => navigateMock,
    }
})

function renderLogin() {
    return render(
        <MemoryRouter>
            <Login />
        </MemoryRouter>,
    )
}

describe('Login page', () => {
    beforeEach(() => {
        navigateMock.mockClear()
        server.resetHandlers()
    })

    it('renders the login form without crashing', () => {
        renderLogin()
        expect(screen.getByRole('button', { name: /se connecter/i })).toBeInTheDocument()
    })

    it('shows a validation error when email format is invalid', async () => {
        const user = userEvent.setup()
        renderLogin()
        await user.type(screen.getByPlaceholderText(/email@exemple/i), 'bad')
        await user.type(screen.getByPlaceholderText(/• • • • • • • •/i), 'password12')
        await user.click(screen.getByRole('button', { name: /se connecter/i }))
        expect(await screen.findByText(/Format email invalide/i)).toBeInTheDocument()
    })

    it('shows a validation error when password is too short', async () => {
        const user = userEvent.setup()
        renderLogin()
        await user.type(screen.getByPlaceholderText(/email@exemple/i), 'a@ump.ac.ma')
        await user.type(screen.getByPlaceholderText(/• • • • • • • •/i), 'short')
        await user.click(screen.getByRole('button', { name: /se connecter/i }))
        expect(await screen.findByText(/Minimum 8 caractères/i)).toBeInTheDocument()
    })

    it('submits successfully and redirects to the dashboard', async () => {
        const user = userEvent.setup()
        server.use(
            http.post(`${API_BASE}/auth/login`, () =>
                HttpResponse.json({
                    token: 't1',
                    role: 'ETUDIANT',
                    nom: 'Test',
                    email: 'u@ump.ac.ma',
                }),
            ),
        )
        renderLogin()
        await user.type(screen.getByPlaceholderText(/email@exemple/i), 'u@ump.ac.ma')
        await user.type(screen.getByPlaceholderText(/• • • • • • • •/i), 'Password1!')
        await user.click(screen.getByRole('button', { name: /se connecter/i }))
        await waitFor(() => {
            expect(navigateMock).toHaveBeenCalledWith('/dashboard')
        })
    })

    it('shows incorrect credentials message on HTTP 401', async () => {
        const user = userEvent.setup()
        server.use(http.post(`${API_BASE}/auth/login`, () => new HttpResponse(null, { status: 401 })))
        renderLogin()
        await user.type(screen.getByPlaceholderText(/email@exemple/i), 'u@ump.ac.ma')
        await user.type(screen.getByPlaceholderText(/• • • • • • • •/i), 'Password1!')
        await user.click(screen.getByRole('button', { name: /se connecter/i }))
        expect(await screen.findByText(/Email ou mot de passe incorrect/i)).toBeInTheDocument()
    })

    it('redirects to email-sent on HTTP 403', async () => {
        const user = userEvent.setup()
        server.use(http.post(`${API_BASE}/auth/login`, () => new HttpResponse(null, { status: 403 })))
        renderLogin()
        await user.type(screen.getByPlaceholderText(/email@exemple/i), 'u@ump.ac.ma')
        await user.type(screen.getByPlaceholderText(/• • • • • • • •/i), 'Password1!')
        await user.click(screen.getByRole('button', { name: /se connecter/i }))
        await waitFor(() => {
            expect(navigateMock).toHaveBeenCalledWith('/email-sent', expect.any(Object))
        })
    })

    it('shows a network error message when the server is unreachable', async () => {
        const user = userEvent.setup()
        server.use(http.post(`${API_BASE}/auth/login`, () => HttpResponse.error()))
        renderLogin()
        await user.type(screen.getByPlaceholderText(/email@exemple/i), 'u@ump.ac.ma')
        await user.type(screen.getByPlaceholderText(/• • • • • • • •/i), 'Password1!')
        await user.click(screen.getByRole('button', { name: /se connecter/i }))
        expect(await screen.findByText(/Impossible de contacter le serveur/i)).toBeInTheDocument()
    })

    it('shows a loading state while submitting', async () => {
        const user = userEvent.setup()
        server.use(
            http.post(`${API_BASE}/auth/login`, async () => {
                await new Promise((r) => setTimeout(r, 80))
                return HttpResponse.json({
                    token: 't',
                    role: 'ETUDIANT',
                    nom: 'T',
                    email: 't@ump.ac.ma',
                })
            }),
        )
        renderLogin()
        await user.type(screen.getByPlaceholderText(/email@exemple/i), 't@ump.ac.ma')
        await user.type(screen.getByPlaceholderText(/• • • • • • • •/i), 'Password1!')
        await user.click(screen.getByRole('button', { name: /se connecter/i }))
        expect(screen.getByText(/Connexion en cours/i)).toBeInTheDocument()
    })
})
