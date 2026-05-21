const sans = "'DM Sans', system-ui, sans-serif"

export type ExamenCorrectionsProfPanelProps = {
    readonly examenId: number
    readonly accentBleu: string
}

/**
 * Les corrections et notes se gèrent uniquement depuis l'application bureau (Sessions examens).
 */
export function ExamenCorrectionsProfPanel({ accentBleu }: ExamenCorrectionsProfPanelProps) {
    return (
        <section
            style={{
                fontFamily: sans,
                border: `1px solid ${accentBleu}33`,
                borderRadius: 12,
                padding: '16px 18px',
                background: `${accentBleu}08`,
                marginBottom: 16,
                boxSizing: 'border-box',
            }}
        >
            <h3 style={{ margin: '0 0 8px', fontSize: 15, color: '#0f1e3d' }}>Notes et corrections</h3>
            <p style={{ margin: 0, fontSize: 14, color: '#475569', lineHeight: 1.55 }}>
                Consultez les réponses des étudiants et modifiez les notes depuis votre application bureau Smartest,
                dans <strong style={{ color: '#0f1e3d' }}>Sessions examens</strong>, une fois la session terminée.
            </p>
        </section>
    )
}
