import { useNavigate } from 'react-router-dom'
import Navbar from '../components/Navbar'
import Footer from '../components/Footer'

const features = [
    {
        icon: '📋',
        title: 'Quiz interactifs',
        desc: 'Évaluations formatives avec correction immédiate après chaque réponse',
    },
    {
        icon: '🎓',
        title: 'Examens en direct',
        desc: 'Sessions supervisées en temps réel par votre professeur',
    },
    {
        icon: '📊',
        title: 'Résultats instantanés',
        desc: 'Consultez vos notes et corrections dès la fin de l\'évaluation',
    },
]

export default function Home() {
    const navigate = useNavigate()

    return (
        <div style={{
            minHeight: '100vh',
            display: 'flex',
            flexDirection: 'column',
            background: '#f0f3f9',
            fontFamily: "'DM Sans', sans-serif",
        }}>
            <Navbar authenticated={false} />

            {/* ── HERO ── */}
            <div style={{
                flex: 1,
                display: 'flex',
                flexDirection: 'column',
                alignItems: 'center',
                justifyContent: 'center',
                padding: '48px 24px',
                position: 'relative',
                overflow: 'hidden',
            }}>
                {/* Glows décoratifs */}
                <div style={{
                    position: 'absolute', width: 500, height: 500, borderRadius: '50%',
                    background: '#4f8ef7', opacity: 0.05,
                    top: -200, right: -120, pointerEvents: 'none',
                }} />
                <div style={{
                    position: 'absolute', width: 350, height: 350, borderRadius: '50%',
                    background: '#0f1e3d', opacity: 0.03,
                    bottom: -120, left: -80, pointerEvents: 'none',
                }} />

                {/* Badge */}
                <div style={{
                    display: 'inline-flex', alignItems: 'center', gap: 8,
                    background: '#eef3fd', border: '1px solid #d0ddf5',
                    borderRadius: 20, padding: '6px 18px', marginBottom: 24,
                }}>
                    <div style={{ width: 6, height: 6, borderRadius: '50%', background: '#4f8ef7' }} />
                    <span style={{ fontSize: 10, color: '#4f8ef7', fontWeight: 700, letterSpacing: 1.5 }}>
                        ESPACE ÉTUDIANT · UMP OUJDA
                    </span>
                </div>

                {/* Titre */}
                <h1 style={{
                    fontFamily: "'DM Serif Display', serif",
                    fontSize: 'clamp(2rem, 5vw, 3.2rem)',
                    color: '#0f1e3d', textAlign: 'center',
                    lineHeight: 1.15, marginBottom: 8, maxWidth: 600,
                }}>
                    Passez vos examens en toute <span style={{ color: '#4f8ef7' }}>sérénité</span>
                </h1>

                {/* Sous-titre */}
                <p style={{
                    fontSize: 11, color: '#8899b8', fontWeight: 600,
                    letterSpacing: 3, textAlign: 'center',
                    textTransform: 'uppercase', marginBottom: 14,
                }}>
                    PLATEFORME D'ÉVALUATION EN LIGNE SÉCURISÉE
                </p>

                {/* Ligne accent */}
                <div style={{
                    width: 44, height: 2, background: '#0f1e3d',
                    borderRadius: 2, marginBottom: 22,
                }} />

                {/* Description */}
                <p style={{
                    fontSize: 15, color: '#6b7a99', textAlign: 'center',
                    lineHeight: 1.8, maxWidth: 480, marginBottom: 40,
                }}>
                    Connectez-vous avec votre email académique{' '}
                    <strong style={{ color: '#0f1e3d' }}>@ump.ac.ma</strong>
                    {' '}pour accéder à vos quiz et examens — résultats instantanés, corrections automatiques.
                </p>

                {/* Boutons */}
                <div style={{
                    display: 'flex', gap: 12, marginBottom: 48,
                    flexWrap: 'wrap', justifyContent: 'center',
                }}>
                    <button type="button" onClick={() => navigate('/login')} style={{
                        height: 46, padding: '0 36px',
                        background: '#0f1e3d', color: '#fff',
                        border: 'none', borderRadius: 10,
                        fontSize: 14, fontWeight: 600, cursor: 'pointer',
                        fontFamily: "'DM Sans', sans-serif", transition: 'opacity 0.15s',
                    }}
                        onMouseEnter={e => e.currentTarget.style.opacity = '0.87'}
                        onMouseLeave={e => e.currentTarget.style.opacity = '1'}
                    >
                        Se connecter
                    </button>
                    <button type="button" onClick={() => navigate('/register')} style={{
                        height: 46, padding: '0 36px',
                        background: '#fff', color: '#1a2e5a',
                        border: '1.5px solid #d4dce8', borderRadius: 10,
                        fontSize: 14, fontWeight: 500, cursor: 'pointer',
                        fontFamily: "'DM Sans', sans-serif", transition: 'background 0.15s',
                    }}
                        onMouseEnter={e => e.currentTarget.style.background = '#f0f3f9'}
                        onMouseLeave={e => e.currentTarget.style.background = '#fff'}
                    >
                        Créer mon compte étudiant
                    </button>
                </div>

                {/* Feature cards */}
                <div style={{
                    display: 'grid',
                    gridTemplateColumns: 'repeat(auto-fit, minmax(220px, 1fr))',
                    gap: 16, width: '100%', maxWidth: 780,
                }}>
                    {features.map((f) => (
                        <article key={f.title} className="home-feature-card">
                            <div style={{
                                width: 32, height: 2, background: '#4f8ef7',
                                borderRadius: 2, margin: '0 auto 16px',
                            }} />
                            <p style={{ fontSize: 13, fontWeight: 700, color: '#0f1e3d', marginBottom: 8 }}>
                                {f.title}
                            </p>
                            <p style={{ fontSize: 11.5, color: '#8899b8', lineHeight: 1.6 }}>
                                {f.desc}
                            </p>
                        </article>
                    ))}
                </div>
            </div>

            <Footer />
        </div>
    )
}