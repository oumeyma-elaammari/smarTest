import { LogOut } from 'lucide-react'
import useAuth from '../hooks/useAuth'

export default function Navbar() {
    const { nom, logout } = useAuth()

    return (
        <nav style={{
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'space-between',
            padding: '0.875rem 1.5rem',
            borderBottom: '1px solid #e5e7eb',
            backgroundColor: 'var(--card)',
        }}>
            {/* Logo */}
            <h1 style={{
                fontWeight: 700,
                fontSize: '1.5rem',
                letterSpacing: '0.1em',
                color: '#1a2e5a',
            }}>
                SmarTest
            </h1>

            {/* Droite */}
            <div style={{
                display: 'flex',
                alignItems: 'center',
                gap: '1rem',
            }}>
                {/* Indicateur connecté */}
                <div style={{
                    display: 'flex',
                    alignItems: 'center',
                    gap: '0.4rem',
                    fontSize: '0.875rem',
                    color: '#16a34a',
                }}>
                    <span style={{
                        width: '8px',
                        height: '8px',
                        borderRadius: '50%',
                        backgroundColor: '#16a34a',
                        display: 'inline-block',
                    }} />
                    Connecté
                </div>

                {/* Nom étudiant */}
                <span style={{
                    fontSize: '0.875rem',
                    color: 'var(--foreground)',
                    fontWeight: 500,
                }}>
                    {nom}
                </span>

                {/* Bouton déconnexion */}
                <button
                    onClick={logout}
                    aria-label="Se déconnecter"
                    style={{
                        display: 'flex',
                        alignItems: 'center',
                        justifyContent: 'center',
                        background: 'none',
                        border: 'none',
                        cursor: 'pointer',
                        color: 'var(--muted-foreground)',
                        padding: '0.25rem',
                        borderRadius: '0.25rem',
                        transition: 'color 0.2s',
                    }}
                    onMouseEnter={e => (e.currentTarget.style.color = '#dc2626')}
                    onMouseLeave={e => (e.currentTarget.style.color = 'var(--muted-foreground)')}
                >
                    <LogOut size={18} />
                </button>
            </div>
        </nav>
    )
}