import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import QuizPassageWeb, { getQuizWebDraftStorageKey } from '../../src/pages/QuizPassageWeb'
import { API_BASE } from '../msw/handlers'
import { server } from '../msw/server'

/** Ordre des questions déterministe (la page mélange via `crypto.getRandomValues`). */
vi.mock('../../src/utils/shuffle', () => ({
    shuffleCopy: <T,>(items: T[]) => [...items],
}))

const navigateMock = vi.fn()

vi.mock('react-router-dom', async (importOriginal) => {
    const mod = await importOriginal<typeof import('react-router-dom')>()
    return {
        ...mod,
        useNavigate: () => navigateMock,
    }
})

const quizBody = {
    id: 1,
    titre: 'Mon quiz',
    nombreQuestions: 2,
    questions: [
        {
            id: 10,
            enonce: 'Deux plus deux ?',
            reponses: [
                { id: 1, contenu: '3' },
                { id: 2, contenu: '4' },
            ],
        },
        {
            id: 11,
            enonce: 'Trois plus trois ?',
            reponses: [
                { id: 3, contenu: '5' },
                { id: 4, contenu: '6' },
            ],
        },
    ],
}

function renderAt(path: string) {
    return render(
        <MemoryRouter initialEntries={[path]}>
            <Routes>
                <Route path="/quiz/:quizId" element={<QuizPassageWeb />} />
            </Routes>
        </MemoryRouter>,
    )
}

describe('QuizPassageWeb page', () => {
    beforeEach(() => {
        sessionStorage.clear()
        navigateMock.mockClear()
        server.resetHandlers()
        server.use(
            http.post(`${API_BASE}/api/quizs/1/verifier-question-web`, () =>
                HttpResponse.json({
                    correcte: true,
                    reponseCorrecteId: 2,
                    reponseCorrecteContenu: '4',
                }),
            ),
            http.post(`${API_BASE}/api/quizs/1/soumettre-web`, () =>
                HttpResponse.json({
                    score: 100,
                    bonnesReponses: 2,
                    totalQuestions: 2,
                    estPremiereTentative: true,
                    corrections: [],
                }),
            ),
        )
    })

    it('shows an error when quiz id is not numeric before calling the API', async () => {
        renderAt('/quiz/abc')
        expect(await screen.findByText(/Quiz invalide/i)).toBeInTheDocument()
    })

    it('loads the quiz and displays questions', async () => {
        server.use(http.get(`${API_BASE}/api/quizs/1/passage-web`, () => HttpResponse.json(quizBody)))
        renderAt('/quiz/1')
        expect(await screen.findByText(/Deux plus deux/i)).toBeInTheDocument()
    })

    it('updates local selection when choosing an answer', async () => {
        const user = userEvent.setup()
        server.use(http.get(`${API_BASE}/api/quizs/1/passage-web`, () => HttpResponse.json(quizBody)))
        renderAt('/quiz/1')
        await screen.findByText(/Deux plus deux/i)
        await user.click(screen.getByLabelText('4'))
        const radio = screen.getByRole('radio', { name: /4/i }) as HTMLInputElement
        expect(radio.checked).toBe(true)
    })

    it('calls verify API and shows correct feedback', async () => {
        const user = userEvent.setup()
        server.use(http.get(`${API_BASE}/api/quizs/1/passage-web`, () => HttpResponse.json(quizBody)))
        renderAt('/quiz/1')
        await screen.findByText(/Deux plus deux/i)
        await user.click(screen.getByLabelText('4'))
        await user.click(screen.getByRole('button', { name: /Vérifier/i }))
        expect(await screen.findByText(/Réponse correcte/i)).toBeInTheDocument()
    })

    it('submits the quiz and shows the score', async () => {
        const user = userEvent.setup()
        server.use(http.get(`${API_BASE}/api/quizs/1/passage-web`, () => HttpResponse.json(quizBody)))
        renderAt('/quiz/1')
        await screen.findByText(/Deux plus deux/i)
        await user.click(screen.getByLabelText('4'))
        await user.click(screen.getByRole('button', { name: /Vérifier/i }))
        await user.click(screen.getByRole('button', { name: /Suivant/i }))
        await user.click(screen.getByLabelText('6'))
        await user.click(screen.getByRole('button', { name: /Vérifier/i }))
        await user.click(screen.getByRole('button', { name: /Soumettre/i }))
        await waitFor(
            () => {
                expect(screen.getByText(/Résultat du quiz/i)).toBeInTheDocument()
            },
            { timeout: 4000 },
        )
        expect(screen.getByText(/100\.00%/)).toBeInTheDocument()
    })

    it('shows quiz not found on HTTP 404', async () => {
        server.use(
            http.get(`${API_BASE}/api/quizs/1/passage-web`, () => new HttpResponse(null, { status: 404 })),
        )
        renderAt('/quiz/1')
        expect(await screen.findByText(/Quiz introuvable/i)).toBeInTheDocument()
    })

    it('shows access denied on HTTP 403', async () => {
        server.use(
            http.get(`${API_BASE}/api/quizs/1/passage-web`, () => new HttpResponse(null, { status: 403 })),
        )
        renderAt('/quiz/1')
        expect(await screen.findByText(/Accès refusé/i)).toBeInTheDocument()
    })

    it('restores draft state from sessionStorage', async () => {
        const user = userEvent.setup()
        server.use(http.get(`${API_BASE}/api/quizs/1/passage-web`, () => HttpResponse.json(quizBody)))
        sessionStorage.setItem(
            getQuizWebDraftStorageKey(1),
            JSON.stringify({
                choix: { 10: 2 },
                indexCourant: 1,
                verification: {
                    10: {
                        correcte: true,
                        reponseCorrecteId: 2,
                        reponseCorrecteContenu: '4',
                    },
                },
            }),
        )
        renderAt('/quiz/1')
        await screen.findByText(/Trois plus trois/i)
        await user.click(screen.getByLabelText('6'))
        await user.click(screen.getByRole('button', { name: /Vérifier/i }))
        expect(await screen.findByText(/Réponse correcte/i)).toBeInTheDocument()
    })

    it('clears corrupted sessionStorage draft without crashing', async () => {
        server.use(http.get(`${API_BASE}/api/quizs/1/passage-web`, () => HttpResponse.json(quizBody)))
        sessionStorage.setItem(getQuizWebDraftStorageKey(1), '{not-json')
        renderAt('/quiz/1')
        expect(await screen.findByText(/Deux plus deux/i)).toBeInTheDocument()
    })
})
