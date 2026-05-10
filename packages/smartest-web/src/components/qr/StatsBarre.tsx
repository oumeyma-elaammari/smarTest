import type { QuizStatQuestion } from '../../types/quizLive'

type StatsBarreProps = {
    stat: QuizStatQuestion
    /** Nombre de participants distincts (plafond indicatif pour X/Y réponses). */
    nombreParticipants: number
}

/**
 * Barres réussite (vert) / échec (rouge) et progression des réponses.
 */
export function StatsBarre({ stat, nombreParticipants }: StatsBarreProps) {
    const cap = Math.max(nombreParticipants, 1)
    const pctReponses = Math.min(100, (stat.nombreReponses / cap) * 100)
    const pr = Math.max(0, Math.min(100, stat.pourcentageReussite))
    const pe = Math.max(0, Math.min(100, stat.pourcentageEchec))
    const vide = stat.nombreReponses === 0

    return (
        <section
            style={{
                marginBottom: 18,
                padding: '14px 16px',
                borderRadius: 12,
                background: '#fff',
                border: '1px solid #e2e8f0',
                boxShadow: '0 4px 18px rgba(15,23,42,0.06)',
            }}
        >
            <div style={{ display: 'flex', gap: 10, alignItems: 'baseline', marginBottom: 10 }}>
                <span
                    style={{
                        flexShrink: 0,
                        fontWeight: 700,
                        fontSize: 13,
                        color: '#2563eb',
                        background: '#eff6ff',
                        padding: '4px 10px',
                        borderRadius: 8,
                    }}
                >
                    Q{stat.numeroQuestion}
                </span>
                <p style={{ margin: 0, lineHeight: 1.45, fontWeight: 500, fontSize: 15 }}>
                    {stat.questionEnonce}
                </p>
            </div>

            <p style={{ margin: '0 0 6px', fontSize: 13, color: '#64748b' }}>
                Réponses reçues :{' '}
                <strong style={{ color: '#0f172a' }}>
                    {stat.nombreReponses} / {nombreParticipants || '—'}
                </strong>{' '}
                (participants)
            </p>
            <div
                style={{
                    margin: '0 0 10px',
                    height: 4,
                    borderRadius: 2,
                    background: '#f1f5f9',
                    overflow: 'hidden',
                }}
                title={`Participation : ${pctReponses.toFixed(0)} %`}
            >
               
            </div>

            {vide ? (
                <p style={{ margin: '0 0 12px', fontSize: 13, fontStyle: 'italic', color: '#94a3b8' }}>
                    En attente de réponses…
                </p>
            ) : null}

          

            {!vide ? (
                <>
                    <div style={{ display: 'flex', alignItems: 'center', gap: 8, marginBottom: 6 }}>
                        <span style={{ fontSize: 12, fontWeight: 600, color: '#15803d', width: 72 }}>
                            Réussite
                        </span>
                        <div
                            style={{
                                flex: 1,
                                height: 10,
                                borderRadius: 6,
                                background: '#ecfdf5',
                                overflow: 'hidden',
                            }}
                        >
                            <div
                                style={{
                                    height: '100%',
                                    width: `${pr}%`,
                                    background: '#22c55e',
                                    transition: 'width 0.35s ease',
                                }}
                            />
                        </div>
                        <span style={{ fontSize: 12, color: '#15803d', width: 40, textAlign: 'right' }}>
                            {pr.toFixed(0)}%
                        </span>
                    </div>

                    <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
                        <span style={{ fontSize: 12, fontWeight: 600, color: '#b91c1c', width: 72 }}>
                            Échec
                        </span>
                        <div
                            style={{
                                flex: 1,
                                height: 10,
                                borderRadius: 6,
                                background: '#fef2f2',
                                overflow: 'hidden',
                            }}
                        >
                            <div
                                style={{
                                    height: '100%',
                                    width: `${pe}%`,
                                    background: '#ef4444',
                                    transition: 'width 0.35s ease',
                                }}
                            />
                        </div>
                        <span style={{ fontSize: 12, color: '#b91c1c', width: 40, textAlign: 'right' }}>
                            {pe.toFixed(0)}%
                        </span>
                    </div>
                </>
            ) : null}
        </section>
    )
}
