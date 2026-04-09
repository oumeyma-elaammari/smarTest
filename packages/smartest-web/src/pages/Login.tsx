import { useState } from 'react'
import { useNavigate, Link, useSearchParams } from 'react-router-dom'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Eye, EyeOff, Loader2, AlertCircle, CheckCircle, XCircle } from 'lucide-react'
import api from '../api/axiosConfig'
import useAuth from '../hooks/useAuth'

const loginSchema = z.object({
    email: z.string().min(1, "L'email est obligatoire").email("Format email invalide"),
    password: z.string().min(1, "Le mot de passe est obligatoire").min(8, "Minimum 8 caractères"),
})

type LoginForm = z.infer<typeof loginSchema>

const ERROR_MESSAGES = {
    INVALID_CREDENTIALS: 'Email ou mot de passe incorrect',
    NOT_STUDENT: "Ce compte n'est pas un compte étudiant",
    EMAIL_NOT_VERIFIED: 'Veuillez confirmer votre email avant de vous connecter',
    NETWORK_ERROR: 'Impossible de contacter le serveur. Vérifiez votre connexion',
    SERVER_ERROR: 'Erreur serveur. Réessayez plus tard',
    UNKNOWN: 'Une erreur inattendue est survenue',
} as const

export default function Login() {
    const navigate = useNavigate()
    const { login } = useAuth()
    const [searchParams] = useSearchParams()          // ✅ déclaré ici
    const verified = searchParams.get('verified')     // ✅ déclaré ici

    const [isLoading, setIsLoading] = useState(false)
    const [showPassword, setShowPassword] = useState(false)
    const [loginError, setLoginError] = useState<string | null>(null)

    const { register, handleSubmit, formState: { errors } } = useForm<LoginForm>({
        resolver: zodResolver(loginSchema),
        mode: 'onSubmit',
    })

    const onSubmit = async (data: LoginForm) => {
        setIsLoading(true)

        try {
            const { data: authData } = await api.post('/auth/login', {
                email: data.email.trim().toLowerCase(),
                password: data.password,
            })

            if (authData.role !== 'ETUDIANT') {
                setIsLoading(false)
                setLoginError(ERROR_MESSAGES.NOT_STUDENT)
                return
            }

            setLoginError(null)
            login(authData)
            navigate('/dashboard')

        } catch (error: any) {
            setIsLoading(false)

            if (!error?.response) {
                setLoginError(ERROR_MESSAGES.NETWORK_ERROR)
                return
            }

            const status = error.response.status   // ✅ déclaré avant utilisation

            if (status === 401) setLoginError(ERROR_MESSAGES.INVALID_CREDENTIALS)
            else if (status === 403) setLoginError(ERROR_MESSAGES.EMAIL_NOT_VERIFIED)
            else if (status >= 500) setLoginError(ERROR_MESSAGES.SERVER_ERROR)
            else setLoginError(ERROR_MESSAGES.UNKNOWN)
        }
    }

    return (
        <main className="flex min-h-screen items-center justify-center bg-background p-4">
            <div className="card w-full max-w-[400px]">

                {/* Header */}
                <div className="text-center mb-6 space-y-1">
                    <h1 className="text-2xl font-bold tracking-widest"
                        style={{ color: '#1a2e5a' }}>
                        SmarTest
                    </h1>
                    <p className="text-muted">Plateforme d'évaluation sécurisée</p>
                </div>

                {/* ✅ Email vérifié avec succès */}
                {verified === 'true' && (
                    <div className="mb-4 px-4 py-3 rounded-lg flex items-center gap-2 text-sm"
                        style={{
                            backgroundColor: '#f0fdf4',
                            color: '#16a34a',
                            border: '1px solid #bbf7d0',
                        }}>
                        <CheckCircle size={16} className="shrink-0" />
                        Email confirmé ! Vous pouvez maintenant vous connecter.
                    </div>
                )}

                {/* ❌ Lien invalide ou expiré */}
                {verified === 'false' && (
                    <div className="mb-4 px-4 py-3 rounded-lg flex items-center gap-2 text-sm"
                        style={{
                            backgroundColor: '#fef2f2',
                            color: '#dc2626',
                            border: '1px solid #fecaca',
                        }}>
                        <XCircle size={16} className="shrink-0" />
                        Lien invalide ou expiré. Réinscrivez-vous.
                    </div>
                )}

                {/* Bannière erreur login */}
                {loginError && (
                    <div className="mb-4 px-4 py-3 rounded-lg flex items-center gap-2 text-sm"
                        style={{
                            backgroundColor: '#fef2f2',
                            color: '#dc2626',
                            border: '1px solid #fecaca',
                        }}>
                        <AlertCircle size={16} className="shrink-0" />
                        {loginError}
                    </div>
                )}

                {/* Formulaire */}
                <form onSubmit={handleSubmit(onSubmit)} className="space-y-4" noValidate>

                    {/* Email */}
                    <div>
                        <label htmlFor="email" className="label">Email</label>
                        <input
                            {...register('email')}
                            id="email"
                            type="email"
                            placeholder="votre.email@exemple.com"
                            autoComplete="email"
                            disabled={isLoading}
                            className={`input ${errors.email ? 'error' : ''}`}
                        />
                        {errors.email && <p className="error-msg">{errors.email.message}</p>}
                    </div>

                    {/* Password */}
                    <div>
                        <label htmlFor="password" className="label">Mot de passe</label>
                        <div className="relative">
                            <input
                                {...register('password')}
                                id="password"
                                type={showPassword ? 'text' : 'password'}
                                placeholder="Votre mot de passe"
                                autoComplete="current-password"
                                disabled={isLoading}
                                className={`input pr-10 ${errors.password ? 'error' : ''}`}
                            />
                            <button
                                type="button"
                                onClick={() => setShowPassword(prev => !prev)}
                                className="absolute right-3 top-1/2 -translate-y-1/2"
                                style={{ color: 'var(--muted-foreground)', background: 'none', border: 'none', cursor: 'pointer' }}
                                aria-label={showPassword ? 'Cacher' : 'Afficher'}
                                tabIndex={-1}
                            >
                                {showPassword ? <EyeOff size={16} /> : <Eye size={16} />}
                            </button>
                        </div>
                        {errors.password && <p className="error-msg">{errors.password.message}</p>}
                    </div>

                    {/* Bouton */}
                    <button
                        type="submit"
                        disabled={isLoading}
                        className="btn-primary"
                    >
                        {isLoading
                            ? <span className="flex items-center justify-center gap-2">
                                <Loader2 size={16} className="animate-spin" />
                                Connexion en cours...
                              </span>
                            : 'Se connecter'
                        }
                    </button>
                </form>

                {/* Liens bas */}
                <div className="mt-5 text-center space-y-2">
                    <p className="text-muted" style={{ fontSize: '0.875rem' }}>
                        Pas encore de compte ?{' '}
                        <Link
                            to="/register"
                            style={{ color: '#1a2e5a', fontWeight: 600, textDecoration: 'none' }}
                            onMouseEnter={e => e.currentTarget.style.textDecoration = 'underline'}
                            onMouseLeave={e => e.currentTarget.style.textDecoration = 'none'}
                        >
                            S'inscrire
                        </Link>
                    </p>
                        <Link to="/forgot-password"
                            style={{
                                fontSize: '0.875rem',
                                color: '#1a2e5a',
                                textDecoration: 'none',
                            }}
                            onMouseEnter={e => e.currentTarget.style.textDecoration = 'underline'}
                            onMouseLeave={e => e.currentTarget.style.textDecoration = 'none'}
                        >
                            Mot de passe oublié ?
                        </Link>
                </div>
            </div>
        </main>
    )
}