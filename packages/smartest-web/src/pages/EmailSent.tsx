import { useEffect, useRef, useState } from 'react'
import { Link, useLocation } from 'react-router-dom'
import { ArrowLeft, Mail, RefreshCw } from 'lucide-react'
import api from '../api/axiosConfig'
import {
    pageStyle, cardStyle, brandStyle, brandSubStyle, backLinkStyle, Footer, Alert,
} from '../styles/AuthStyles'

const steps = [
    { step: '1', text: 'Ouvrez votre boîte mail académique' },
    { step: '2', text: <>Cherchez un email de Smar<span style={{ color: '#4f8ef7' }}>Test</span></> },
    { step: '3', text: <>Cliquez sur <strong>"Confirmer mon email"</strong></> },
    { step: '4', text: 'Connectez-vous à la plateforme' },
]

const COOLDOWN_SEC = 60

export default function EmailSent() {
    const location = useLocation()
    const email: string = (location.state as any)?.email ?? ''

    const [status, setStatus]     = useState<'idle' | 'loading' | 'success' | 'error'>('idle')
    const [cooldown, setCooldown] = useState(0)
    const timerRef                = useRef<ReturnType<typeof setInterval> | null>(null)

    useEffect(() => () => { if (timerRef.current) clearInterval(timerRef.current) }, [])

    const startCooldown = () => {
        setCooldown(COOLDOWN_SEC)
        timerRef.current = setInterval(() => {
            setCooldown(prev => {
                if (prev <= 1) { clearInterval(timerRef.current!); return 0 }
                return prev - 1
            })
        }, 1000)
    }

    const handleResend = async () => {
        if (!email || status === 'loading' || cooldown > 0) return
        setStatus('loading')
        try {
            await api.post(`/auth/verify-email/resend/etudiant?email=${encodeURIComponent(email)}`)
            setStatus('success')
            startCooldown()
        } catch {
            setStatus('error')
        }
    }

    return (
        <main style={pageStyle}>
            <div style={{ ...cardStyle, maxWidth: 450, textAlign: 'center' }}>

                <h1 style={brandStyle}>Smar<span style={{ color: '#4f8ef7' }}>Test</span></h1>
                <p style={brandSubStyle}>Plateforme d'évaluation</p>

                <div style={{
                    width: 76, height: 76, borderRadius: '50%',
                    background: '#e8eef8', display: 'flex',
                    alignItems: 'center', justifyContent: 'center',
                    margin: '0 auto 1.5rem',
                }}>
                    <Mail size={34} color="#1a2e5a" strokeWidth={1.8} />
                </div>

                <h2 style={{ fontFamily: "'DM Serif Display', serif", fontSize: '1.5rem', color: '#0f1e3d', marginBottom: 8 }}>
                    Vérifiez votre email
                </h2>
                <div style={{ width: 40, height: 2, background: '#1a2e5a', borderRadius: 2, margin: '0.75rem auto 1rem' }} />

                <p style={{ fontSize: '0.83rem', color: '#6b7a99', lineHeight: 1.65, marginBottom: '1.5rem' }}>
                    {email
                        ? <>Un email de confirmation a été envoyé à <strong style={{ color: '#0f1e3d' }}>{email}</strong>.<br />Suivez les étapes ci-dessous pour activer votre compte.</>
                        : 'Un email de confirmation a été envoyé à votre adresse académique. Suivez les étapes ci-dessous pour activer votre compte.'
                    }
                </p>

                <div style={{
                    background: '#f6f8fc', border: '1px solid #e2e8f4',
                    borderRadius: 12, padding: '1.25rem', marginBottom: '1.75rem',
                    textAlign: 'left', display: 'flex', flexDirection: 'column', gap: '0.875rem',
                }}>
                    {steps.map(({ step, text }, i) => (
                        <div key={step} style={{ display: 'flex', alignItems: 'center', gap: '0.875rem', position: 'relative' }}>
                            {i < steps.length - 1 && (
                                <div style={{
                                    position: 'absolute', left: 12, top: 26,
                                    width: 2, height: 'calc(100% + 14px)', background: '#e2e8f4',
                                }} />
                            )}
                            <div style={{
                                width: 26, height: 26, borderRadius: '50%',
                                background: '#0f1e3d', color: '#fff',
                                fontSize: '0.72rem', fontWeight: 600,
                                display: 'flex', alignItems: 'center', justifyContent: 'center',
                                flexShrink: 0, position: 'relative', zIndex: 1,
                            }}>{step}</div>
                            <span style={{ fontSize: '0.83rem', color: '#2d3a52', lineHeight: 1.4 }}>{text}</span>
                        </div>
                    ))}
                </div>

                {/* ── Renvoyer l'email ── */}
                {email && (
                    <div style={{
                        borderTop: '1px solid #e2e8f4', paddingTop: '1.25rem', marginBottom: '1.25rem',
                    }}>
                        <p style={{ fontSize: '0.8rem', color: '#6b7a99', marginBottom: '0.75rem' }}>
                            Vous n'avez pas reçu l'email ?
                        </p>

                        {status === 'success' && (
                            <Alert type="success">Email renvoyé ! Vérifiez votre boîte mail.</Alert>
                        )}
                        {status === 'error' && (
                            <Alert type="error">Impossible de renvoyer l'email. Réessayez.</Alert>
                        )}

                        <button
                            onClick={handleResend}
                            disabled={status === 'loading' || cooldown > 0}
                            style={{
                                display: 'inline-flex', alignItems: 'center', gap: '0.4rem',
                                padding: '0.5rem 1.1rem', borderRadius: 8,
                                border: '1.5px solid #d8e0f0', background: '#fff',
                                color: cooldown > 0 ? '#94a3b8' : '#0f1e3d',
                                fontSize: '0.82rem', fontWeight: 500, cursor: cooldown > 0 || status === 'loading' ? 'not-allowed' : 'pointer',
                                transition: 'all 0.15s', opacity: status === 'loading' ? 0.7 : 1,
                            }}
                            onMouseEnter={e => { if (cooldown === 0 && status !== 'loading') e.currentTarget.style.background = '#f6f8fc' }}
                            onMouseLeave={e => { e.currentTarget.style.background = '#fff' }}
                        >
                            <RefreshCw size={14} style={{ animation: status === 'loading' ? 'spin 1s linear infinite' : 'none' }} />
                            {status === 'loading'
                                ? 'Envoi en cours...'
                                : cooldown > 0
                                    ? `Renvoyer dans ${cooldown}s`
                                    : "Renvoyer l'email"
                            }
                        </button>
                    </div>
                )}

                <Link to="/login" style={backLinkStyle}
                    onMouseEnter={e => e.currentTarget.style.background = '#f0f3f9'}
                    onMouseLeave={e => e.currentTarget.style.background = 'transparent'}
                >
                    <ArrowLeft size={15} />
                    Retour à la connexion
                </Link>

                <Footer />
            </div>
        </main>
    )
}
