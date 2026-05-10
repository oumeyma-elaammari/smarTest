import { BrowserRouter, Routes, Route } from 'react-router-dom'
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
import QuizPassageWeb from './pages/QuizPassageWeb'

export function DashboardLayout({
    children,
    showFooter = true,
}: {
    children: React.ReactNode
    showFooter?: boolean
}) {
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
                        paddingTop: 28,
                        paddingBottom: 40,
                        paddingLeft: 'clamp(1rem, 3vw, 2rem)',
                        paddingRight: 'clamp(1rem, 3vw, 2rem)',
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
                        <PrivateRoute>
                            <DashboardLayout>
                                <MesQuizWeb />
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
                    path="/examen/:quizId"
                    element={
                        <PrivateRoute>
                            <DashboardLayout showFooter={false}>
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

                {/* QR session éphémère : aucune auth */}
                <Route path="/quiz-live/:sessionToken/participer" element={<QuizLiveParticiper />} />
                <Route path="/quiz-live/:sessionToken" element={<QuizLive />} />
                <Route path="*" element={<NotFound />} />
            </Routes>
        </BrowserRouter>
        </ErrorBoundary>
    )
}
