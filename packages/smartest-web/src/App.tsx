import { useState } from 'react'
import type { CSSProperties } from 'react'
import { BrowserRouter, Routes, Route } from 'react-router-dom'
import Login from './pages/Login'
import Home from './pages/Home'
import PrivateRoute from './components/PrivateRoute'
import Navbar from './components/Navbar'
import Register from './pages/Register'
import EmailSent from './pages/EmailSent'
import ResetPassword from './pages/ResetPassword'
import ForgotPassword from './pages/ForgotPassword'
import EmailVerification from './pages/EmailVerification'
import MesQuizWeb from './pages/MesQuizWeb'
import QuizPassageWeb from './pages/QuizPassageWeb'

const sans = "'DM Sans', system-ui, sans-serif"
const serif = "'DM Serif Display', Georgia, serif"
const bleuTest = '#4f8ef7'

function DashboardLayout({ children }: { children: React.ReactNode }) {
    return (
        <>
            <Navbar />
            <main
                style={{
                    minHeight: 'calc(100vh - 48px)',
                    width: '100%',
                    background: '#ffffff',
                    display: 'flex',
                    flexDirection: 'column',
                    alignItems: 'center',
                    overflowY: 'auto',
                    paddingTop: 28,
                    paddingBottom: 40,
                    paddingLeft: 'clamp(1rem, 3vw, 2rem)',
                    paddingRight: 'clamp(1rem, 3vw, 2rem)',
                    boxSizing: 'border-box',
                }}
            >
                {children}
            </main>
        </>
    )
}

function Dashboard() {
    const [onglet, setOnglet] = useState<'quiz' | 'examens'>('quiz')

    /** Même style typographique que l’ancien titre « Mes quiz » : serif, pas de fond. */
    const filtreBtn = (actif: boolean): CSSProperties => ({
        border: 'none',
        background: 'none',
        padding: '0 10px 0 0',
        margin: 0,
        fontFamily: serif,
        fontSize: '1.5rem',
        fontWeight: actif ? 550 : 350,
        cursor: 'pointer',
        color: actif ? '#0f1e3d' : '#94a3b8',
        lineHeight: 1.2,
        transition: 'color 0.15s, font-weight 0.15s',
    })

    return (
        <DashboardLayout>
            <div
                style={{
                    width: '100%',
                    maxWidth: 1040,
                    margin: '0 auto',
                    display: 'flex',
                    flexDirection: 'column',
                    alignItems: 'stretch',
                    boxSizing: 'border-box',
                }}
            >
                <div
                    style={{
                        display: 'flex',
                        justifyContent: 'left',
                        alignItems: 'baseline',
                        flexWrap: 'wrap',
                        gap: '4px 0',
                        width: '100%',
                        marginBottom: 22,
                    }}
                    role="tablist"
                    aria-label="Filtrer le contenu"
                >
                    <button
                        type="button"
                        role="tab"
                        aria-selected={onglet === 'quiz'}
                        style={filtreBtn(onglet === 'quiz')}
                        onClick={() => setOnglet('quiz')}
                    >
                        Mes quiz
                    </button>
                    <span
                        style={{
                            color: '#cbd5e1',
                            fontFamily: serif,
                            fontSize: '1.35rem',
                            userSelect: 'none',
                            padding: '0 6px',
                            lineHeight: 1,
                        }}
                        aria-hidden
                    >
                        |
                    </span>
                    <button
                        type="button"
                        role="tab"
                        aria-selected={onglet === 'examens'}
                        style={filtreBtn(onglet === 'examens')}
                        onClick={() => setOnglet('examens')}
                    >
                        Mes examens
                    </button>
                </div>

                <div
                    style={{
                        width: '100%',
                        display: 'flex',
                        flexDirection: 'column',
                        alignItems: 'stretch',
                    }}
                >
                    {onglet === 'quiz' ? (
                        <MesQuizWeb accentBleu={bleuTest} />
                    ) : (
                        <p
                            style={{
                                fontFamily: sans,
                                color: '#64748b',
                                margin: 0,
                                textAlign: 'center',
                                lineHeight: 1.6,
                                maxWidth: 520,
                                alignSelf: 'center',
                            }}
                        >
                            Les examens accessibles depuis le web seront listés ici dans une prochaine version.
                        </p>
                    )}
                </div>
            </div>
        </DashboardLayout>
    )
}

export default function App() {
    return (
        <BrowserRouter>
            <Routes>
                <Route path="/" element={<Home />} />
                <Route path="/login" element={<Login />} />
                <Route path="/register" element={<Register />} />
                <Route path="/email-sent" element={<EmailSent />} />
                <Route path="/forgot-password" element={<ForgotPassword />} />
                <Route path="/reset-password" element={<ResetPassword />} />

                <Route path="/verify-email" element={<EmailVerification />} />

                <Route path="/dashboard" element={
                    <PrivateRoute>
                        <Dashboard />
                    </PrivateRoute>
                } />
                <Route path="/quiz/:quizId" element={
                    <PrivateRoute>
                        <DashboardLayout>
                            <div
                                style={{
                                    width: '100%',
                                    maxWidth: 720,
                                    margin: '0 auto',
                                    padding: '1.5rem 1rem 2rem',
                                    boxSizing: 'border-box',
                                }}
                            >
                                <QuizPassageWeb />
                            </div>
                        </DashboardLayout>
                    </PrivateRoute>
                } />
            </Routes>
        </BrowserRouter>
    )
}