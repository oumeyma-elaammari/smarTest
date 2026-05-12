import type { ReactNode } from 'react'
import type { QuizWebItem } from '../../api/quizApi'

const sans = "'DM Sans', system-ui, sans-serif"

function formatScorePourcent(v: number): string {
    const x = Math.round(v * 10) / 10
    return Number.isInteger(x) ? String(Math.round(v)) : x.toFixed(1).replace('.', ',')
}

type QuizCardProps = {
    readonly item: QuizWebItem
    readonly accentBleu: string
    readonly onStart: (id: number) => void
}

function meilleurScoreBloc(premiere: boolean, accentBleu: string, meilleurScore: number | null | undefined): ReactNode {
    if (premiere) {
        return (
            <span style={{ color: accentBleu, fontWeight: 600 }}>
                Nouveau quiz — Lancez le quand vous êtes prêt !
            </span>
        )
    }
    if (meilleurScore != null) {
        return (
            <>
                Meilleur score :{' '}
                <span style={{ color: accentBleu, fontWeight: 600 }}>
                    {formatScorePourcent(meilleurScore)} %
                </span>
            </>
        )
    }
    return 'Meilleur score : —'
}

export function QuizCard({ item: q, accentBleu, onStart }: QuizCardProps) {
    const premiere = q.premiereTentative === undefined || q.premiereTentative === true
    const titreBrut = (q.titre ?? '').trim() || 'Quiz'
    const titreAffiche = titreBrut.toLowerCase().startsWith('quiz') ? titreBrut : `Quiz — ${titreBrut}`
    return (
        <li
            style={{
                fontFamily: sans,
                border: '1px solid #e2e8f4',
                borderRadius: 12,
                padding: '18px 18px',
                background: '#fff',
                boxShadow: '0 1px 2px rgba(15, 30, 61, 0.04)',
                textAlign: 'left',
                width: '100%',
                minWidth: 0,
                boxSizing: 'border-box',
                display: 'flex',
                flexDirection: 'column',
                alignItems: 'flex-start',
            }}
        >
            <div style={{ fontWeight: 700, color: '#0f1e3d', fontSize: '1.05rem', marginBottom: 8 }}>
                {titreAffiche}
            </div>
            <div
                style={{
                    fontSize: 13,
                    color: '#64748b',
                    display: 'flex',
                    flexWrap: 'wrap',
                    gap: '5px 8px',
                    justifyContent: 'flex-start',
                    alignItems: 'center',
                    marginBottom: 2,
                }}
            >
                {q.professeurNom ? (
                    <span style={{ fontWeight: 500 }}>
                        <span style={{ color: '#64748b' }}>Prof. </span>
                        <span style={{ fontWeight: 600 }}>{q.professeurNom}</span>
                    </span>
                ) : null}
                {q.professeurNom && q.nombreQuestions != null ? (
                    <span style={{ color: '#cbd5e1', userSelect: 'none' }} aria-hidden>
                        ·
                    </span>
                ) : null}
                {q.nombreQuestions != null ? <span>{q.nombreQuestions} question(s)</span> : null}
            </div>
            <p
                style={{
                    margin: '10px 0 0',
                    fontSize: 13,
                    color: '#94a3b8',
                    lineHeight: 1.5,
                    width: '100%',
                    textAlign: 'left',
                }}
            >
                {meilleurScoreBloc(premiere, accentBleu, q.meilleurScore ?? null)}
            </p>
            <button
                type="button"
                onClick={() => onStart(q.id)}
                style={{
                    marginTop: 12,
                    alignSelf: 'flex-start',
                    padding: '4px 7px',
                    borderRadius: 5,
                    border: 'none',
                    cursor: 'pointer',
                    background: '#0f1e3d',
                    color: '#fff',
                    fontWeight: 400,
                    fontSize: 13,
                    fontFamily: sans,
                    width: '30%',
                }}
            >
                Commencer
            </button>
        </li>
    )
}
