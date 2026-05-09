import { Link } from 'react-router-dom'

const sans = "'DM Sans', system-ui, sans-serif"

export default function NotFound() {
    return (
        <main
            style={{
                minHeight: '100vh',
                display: 'flex',
                flexDirection: 'column',
                alignItems: 'center',
                justifyContent: 'center',
                padding: 24,
                fontFamily: sans,
                background: '#f8fafc',
                color: '#0f172a',
                textAlign: 'center',
                gap: 16,
            }}
        >
            <p style={{ margin: 0, fontSize: '4rem', fontWeight: 700, color: '#cbd5e1' }}>404</p>
            <h1 style={{ margin: 0, fontSize: '1.35rem' }}>Page introuvable</h1>
            <p style={{ margin: 0, color: '#64748b', maxWidth: 420, lineHeight: 1.5 }}>
                L’adresse demandée n’existe pas ou a été déplacée.
            </p>
            <Link
                to="/"
                style={{
                    marginTop: 8,
                    padding: '8px 16px',
                    borderRadius: 8,
                    background: '#0f1e3d',
                    color: '#fff',
                    textDecoration: 'none',
                    fontWeight: 600,
                }}
            >
                Retour à l’accueil
            </Link>
        </main>
    )
}
