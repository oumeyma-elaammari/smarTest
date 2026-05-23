import { useEffect, useState } from 'react'
import type { CSSProperties } from 'react'
import { BrowserRouter, Routes, Route, useSearchParams, Navigate, useParams } from 'react-router-dom'
import ErrorBoundary from './components/ErrorBoundary'
import Login from './pages/Login'
import Home from './pages/Home'
import PrivateRoute from './components/PrivateRoute'
import Navbar from './components/Navbar'
import NotFound from './pages/NotFound'
import Register from './pages/Register'
import EmailSent from './pages/EmailSent'
import ResetPassword from './pages/ResetPassword'
import ForgotPassword from './pages/ForgotPassword'
import EmailVerification from './pages/EmailVerification'
import QuizLive from './pages/QuizLive'
import QuizLiveParticiper from './pages/QuizLiveParticiper'
import Footer from './components/Footer'
import MesQuizWeb from './pages/MesQuizWeb'
import MesExamensWeb from './pages/MesExamensWeb'
import QuizPassageWeb from './pages/QuizPassageWeb'
import ExamenPassageWeb from './pages/ExamenPassageWeb'
import ExamenSupervisionPage from './pages/ExamenSupervisionPage'
import { useMatchMedia } from './hooks/useMatchMedia'
import useAuth from './hooks/useAuth'

const serif = "'DM Serif Display', Georgia, serif"
const bleuTest = '#4f8ef7'

type DashboardLayoutProps = {
    readonly children: React.ReactNode
    readonly showFooter?: boolean
}

export function DashboardLayout({
    children,
    showFooter = true,
}: DashboardLayoutProps) {
    return (
        <>
            <Navbar />
            <div
                style={{
                    minHeight: 'calc(100vh - 48px)',
                    width: '100%',
                    background: '#ffffff',
                    display: 'flex',
                    flexDirection: 'column',
                    boxSizing: 'border-box',
                }}
            >
                <main
                    style={{
                        flex: 1,
                        width: '100%',
                        display: 'flex',
                        flexDirection: 'column',
                        alignItems: 'center',
                        overflowY: 'auto',
                        paddingTop: 'clamp(16px, 4vw, 28px)',
                        paddingBottom: 'clamp(24px, 5vw, 40px)',
                        paddingLeft: 'clamp(12px, 4vw, 2rem)',
                        paddingRight: 'clamp(12px, 4vw, 2rem)',
                        boxSizing: 'border-box',
                    }}
                >
                    {children}
                </main>
                {showFooter ? <Footer /> : null}
            </div>
        </>
    )
}

function Dashboard() {
    const [searchParams] = useSearchParams()
    const tabParam = searchParams.get('tab')
    const role = useAuth((s) => s.role)
    const isProfesseurWeb = (role ?? '').trim().toUpperCase() === 'PROFESSEUR'
    const wideTabs = useMatchMedia('(min-width: 480px)')
    const [onglet, setOnglet] = useState<'quiz' | 'examens'>(() =>
        tabParam === 'examens' || isProfesseurWeb ? 'examens' : 'quiz',
    )

    useEffect(() => {
        if (isProfesseurWeb) {
            setOnglet('examens')
            return
        }
        if (tabParam === 'examens') setOnglet('examens')
        if (tabParam === 'quiz') setOnglet('quiz')
    }, [tabParam, isProfesseurWeb])

    const filtreBtn = (actif: boolean): CSSProperties => ({
        border: 'none',
        background: 'none',
        padding: wideTabs ? '0 10px 0 0' : '0 8px 0 0',
        margin: 0,
        fontFamily: serif,
        fontSize: wideTabs ? 'clamp(1.15rem, 4vw, 1.5rem)' : '1.15rem',
        fontWeight: actif ? 550 : 350,
        cursor: 'pointer',
        color: actif ? '#0f1e3d' : '#94a3b8',
        lineHeight: 1.2,
        transition: 'color 0.15s, font-weight 0.15s',
        whiteSpace: 'nowrap',
    })

    return (
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
                    justifyContent: 'flex-start',
                    alignItems: 'baseline',
                    flexWrap: 'wrap',
                    gap: wideTabs ? '4px 0' : '2px 12px',
                    width: '100%',
                    marginBottom: 'clamp(14px, 3vw, 22px)',
                }}
                role={isProfesseurWeb ? undefined : 'tablist'}
                aria-label={isProfesseurWeb ? undefined : 'Filtrer le contenu'}
            >
                {isProfesseurWeb ? (
                    <h1
                        style={{
                            margin: 0,
                            fontFamily: serif,
                            fontSize: wideTabs ? 'clamp(1.15rem, 4vw, 1.5rem)' : '1.15rem',
                            fontWeight: 550,
                            color: '#0f1e3d',
                            lineHeight: 1.2,
                        }}
                    >
                        Liste des examens
                    </h1>
                ) : (
                    <>
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
                    </>
                )}
            </div>

            <div style={{ width: '100%', flex: 1 }}>
                {isProfesseurWeb || onglet === 'examens' ? (
                    <MesExamensWeb accentBleu={bleuTest} />
                ) : (
                    <MesQuizWeb accentBleu={bleuTest} />
                )}
            </div>
        </div>
    )
}

