import { LogOut } from 'lucide-react'
import { useNavigate } from 'react-router-dom'
import useAuth from '../hooks/useAuth'

interface NavbarProps {
    /** true = mode authentifié (nom + déconnexion), false = mode public */
    authenticated?: boolean
}

export default function Navbar({ authenticated = false }: NavbarProps) {
    const { nom, logout } = useAuth()
    const navigate = useNavigate()

    return (
        <nav style={{
            background: '#fff',
            borderBottom: '1px solid #e2e8f4',
            padding: '0 48px',
            height: 52,
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'space-between',
            flexShrink: 0,
            position: 'sticky',
            top: 0,
            zIndex: 100,
        }}>
            {/* ── LOGO ── */}
            <button
                onClick={() => navigate(authenticated ? '/dashboard' : '/')}
                style={{
                    display: 'flex', alignItems: 'center', gap: 10,
                    background: 'none', border: 'none', cursor: 'pointer',
                    padding: 0,
                }}
            >
            
                <span style={{
                    fontFamily: "'DM Serif Display', serif",
                    fontSize: 20, fontWeight: 700, color: '#0f1e3d',
                }}>
                    Smar<span style={{ color: '#4f8ef7' }}>Test</span>
                </span>
            </button>

            {/* ── DROITE ── */}
            {authenticated ? (
                /* Mode connecté */
                <div style={{ display: 'flex', alignItems: 'center', gap: 16 }}>
                    <div style={{
                        display: 'flex', alignItems: 'center', gap: 6,
                        fontSize: 12, color: '#16a34a', fontWeight: 500,
                    }}>
                        <span style={{
                            width: 7, height: 7, borderRadius: '50%',
                            background: '#16a34a', display: 'inline-block',
                        }} />
                        Connecté
                    </div>

                    <span style={{
                        fontSize: 13, color: '#0f1e3d', fontWeight: 600,
                        background: '#f0f3f9', border: '1px solid #e2e8f4',
                        borderRadius: 20, padding: '4px 14px',
                    }}>
                        {nom}
                    </span>

                    <button
                        onClick={logout}
                        title="Se déconnecter"
                        style={{
                            display: 'flex', alignItems: 'center', gap: 6,
                            background: 'none', border: '1px solid #e2e8f4',
                            borderRadius: 8, cursor: 'pointer',
                            color: '#8899b8', padding: '5px 10px',
                            fontSize: 12, fontWeight: 500,
                            fontFamily: "'DM Sans', sans-serif",
                            transition: 'all 0.15s',
                        }}
                        onMouseEnter={e => {
                            e.currentTarget.style.color = '#dc2626'
                            e.currentTarget.style.borderColor = '#f7c1c1'
                            e.currentTarget.style.background = '#fcebeb'
                        }}
                        onMouseLeave={e => {
                            e.currentTarget.style.color = '#8899b8'
                            e.currentTarget.style.borderColor = '#e2e8f4'
                            e.currentTarget.style.background = 'none'
                        }}
                    >
                        <LogOut size={14} />
                        Déconnexion
                    </button>
                </div>
            ) : (
                /* Mode public */
                <div style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
                    <button
                        onClick={() => navigate('/login')}
                        style={{
                            height: 34, padding: '0 16px',
                            background: 'none', color: '#0f1e3d',
                            border: '1px solid #e2e8f4', borderRadius: 8,
                            fontSize: 13, fontWeight: 500, cursor: 'pointer',
                            fontFamily: "'DM Sans', sans-serif",
                            transition: 'background 0.15s',
                        }}
                        onMouseEnter={e => e.currentTarget.style.background = '#f0f3f9'}
                        onMouseLeave={e => e.currentTarget.style.background = 'none'}
                    >
                        Se connecter
                    </button>
                    <button
                        onClick={() => navigate('/register')}
                        style={{
                            height: 34, padding: '0 16px',
                            background: '#0f1e3d', color: '#fff',
                            border: 'none', borderRadius: 8,
                            fontSize: 13, fontWeight: 600, cursor: 'pointer',
                            fontFamily: "'DM Sans', sans-serif",
                            transition: 'opacity 0.15s',
                        }}
                        onMouseEnter={e => e.currentTarget.style.opacity = '0.87'}
                        onMouseLeave={e => e.currentTarget.style.opacity = '1'}
                    >
                        S'inscrire
                    </button>
                </div>
            )}
        </nav>
    )
}
