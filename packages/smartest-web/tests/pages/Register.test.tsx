import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import { MemoryRouter } from 'react-router-dom'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import Register from '../../src/pages/Register'
import { API_BASE } from '../msw/handlers'
import { server } from '../msw/server'

const navigateMock = vi.fn()

vi.mock('react-router-dom', async (importOriginal) => {
    const mod = await importOriginal<typeof import('react-router-dom')>()
    return {
        ...mod,
        useNavigate: () => navigateMock,
    }
})

function renderRegister() {
    return render(
        <MemoryRouter>
            <Register />
        </MemoryRouter>,
    )
}

function passwordInputs() {
    const inputs = screen.getAllByPlaceholderText(/• • • • • • • •/i)
    return {
        password: inputs[0],
        confirmPassword: inputs[1],
    }
}

describe('Register page', () => {
    beforeEach(() => {
        navigateMock.mockClear()
        server.resetHandlers()
    })

    it('rejects email outside @ump.ac.ma domain', async () => {
        const user = userEvent.setup()
        renderRegister()
        await user.type(screen.getByPlaceholderText(/Nom et prénom/i), 'Jean Dupont')
        await user.type(screen.getByPlaceholderText(/prenom\.nom@ump\.ac\.ma/i), 'x@gmail.com')
        const { password, confirmPassword } = passwordInputs()
        await user.type(password, 'Password1!')
        await user.type(confirmPassword, 'Password1!')
        await user.click(screen.getByRole('button', { name: "S'inscrire" }))
        expect(await screen.findByText(/Email académique @ump\.ac\.ma requis/i)).toBeInTheDocument()
    })

    it('shows password strength indicators for a weak password', async () => {
        const user = userEvent.setup()
        renderRegister()
        const pw = document.querySelector('input[name="password"]') as HTMLInputElement
        await user.type(pw, 'abc')
        expect(screen.getByText(/8 caractères/i)).toBeInTheDocument()
    })

    it('shows an error when passwords do not match', async () => {
        const user = userEvent.setup()
        renderRegister()
        await user.type(screen.getByPlaceholderText(/Nom et prénom/i), 'Jean Dupont')
        await user.type(screen.getByPlaceholderText(/prenom\.nom@ump\.ac\.ma/i), 'jean@ump.ac.ma')
        const { password, confirmPassword } = passwordInputs()
        await user.type(password, 'Password1!')
        await user.type(confirmPassword, 'Password2!')
        await user.click(screen.getByRole('button', { name: "S'inscrire" }))
        expect(await screen.findByText(/Les mots de passe ne correspondent pas/i)).toBeInTheDocument()
    })

    it('redirects to email-sent after successful registration', async () => {
        const user = userEvent.setup()
        server.use(http.post(`${API_BASE}/auth/register/etudiant`, () => HttpResponse.json({})))
        renderRegister()
        await user.type(screen.getByPlaceholderText(/Nom et prénom/i), 'Jean Dupont')
        await user.type(screen.getByPlaceholderText(/prenom\.nom@ump\.ac\.ma/i), 'jean@ump.ac.ma')
        const { password, confirmPassword } = passwordInputs()
        await user.type(password, 'Password1!')
        await user.type(confirmPassword, 'Password1!')
        await user.click(screen.getByRole('button', { name: "S'inscrire" }))
        await waitFor(() => {
            expect(navigateMock).toHaveBeenCalledWith('/email-sent', expect.any(Object))
        })
    })

    it('shows email already used on HTTP 409', async () => {
        const user = userEvent.setup()
        server.use(http.post(`${API_BASE}/auth/register/etudiant`, () => new HttpResponse(null, { status: 409 })))
        renderRegister()
        await user.type(screen.getByPlaceholderText(/Nom et prénom/i), 'Jean Dupont')
        await user.type(screen.getByPlaceholderText(/prenom\.nom@ump\.ac\.ma/i), 'jean@ump.ac.ma')
        const { password, confirmPassword } = passwordInputs()
        await user.type(password, 'Password1!')
        await user.type(confirmPassword, 'Password1!')
        await user.click(screen.getByRole('button', { name: "S'inscrire" }))
        expect(await screen.findByText(/Email déjà utilisé/i)).toBeInTheDocument()
    })

    it('shows a network error when registration cannot reach the server', async () => {
        const user = userEvent.setup()
        server.use(http.post(`${API_BASE}/auth/register/etudiant`, () => HttpResponse.error()))
        renderRegister()
        await user.type(screen.getByPlaceholderText(/Nom et prénom/i), 'Jean Dupont')
        await user.type(screen.getByPlaceholderText(/prenom\.nom@ump\.ac\.ma/i), 'jean@ump.ac.ma')
        const { password, confirmPassword } = passwordInputs()
        await user.type(password, 'Password1!')
        await user.type(confirmPassword, 'Password1!')
        await user.click(screen.getByRole('button', { name: "S'inscrire" }))
        expect(await screen.findByText(/Impossible de contacter le serveur/i)).toBeInTheDocument()
    })
})
