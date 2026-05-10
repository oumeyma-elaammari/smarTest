import { QRCodeSVG } from 'qrcode.react'

type QrDisplayProps = {
    /** URL à encoder (ex. window.location.href). */
    value: string
    size?: number
    /** Libellé pour lecteurs d’écran */
    title?: string
}

/**
 * QR code de la session (lien courant), mobile-first.
 */
export function QrDisplay({ value, size = 220, title = 'Code QR de la session' }: QrDisplayProps) {
    return (
        <div
            style={{
                display: 'flex',
                justifyContent: 'center',
                padding: '1rem',
                borderRadius: 12,
                border: '1px solid #e2e8f0',
                background: '#fff',
                boxShadow: '0 4px 18px rgba(15,23,42,0.06)',
            }}
        >
            <QRCodeSVG value={value} size={size} level="M" title={title} />
        </div>
    )
}
