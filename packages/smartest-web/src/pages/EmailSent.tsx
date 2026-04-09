import { Link } from 'react-router-dom'
import { Mail, ArrowLeft } from 'lucide-react'

export default function EmailSent() {
    return (
        <main className="flex min-h-screen items-center justify-center bg-background p-4">
            <div className="card w-full max-w-[420px] text-center">

                {/* Icône */}
                <div className="flex justify-center mb-6">
                    <div style={{
                        width: '72px',
                        height: '72px',
                        borderRadius: '50%',
                        backgroundColor: '#eff6ff',
                        display: 'flex',
                        alignItems: 'center',
                        justifyContent: 'center',
                    }}>
                        <Mail size={32} color="#1a2e5a" />
                    </div>
                </div>

                {/* Titre */}
                <h1 style={{
                    fontSize: '1.25rem',
                    fontWeight: 700,
                    color: 'var(--foreground)',
                    marginBottom: '0.75rem',
                }}>
                    Vérifiez votre email
                </h1>

                {/* Message */}
                <p className="text-muted" style={{
                    fontSize: '0.875rem',
                    lineHeight: '1.6',
                    marginBottom: '0.5rem',
                }}>
                    Un email de confirmation a été envoyé à votre adresse académique.
                </p>
                <p className="text-muted" style={{
                    fontSize: '0.875rem',
                    lineHeight: '1.6',
                    marginBottom: '2rem',
                }}>
                    Cliquez sur le lien dans l'email pour activer votre compte et accéder à la plateforme.
                </p>

                {/* Étapes */}
                <div style={{
                    backgroundColor: '#f8fafc',
                    border: '1px solid #e5e7eb',
                    borderRadius: '0.5rem',
                    padding: '1rem',
                    marginBottom: '1.5rem',
                    textAlign: 'left',
                }}>
                    {[
                        { step: '1', text: 'Ouvrez votre boîte mail académique' },
                        { step: '2', text: 'Cherchez un email de SmarTest' },
                        { step: '3', text: 'Cliquez sur "Confirmer mon email"' },
                        { step: '4', text: 'Connectez-vous à la plateforme' },
                    ].map(({ step, text }) => (
                        <div key={step} style={{
                            display: 'flex',
                            alignItems: 'center',
                            gap: '0.75rem',
                            marginBottom: step === '4' ? 0 : '0.75rem',
                        }}>
                            <div style={{
                                width: '24px',
                                height: '24px',
                                borderRadius: '50%',
                                backgroundColor: '#1a2e5a',
                                color: 'white',
                                fontSize: '0.75rem',
                                fontWeight: 700,
                                display: 'flex',
                                alignItems: 'center',
                                justifyContent: 'center',
                                flexShrink: 0,
                            }}>
                                {step}
                            </div>
                            <span style={{ fontSize: '0.875rem', color: 'var(--foreground)' }}>
                                {text}
                            </span>
                        </div>
                    ))}
                </div>

                {/* Lien retour login */}
                <Link
                    to="/login"
                    style={{
                        display: 'inline-flex',
                        alignItems: 'center',
                        gap: '0.5rem',
                        fontSize: '0.875rem',
                        color: '#1a2e5a',
                        fontWeight: 600,
                        textDecoration: 'none',
                    }}
                    onMouseEnter={e => e.currentTarget.style.textDecoration = 'underline'}
                    onMouseLeave={e => e.currentTarget.style.textDecoration = 'none'}
                >
                    <ArrowLeft size={16} />
                    Retour à la connexion
                </Link>
            </div>
        </main>
    )
}