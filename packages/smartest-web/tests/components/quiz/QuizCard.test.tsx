import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import type { QuizWebItem } from '../../../src/api/quizApi'
import { QuizCard } from '../../../src/components/quiz/QuizCard'

function baseItem(over: Partial<QuizWebItem> = {}): QuizWebItem {
    return {
        id: 7,
        titre: 'Chapitre 3',
        professeurNom: 'Martin',
        nombreQuestions: 5,
        premiereTentative: false,
        meilleurScore: 72,
        ...over,
    }
}

describe('QuizCard', () => {
    it('renders title, professor and question count when data is complete', () => {
        render(<QuizCard item={baseItem()} accentBleu="#4f8ef7" onStart={vi.fn()} />)
        expect(screen.getByText(/Quiz — Chapitre 3/i)).toBeInTheDocument()
        expect(screen.getByText(/Martin/i)).toBeInTheDocument()
        expect(screen.getByText(/5 question\(s\)/i)).toBeInTheDocument()
    })

    it('renders new quiz hint when there is no score yet', () => {
        render(
            <QuizCard
                item={baseItem({ premiereTentative: true, meilleurScore: null })}
                accentBleu="#4f8ef7"
                onStart={vi.fn()}
            />,
        )
        expect(screen.getByText(/Nouveau quiz/i)).toBeInTheDocument()
    })

    it('formats and displays the best score when present', () => {
        render(<QuizCard item={baseItem({ meilleurScore: 81.3 })} accentBleu="#4f8ef7" onStart={vi.fn()} />)
        expect(screen.getByText(/81,3/i)).toBeInTheDocument()
    })

    it('calls onStart with the quiz id when clicking start', async () => {
        const user = userEvent.setup()
        const onStart = vi.fn()
        render(<QuizCard item={baseItem()} accentBleu="#4f8ef7" onStart={onStart} />)
        await user.click(screen.getByRole('button', { name: /Commencer/i }))
        expect(onStart).toHaveBeenCalledWith(7)
    })
})
