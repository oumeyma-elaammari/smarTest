import type { CSSProperties } from 'react'
import { LogOut } from 'lucide-react'
import { useNavigate } from 'react-router-dom'
import useAuth from '../hooks/useAuth'
import { useMatchMedia } from '../hooks/useMatchMedia'

const sans = "'DM Sans', system-ui, sans-serif"
const serif = "'DM Serif Display', Georgia, serif"

type NavbarProps = {
    readonly authenticated?: boolean
}

export default function Navbar({ authenticated = true }: NavbarProps) {
    const navigate = useNavigate()
    const { nom, logout } = useAuth()
    const compact = useMatchMedia('(max-width: 520px)')

    const handleLogout = () => {
        try {
            logout()
        } catch {
            try {
                localStorage.clear()
            } catch {
                /* stockage indisponible */
            }
            globalThis.location.href = '/login'
        }
    }

    const shell: CSSProperties = {
        position: 'sticky',
        top: 0,
        zIndex: 40,
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'space-between',
        gap: 12,
        padding: compact ? '8px 12px' : '8px clamp(16px, 4vw, 25px)',
        fontFamily: sans,
        background: 'rgba(255,255,255,0.92)',
        backdropFilter: 'blur(10px)',
        borderBottom: '1px solid #e2e8f4',
        boxShadow: '0 1px 0 rgba(15, 30, 61, 0.04)',
        boxSizing: 'border-box',
        width: '100%',
    }

    const logoBtn: CSSProperties = {
        border: 'none',
        background: 'none',
        cursor: 'pointer',
        padding: 0,
        fontFamily: serif,
        fontWeight: 400,
        fontSize: '1.25rem',
        letterSpacing: '0.05em',
        color: '#0f1e3d',
        lineHeight: 1,
    }

    /** Hauteur réduite tout en restant utilisable au clic */
    const btnSm: CSSProperties = {
        height: 30,
        padding: '0 14px',
        fontFamily: sans,
        fontSize: 11,
        fontWeight: 400,
        borderRadius: 7,
        cursor: 'pointer',
    }

    if (authenticated) {
        return (
            <nav style={shell} aria-label="Navigation">
                <button type="button" onClick={() => navigate('/dashboard')} style={logoBtn}>
                    Smar<span style={{ color: '#4f8ef7' }}>Test</span>
                </button>

                <div
                    style={{
                        display: 'flex',
                        alignItems: 'center',
                        gap: 8,
                        flexWrap: 'wrap',
                        justifyContent: 'flex-end',
                    }}
                >
                    <div
                        style={{
                            display: 'flex',
                            alignItems: 'center',
                            gap: 6,
                            padding: compact ? '2px 6px' : '2px 8px',
                            borderRadius: 999,
                            background: '#f4f7fc',
                            border: '1px solid #e2e8f4',
                            maxWidth: compact ? 'min(100%, 160px)' : 'min(100%, 240px)',
                            minWidth: 0,
                        }}
                    >
                        <span
                            style={{
                                width: 5,
                                height: 5,
                                borderRadius: '50%',
                                background: '#22c55e',
                                flexShrink: 0,
                            }}
                        />
                        {!compact ? (
                            <span style={{ fontSize: 11, color: '#64748b', fontWeight: 500, flexShrink: 0 }}>
                                Connecté
                            </span>
                        ) : null}
                        <span
                            style={{
                                fontSize: 11,
                                color: '#0f1e3d',
                                fontWeight: 600,
                                overflow: 'hidden',
                                textOverflow: 'ellipsis',
                                whiteSpace: 'nowrap',
                            }}
                            title={nom || undefined}
                        >
                            {nom}
                        </span>
                    </div>

                    <button
                        type="button"
                        onClick={handleLogout}
                        aria-label="Se déconnecter"
                        style={{
                            ...btnSm,
                            display: 'inline-flex',
                            alignItems: 'center',
                            gap: 5,
                            padding: compact ? '0 8px' : '0 10px',
                            color: '#64748b',
                            background: '#fff',
                            border: '1px solid #e2e8f4',
                            flexShrink: 0,
                        }}
                    >
                        <LogOut size={14} strokeWidth={2} />
                        {compact ? null : 'Déconnexion'}
                    </button>
                </div>
            </nav>
        )
    }

    return (
        <nav style={shell} aria-label="Navigation principale">
            <button type="button" onClick={() => navigate('/')} style={logoBtn}>
                Smar<span style={{ color: '#4f8ef7' }}>Test</span>
            </button>
            <div style={{ display: 'flex', alignItems: 'center', gap: 8, flexShrink: 0 }}>
                <button
                    type="button"
                    onClick={() => navigate('/login')}
                    style={{
                        ...btnSm,
                        color: '#1a2e5a',
                        background: '#fff',
                        border: '1px solid #d4dce8',
                    }}
                >
                    Connexion
                </button>
                <button
                    type="button"
                    onClick={() => navigate('/register')}
                    style={{
                        ...btnSm,
                        color: '#fff',
                        background: '#0f1e3d',
                        border: 'none',
                        boxShadow: '0 1px 4px rgba(15, 30, 61, 0.12)',
                    }}
                >
                    Inscription
                </button>
            </div>
        </nav>
    )
}
