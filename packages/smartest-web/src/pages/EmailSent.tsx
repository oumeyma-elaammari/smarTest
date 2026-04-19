import { Link } from 'react-router-dom'
import { ArrowLeft, Mail } from 'lucide-react'
import {
    pageStyle, cardStyle, brandStyle, brandSubStyle, backLinkStyle, Footer,
} from '../styles/AuthStyles'

const steps = [
    { step: '1', text: 'Ouvrez votre boîte mail académique' },
    { step: '2', text: 'Cherchez un email de SmarTest' },
    { step: '3', text: <>Cliquez sur <strong>"Confirmer mon email"</strong></> },
    { step: '4', text: 'Connectez-vous à la plateforme' },
]

export default function EmailSent() {
    return (
        <main style={pageStyle}>
            <div style={{ ...cardStyle, maxWidth: 450, textAlign: 'center' }}>

                <h1 style={brandStyle}>SmarTest</h1>
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
                    Un email de confirmation a été envoyé à votre adresse académique.
                    Suivez les étapes ci-dessous pour activer votre compte.
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