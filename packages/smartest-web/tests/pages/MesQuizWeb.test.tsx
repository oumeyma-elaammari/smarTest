import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import { MemoryRouter } from 'react-router-dom'
import { describe, expect, it, vi, beforeEach } from 'vitest'
import MesQuizWeb from '../../src/pages/MesQuizWeb'
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

function quizPayload(count: number, titrePrefix = 'Quiz') {
    return Array.from({ length: count }, (_, i) => ({
        id: i + 1,
        titre: `${titrePrefix} ${i + 1}`,
        duree: 15,
        professeurNom: 'Prof',
        nombreQuestions: 3,
        premiereTentative: true,
        meilleurScore: null as number | null,
    }))
}

describe('MesQuizWeb page', () => {
    beforeEach(() => {
        navigateMock.mockClear()
        server.resetHandlers()
    })

    it('shows a spinner while loading', async () => {
        server.use(
            http.get(`${API_BASE}/api/quizs/mes-publications-web`, async () => {
                await new Promise((r) => setTimeout(r, 60))
                return HttpResponse.json([])
            }),
        )
        render(
            <MemoryRouter>
                <MesQuizWeb />
            </MemoryRouter>,
        )
        expect(screen.getByText(/Chargement/i)).toBeInTheDocument()
        await waitFor(() => expect(screen.queryByText(/Chargement/i)).not.toBeInTheDocument())
    })

    it('renders QuizCards after a successful fetch', async () => {
        server.use(http.get(`${API_BASE}/api/quizs/mes-publications-web`, () => HttpResponse.json(quizPayload(2))))
        render(
            <MemoryRouter>
                <MesQuizWeb />
            </MemoryRouter>,
        )
        expect(await screen.findByText('Quiz 1')).toBeInTheDocument()
        expect(screen.getAllByRole('button', { name: /Commencer/i })).toHaveLength(2)
    })

    it('shows an error message when the network fails', async () => {
        server.use(http.get(`${API_BASE}/api/quizs/mes-publications-web`, () => HttpResponse.error()))
        render(
            <MemoryRouter>
                <MesQuizWeb />
            </MemoryRouter>,
        )
        expect(await screen.findByText(/Impossible de charger vos quiz/i)).toBeInTheDocument()
    })

    it('shows Non autorisé on HTTP 401', async () => {
        server.use(
            http.get(`${API_BASE}/api/quizs/mes-publications-web`, () => new HttpResponse(null, { status: 401 })),
        )
        render(
            <MemoryRouter>
                <MesQuizWeb />
            </MemoryRouter>,
        )
        expect(await screen.findByText(/Non autorisé/i)).toBeInTheDocument()
    })

    it('filters quizzes when searching', async () => {
        const user = userEvent.setup()
        server.use(
            http.get(`${API_BASE}/api/quizs/mes-publications-web`, () =>
                HttpResponse.json([
                    ...quizPayload(1, 'Algèbre'),
                    ...[{ id: 2, titre: 'Bio 2', duree: 10, premiereTentative: true, meilleurScore: null }],
                ]),
            ),
        )
        render(
            <MemoryRouter>
                <MesQuizWeb />
            </MemoryRouter>,
        )
        await screen.findByText(/Quiz — Algèbre 1/i)
        const search = screen.getByRole('searchbox', { name: /Rechercher un quiz/i })
        await user.type(search, 'Bio')
        expect(screen.queryByText(/Algèbre/i)).not.toBeInTheDocument()
        expect(screen.getByText(/Quiz — Bio 2/i)).toBeInTheDocument()
    })

    it('paginates with previous and next buttons', async () => {
        const user = userEvent.setup()
        server.use(
            http.get(`${API_BASE}/api/quizs/mes-publications-web`, () => HttpResponse.json(quizPayload(11))),
        )
        render(
            <MemoryRouter>
                <MesQuizWeb />
            </MemoryRouter>,
        )
        await screen.findByText(/^Quiz 1$/i)
        expect(screen.queryByText(/^Quiz 11$/i)).not.toBeInTheDocument()
        await user.click(screen.getByRole('button', { name: /Suivant/i }))
        expect(await screen.findByText(/^Quiz 11$/i)).toBeInTheDocument()
        await user.click(screen.getByRole('button', { name: /Précédent/i }))
        expect(screen.getByText(/^Quiz 1$/i)).toBeInTheDocument()
    })

    it('does not update state after unmount when fetch is slow', async () => {
        server.use(
            http.get(`${API_BASE}/api/quizs/mes-publications-web`, async () => {
                await new Promise((r) => setTimeout(r, 100))
                return HttpResponse.json(quizPayload(1))
            }),
        )
        const { unmount } = render(
            <MemoryRouter>
                <MesQuizWeb />
            </MemoryRouter>,
        )
        unmount()
        await new Promise((r) => setTimeout(r, 150))
    })

    it('navigates to quiz route when starting a quiz', async () => {
        const user = userEvent.setup()
        server.use(http.get(`${API_BASE}/api/quizs/mes-publications-web`, () => HttpResponse.json(quizPayload(1))))
        render(
            <MemoryRouter>
                <MesQuizWeb />
            </MemoryRouter>,
        )
        const [firstStartButton] = await screen.findAllByRole('button', { name: /Commencer/i })
        await user.click(firstStartButton)
        expect(navigateMock).toHaveBeenCalledWith('/quiz/1')
    })
})