/** Alias bureau SmarTest : /examens/:id → même écran que la supervision prof. */
function RedirectExamenGestionDepuisBureau() {
    const { examenId } = useParams<{ examenId: string }>()
    const id = examenId?.trim()
    if (!id) return <Navigate to="/dashboard?tab=examens" replace />
    return <Navigate to={`/supervision/examen/${id}`} replace />
}

export default function App() {
    return (
        <ErrorBoundary>
            <BrowserRouter>
                <Routes>
                    <Route path="/" element={<Home />} />
                    <Route path="/login" element={<Login />} />
                    <Route path="/register" element={<Register />} />
                    <Route path="/email-sent" element={<EmailSent />} />
                    <Route path="/forgot-password" element={<ForgotPassword />} />
                    <Route path="/reset-password" element={<ResetPassword />} />

                    <Route path="/verify-email" element={<EmailVerification />} />

                    <Route
                        path="/dashboard"
                        element={
                            <PrivateRoute allowedRoles={['ETUDIANT', 'PROFESSEUR']}>
                                <DashboardLayout>
                                    <Dashboard />
                                </DashboardLayout>
                            </PrivateRoute>
                        }
                    />

                    <Route
                        path="/quiz/:quizId"
                        element={
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
                        }
                    />
                    <Route
                        path="/passer-quiz/:quizId"
                        element={
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
                        }
                    />

                    <Route
                        path="/examen/:examenId"
                        element={
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
                                        <ExamenPassageWeb />
                                    </div>
                                </DashboardLayout>
                            </PrivateRoute>
                        }
                    />
                    <Route
                        path="/examen/:examenId/epreuve"
                        element={
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
                                        <ExamenPassageWeb />
                                    </div>
                                </DashboardLayout>
                            </PrivateRoute>
                        }
                    />
                    <Route
                        path="/supervision/examen/:examenId"
                        element={
                            <PrivateRoute allowedRoles={['PROFESSEUR']}>
                                <DashboardLayout>
                                    <div
                                        style={{
                                            width: '100%',
                                            maxWidth: 1040,
                                            margin: '0 auto',
                                            boxSizing: 'border-box',
                                        }}
                                    >
                                        <ExamenSupervisionPage accentBleu={bleuTest} />
                                    </div>
                                </DashboardLayout>
                            </PrivateRoute>
                        }
                    />
                    <Route
                        path="/examens/:examenId"
                        element={
                            <PrivateRoute allowedRoles={['PROFESSEUR']}>
                                <RedirectExamenGestionDepuisBureau />
                            </PrivateRoute>
                        }
                    />

                    {/* QR session éphémère : WebSocket / STOMP, sans auth */}
                    <Route path="/quiz-live/:sessionToken/participer" element={<QuizLiveParticiper />} />
                    <Route path="/quiz-live/:sessionToken" element={<QuizLive />} />

                    <Route path="*" element={<NotFound />} />
                </Routes>
            </BrowserRouter>
        </ErrorBoundary>
    )
}
