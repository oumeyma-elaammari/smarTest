import { render, screen } from '@testing-library/react'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { beforeEach, describe, expect, it } from 'vitest'
import PrivateRoute from '../../src/components/PrivateRoute'
import useAuth from '../../src/hooks/useAuth'

describe('PrivateRoute', () => {
    beforeEach(() => {
        localStorage.clear()
        useAuth.setState({
            token: null,
            role: null,
            nom: null,
            email: null,
            isAuthenticated: false,
        })
    })

    function renderGuard(initialPath = '/dash') {
        return render(
            <MemoryRouter initialEntries={[initialPath]}>
                <Routes>
                    <Route
                        path="/dash"
                        element={
                            <PrivateRoute>
                                <div>Contenu protégé</div>
                            </PrivateRoute>
                        }
                    />
                    <Route path="/login" element={<div>Page login</div>} />
                </Routes>
            </MemoryRouter>,
        )
    }

    it('redirects unauthenticated users to login', () => {
        renderGuard()
        expect(screen.getByText(/Page login/i)).toBeInTheDocument()
    })

    it('renders children for authenticated ETUDIANT users', () => {
        useAuth.setState({
            token: 't',
            role: 'ETUDIANT',
            nom: 'E',
            email: 'e@ump.ac.ma',
            isAuthenticated: true,
        })
        renderGuard()
        expect(screen.getByText(/Contenu protégé/i)).toBeInTheDocument()
    })

    it('redirects authenticated PROF users to login', () => {
        useAuth.setState({
            token: 't',
            role: 'PROF',
            nom: 'P',
            email: 'p@ump.ac.ma',
            isAuthenticated: true,
        })
        renderGuard()
        expect(screen.getByText(/Page login/i)).toBeInTheDocument()
    })
})
