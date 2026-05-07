import { http, HttpResponse } from 'msw'

export const API_BASE = 'http://localhost:8081'

export const handlers = [
    http.post(`${API_BASE}/auth/login`, () =>
        HttpResponse.json({
            token: 'mock-token',
            role: 'ETUDIANT',
            nom: 'Étudiant',
            email: 'etudiant@ump.ac.ma',
        }),
    ),
    http.post(`${API_BASE}/auth/register/etudiant`, () => HttpResponse.json({})),
    http.get(`${API_BASE}/api/quizs/mes-publications-web`, () => HttpResponse.json([])),
]
