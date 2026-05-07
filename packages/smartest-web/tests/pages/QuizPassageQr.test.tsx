import { render, screen, waitFor } from '@testing-library/react'
import { http, HttpResponse } from 'msw'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import QuizPassageQr from '../../src/pages/QuizPassageQr'
import { API_BASE } from '../msw/handlers'
import { server } from '../msw/server'

const qrQuiz = {
    id: 1,
    titre: 'QR Quiz',
    nombreQuestions: 1,
    questions: [
        {
            id: 50,
            enonce: 'Question unique',
            reponses: [
                { id: 9, contenu: 'Oui' },
                { id: 10, contenu: 'Non' },
            ],
        },
    ],
}

function renderQr(quizId = '1') {
    return render(
        <MemoryRouter initialEntries={[`/quiz-qr/${quizId}`]}>
            <Routes>
                <Route path="/quiz-qr/:quizId" element={<QuizPassageQr />} />
            </Routes>
        </MemoryRouter>,
    )
}

describe('QuizPassageQr page', () => {
    beforeEach(() => {
        localStorage.clear()
        sessionStorage.clear()
        server.resetHandlers()
    })

    it('generates a stable participant id on first load', async () => {
        server.use(http.get(`${API_BASE}/api/quizs/1/passage-qr`, () => HttpResponse.json(qrQuiz)))
        renderQr()
        await screen.findByText(/Question unique/i)
        expect(localStorage.getItem('smartest.qr.participant')).toMatch(/^qr-/)
    })

    it('reuses participant id from localStorage', async () => {
        localStorage.setItem('smartest.qr.participant', 'qr-existing-id')
        server.use(http.get(`${API_BASE}/api/quizs/1/passage-qr`, () => HttpResponse.json(qrQuiz)))
        renderQr()
        await screen.findByText(/Question unique/i)
        expect(localStorage.getItem('smartest.qr.participant')).toBe('qr-existing-id')
    })

    it('shows a network error message when the quiz cannot load', async () => {
        server.use(http.get(`${API_BASE}/api/quizs/1/passage-qr`, () => HttpResponse.error()))
        renderQr()
        expect(await screen.findByText(/Impossible de contacter le serveur/i)).toBeInTheDocument()
    })

    it('does not crash when localStorage cannot store the participant id', async () => {
        const originalSetItem = Storage.prototype.setItem
        const spy = vi.spyOn(Storage.prototype, 'setItem').mockImplementation(function (this: Storage, key: string, val: string) {
            if (this === localStorage && key === 'smartest.qr.participant') {
                const err = new Error('QuotaExceededError')
                err.name = 'QuotaExceededError'
                throw err
            }
            return originalSetItem.apply(this, [key, val] as [string, string])
        })
        server.use(http.get(`${API_BASE}/api/quizs/1/passage-qr`, () => HttpResponse.json(qrQuiz)))
        renderQr()
        await waitFor(() => {
            expect(screen.getByText(/Question unique/i)).toBeInTheDocument()
        })
        spy.mockRestore()
    })
})
