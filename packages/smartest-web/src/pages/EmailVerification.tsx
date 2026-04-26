import { useEffect, useState } from 'react'
import { useSearchParams, useNavigate } from 'react-router-dom'
import { CheckCircle, XCircle, Mail } from 'lucide-react'
import {
    pageStyle, cardStyle, brandStyle, brandSubStyle,
    submitBtnStyle, Footer,
} from '../styles/AuthStyles'

type Status = 'loading' | 'success' | 'error'

export default function EmailVerification() {
    const [searchParams] = useSearchParams()
    const navigate       = useNavigate()
    const [countdown, setCountdown] = useState(5)

    const statusParam = searchParams.get('status')
    const status: Status = statusParam === 'success' ? 'success'
        : statusParam === 'error' ? 'error' : 'loading'

    useEffect(() => {
        if (status !== 'success') return
        if (countdown === 0) { navigate('/login'); return }
        const t = setTimeout(() => setCountdown(c => c - 1), 1000)
        return () => clearTimeout(t)
    }, [status, countdown, navigate])

    return (
        <main style={pageStyle}>
            <div style={{ ...cardStyle, textAlign: 'center', display: 'flex', flexDirection: 'column', alignItems: 'center' }}>

                {status !== 'loading' && (
                    <span style={{
                        display: 'inline-block', fontSize: '0.65rem', fontWeight: 600,
                        letterSpacing: '0.1em', textTransform: 'uppercase',
                        padding: '3px 12px', borderRadius: 20, marginBottom: '1rem',
                        ...(status === 'success'
                            ? { background: '#eaf3de', color: '#3b6d11' }
                            : { background: '#fcebeb', color: '#791f1f' }),
                    }}>
                        {status === 'success' ? 'Succès' : 'Erreur'}
                    </span>
                )}

                <h1 style={brandStyle}>SmarTest</h1>
                <p style={brandSubStyle}>Plateforme d'évaluation</p>

                {/* ── SUCCESS ── */}
                {status === 'success' && (
                    <>
                        <div style={{ width: 72, height: 72, borderRadius: '50%', background: '#eaf3de', display: 'flex', alignItems: 'center', justifyContent: 'center', marginBottom: '1.25rem' }}>
                            <CheckCircle size={32} style={{ color: '#3b6d11' }} />
                        </div>
                        <h2 style={{ fontFamily: "'DM Serif Display', serif", fontSize: '1.35rem', color: '#0f1e3d', marginBottom: 8 }}>
                            Email confirmé !
                        </h2>
                        <div style={{ width: 40, height: 1, background: '#e2e8f4', margin: '0.75rem auto' }} />
                        <p style={{ fontSize: '0.82rem', color: '#6b7a99', lineHeight: 1.6, marginBottom: '1.25rem' }}>
                            Votre compte a été activé avec succès. Vous pouvez maintenant accéder à la plateforme.
                        </p>
                        <div style={{
                            width: '100%', padding: '0.65rem 1rem', borderRadius: 8,
                            background: '#eaf3de', border: '1px solid #c0dd97',
                            color: '#27500a', fontSize: '0.8rem', fontWeight: 500, marginBottom: '1.25rem',
                        }}>
                            Redirection dans <strong>{countdown}s</strong>...
                        </div>
                        <button onClick={() => navigate('/login')} style={submitBtnStyle}
                            onMouseEnter={e => e.currentTarget.style.opacity = '0.88'}
                            onMouseLeave={e => e.currentTarget.style.opacity = '1'}
                        >
                            Se connecter maintenant
                        </button>
                    </>
                )}

                {/* ── ERROR ── */}
                {status === 'error' && (
                    <>
                        <div style={{ width: 72, height: 72, borderRadius: '50%', background: '#fcebeb', display: 'flex', alignItems: 'center', justifyContent: 'center', marginBottom: '1.25rem' }}>
                            <XCircle size={32} style={{ color: '#a32d2d' }} />
                        </div>
                        <h2 style={{ fontFamily: "'DM Serif Display', serif", fontSize: '1.35rem', color: '#0f1e3d', marginBottom: 8 }}>
                            Lien invalide
                        </h2>
                        <div style={{ width: 40, height: 1, background: '#e2e8f4', margin: '0.75rem auto' }} />
                        <p style={{ fontSize: '0.82rem', color: '#6b7a99', lineHeight: 1.6, marginBottom: '1.25rem' }}>
                            Ce lien est invalide ou a expiré. Les liens de confirmation sont valides pendant 24h seulement.
                        </p>
                        <div style={{
                            width: '100%', padding: '0.65rem 1rem', borderRadius: 8,
                            background: '#fcebeb', border: '1px solid #f7c1c1',
                            color: '#791f1f', fontSize: '0.8rem', fontWeight: 500, marginBottom: '1.25rem',
                        }}>
                            Lien expiré — veuillez vous réinscrire
                        </div>
                        <button onClick={() => navigate('/register')}
                            style={{ ...submitBtnStyle, marginBottom: '0.5rem' }}
                            onMouseEnter={e => e.currentTarget.style.opacity = '0.88'}
                            onMouseLeave={e => e.currentTarget.style.opacity = '1'}
                        >
                            Créer un nouveau compte
                        </button>
                        <button onClick={() => navigate('/login')}
                            style={{
                                width: '100%', height: 42, background: 'transparent',
                                color: '#1a2e5a', border: '1px solid #d4dce8', borderRadius: 9,
                                fontFamily: "'DM Sans', sans-serif", fontSize: '0.875rem',
                                fontWeight: 500, cursor: 'pointer', transition: 'background 0.15s',
                            }}
                            onMouseEnter={e => e.currentTarget.style.background = '#f6f8fc'}
                            onMouseLeave={e => e.currentTarget.style.background = 'transparent'}
                        >
                            Retour à la connexion
                        </button>
                    </>
                )}

                {/* ── LOADING ── */}
                {status === 'loading' && (
                    <>
                        <p style={{ fontSize: '0.85rem', color: '#6b7a99', marginBottom: '1rem' }}>
                            Paramètres manquants...
                        </p>
                        <button onClick={() => navigate('/login')} style={submitBtnStyle}>
                            Retour à la connexion
                        </button>
                    </>
                )}

                <Footer />
            </div>
        </main>
    )
}