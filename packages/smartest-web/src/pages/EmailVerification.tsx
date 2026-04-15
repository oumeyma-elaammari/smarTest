import { useEffect, useState } from 'react'
import { useSearchParams, useNavigate } from 'react-router-dom'
import { CheckCircle, XCircle, Mail } from 'lucide-react'

type Status = 'loading' | 'success' | 'error'

export default function EmailVerification() {
    const [searchParams] = useSearchParams()
    const navigate = useNavigate()
    const [countdown, setCountdown] = useState(5)

    // ✅ Lire directement le paramètre status envoyé par le backend
    const statusParam = searchParams.get('status')
    const status: Status = statusParam === 'success'
        ? 'success'
        : statusParam === 'error'
            ? 'error'
            : 'loading'

    // Countdown vers login après succès
    useEffect(() => {
        if (status !== 'success') return
        if (countdown === 0) {
            navigate('/login')
            return
        }
        const timer = setTimeout(() => setCountdown(c => c - 1), 1000)
        return () => clearTimeout(timer)
    }, [status, countdown, navigate])

    return (
        <main className="flex min-h-screen items-center justify-center bg-background p-4">
            <div className="card w-full max-w-[420px] text-center">

                {/* Logo */}
                <div className="mb-6">
                    <h1 className="text-2xl font-bold tracking-widest" style={{ color: '#1a2e5a' }}>
                        SmarTest
                    </h1>
                    <p className="text-muted">Plateforme d'évaluation sécurisée</p>
                </div>

                {/* ── SUCCESS ── */}
                {status === 'success' && (
                    <div className="space-y-4 py-6">
                        <div className="flex justify-center">
                            <div className="p-4 rounded-full" style={{ backgroundColor: '#f0fdf4' }}>
                                <CheckCircle size={40} style={{ color: '#16a34a' }} />
                            </div>
                        </div>
                        <h2 className="text-lg font-semibold" style={{ color: '#16a34a' }}>
                            Email confirmé avec succès !
                        </h2>
                        <p className="text-muted">
                            Votre compte a été activé. Vous pouvez maintenant vous connecter.
                        </p>
                        <div className="px-4 py-3 rounded-lg text-sm"
                            style={{
                                backgroundColor: '#f0fdf4',
                                color: '#16a34a',
                                border: '1px solid #bbf7d0',
                            }}>
                            Redirection dans <strong>{countdown}s</strong>...
                        </div>
                        <button onClick={() => navigate('/login')} className="btn-primary">
                            Se connecter maintenant
                        </button>
                    </div>
                )}

                {/* ── ERROR ── */}
                {status === 'error' && (
                    <div className="space-y-4 py-6">
                        <div className="flex justify-center">
                            <div className="p-4 rounded-full" style={{ backgroundColor: '#fef2f2' }}>
                                <XCircle size={40} style={{ color: '#dc2626' }} />
                            </div>
                        </div>
                        <h2 className="text-lg font-semibold" style={{ color: '#dc2626' }}>
                            Lien invalide ou expiré
                        </h2>
                        <p className="text-muted">
                            Ce lien est invalide ou a expiré. Veuillez vous réinscrire.
                        </p>
                        <div className="px-4 py-3 rounded-lg text-sm"
                            style={{
                                backgroundColor: '#fef2f2',
                                color: '#dc2626',
                                border: '1px solid #fecaca',
                            }}>
                            Le lien est valide pendant 24h seulement.
                        </div>
                        <div className="space-y-2">
                            <button onClick={() => navigate('/register')} className="btn-primary">
                                Créer un nouveau compte
                            </button>
                            <button
                                onClick={() => navigate('/login')}
                                style={{
                                    width: '100%',
                                    padding: '0.625rem 1rem',
                                    borderRadius: '0.5rem',
                                    fontWeight: 600,
                                    fontSize: '0.875rem',
                                    border: '1px solid #d1d5db',
                                    backgroundColor: 'transparent',
                                    cursor: 'pointer',
                                    color: '#1a2e5a',
                                }}
                                onMouseEnter={e => e.currentTarget.style.backgroundColor = '#f9fafb'}
                                onMouseLeave={e => e.currentTarget.style.backgroundColor = 'transparent'}
                            >
                                Retour à la connexion
                            </button>
                        </div>
                    </div>
                )}

                {/* ── LOADING (pas de paramètre) ── */}
                {status === 'loading' && (
                    <div className="space-y-4 py-6">
                        <p className="text-muted">Paramètres manquants...</p>
                        <button onClick={() => navigate('/login')} className="btn-primary">
                            Retour à la connexion
                        </button>
                    </div>
                )}

                <div className="mt-4 flex justify-center items-center gap-1">
                    <Mail size={14} style={{ color: 'var(--muted-foreground)' }} />
                    <span className="text-muted" style={{ fontSize: '0.75rem' }}>SmarTest — ENSA</span>
                </div>
            </div>
        </main>
    )
}