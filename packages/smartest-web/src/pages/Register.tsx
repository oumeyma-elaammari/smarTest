import { useState, useEffect } from 'react'
import { useNavigate, Link } from 'react-router-dom'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Eye, EyeOff, Loader2, AlertCircle, CheckCircle } from 'lucide-react'
import api from '../api/axiosConfig'

const registerSchema = z.object({
    nom: z
        .string()
        .min(1, "Le nom est obligatoire")
        .min(2, "Le nom doit contenir au moins 2 caractères")
        .max(50, "Le nom ne doit pas dépasser 50 caractères")
        .regex(/^[a-zA-ZÀ-ÿ\s\-']+$/, "Le nom ne doit contenir que des lettres, espaces, tirets ou apostrophes"),
    email: z
        .string()
        .min(1, "L'email est obligatoire")
        .email("Format email invalide")
        .regex(/^[a-zA-Z0-9._%+\-]+@ump\.ac\.ma$/, "L'email doit être un email académique (@ump.ac.ma)"),
    password: z
        .string()
        .min(1, "Le mot de passe est obligatoire")
        .min(8, "Le mot de passe doit contenir au moins 8 caractères")
        .regex(/[A-Z]/, "Doit contenir au moins une majuscule")
        .regex(/[0-9]/, "Doit contenir au moins un chiffre")
        .regex(/[!@#$%^&*()_+\-=[\]{};':"\\|,.<>/?]/, "Doit contenir au moins un caractère spécial"),
    confirmPassword: z.string().min(1, "La confirmation est obligatoire"),
}).refine(data => data.password === data.confirmPassword, {
    message: "Les mots de passe ne correspondent pas",
    path: ["confirmPassword"],
})

type RegisterForm = z.infer<typeof registerSchema>

const ERROR_MESSAGES = {
    EMAIL_USED: 'Cet email est déjà utilisé',
    SERVER_ERROR: 'Erreur serveur. Réessayez plus tard',
    NETWORK_ERROR: 'Impossible de contacter le serveur. Vérifiez votre connexion',
    UNKNOWN: 'Une erreur inattendue est survenue',
} as const

const PasswordRequirements = ({ password }: { password: string }) => {
    const requirements = [
        { label: "Au moins 8 caractères", test: (p: string) => p.length >= 8 },
        { label: "Au moins 1 majuscule", test: (p: string) => /[A-Z]/.test(p) },
        { label: "Au moins 1 chiffre", test: (p: string) => /[0-9]/.test(p) },
        { label: "Au moins 1 caractère spécial", test: (p: string) => /[!@#$%^&*()_+\-=[\]{};':"\\|,.<>/?]/.test(p) },
    ]

    if (!password) return null

    return (
        <div className="mt-2 p-2 rounded-lg" style={{ backgroundColor: '#f8fafc' }}>
            <p className="text-xs font-medium mb-1" style={{ color: '#475569' }}>Exigences :</p>
            <div className="space-y-1">
                {requirements.map((req, idx) => {
                    const isValid = req.test(password)
                    return (
                        <div key={idx} className="flex items-center gap-2 text-xs">
                            <span style={{ color: isValid ? '#16a34a' : '#94a3b8' }}>
                                {isValid ? '✓' : '○'}
                            </span>
                            <span style={{ color: isValid ? '#16a34a' : '#64748b' }}>
                                {req.label}
                            </span>
                        </div>
                    )
                })}
            </div>
        </div>
    )
}

export default function Register() {
    const navigate = useNavigate()
    const [isLoading, setIsLoading] = useState(false)
    const [showPassword, setShowPassword] = useState(false)
    const [showConfirm, setShowConfirm] = useState(false)
    const [error, setError] = useState<string | null>(null)
    const [passwordValue, setPasswordValue] = useState('')

    const { register, handleSubmit, formState: { errors }, watch, trigger } = useForm<RegisterForm>({
        resolver: zodResolver(registerSchema),
        mode: 'onChange',
    })

    const watchedPassword = watch('password', '')

    useEffect(() => {
        setPasswordValue(watchedPassword || '')
    }, [watchedPassword])

    const onSubmit = async (data: RegisterForm) => {
        setIsLoading(true)
        setError(null)

        try {
            // UN SEUL appel API
            await api.post('/auth/register/etudiant', {
                nom: data.nom.trim(),
                email: data.email.trim().toLowerCase(),
                password: data.password,
                confirmPassword: data.confirmPassword,
            })

            // Redirection vers page email-sent
            navigate('/email-sent')

        } catch (err: any) {
            if (!err?.response) {
                setError(ERROR_MESSAGES.NETWORK_ERROR)
            } else {
                const status = err.response?.status
                if (status === 409) setError(ERROR_MESSAGES.EMAIL_USED)
                else if (status >= 500) setError(ERROR_MESSAGES.SERVER_ERROR)
                else setError(ERROR_MESSAGES.UNKNOWN)
            }
        } finally {
            setIsLoading(false)
        }
    }

    return (
        <main className="flex min-h-screen">

            {/* ── GAUCHE ───────────────────────────────────── */}
            <div
                className="hidden md:flex flex-col justify-center items-center w-1/2 p-12 relative overflow-hidden"
                style={{ backgroundColor: '#1a2e5a' }}
            >
                <div className="absolute top-0 right-0 w-64 h-64 rounded-full"
                    style={{ backgroundColor: 'rgba(255,255,255,0.05)', transform: 'translate(30%, -30%)' }} />
                <div className="absolute bottom-0 left-0 w-96 h-96 rounded-full"
                    style={{ backgroundColor: 'rgba(255,255,255,0.03)', transform: 'translate(-30%, 30%)' }} />

                <div className="relative z-10 text-center">
                    <div className="mb-8 mx-auto" style={{
                        width: '100px', height: '100px', borderRadius: '50%',
                        backgroundColor: 'rgba(255,255,255,0.1)',
                        display: 'flex', alignItems: 'center', justifyContent: 'center',
                        fontSize: '3rem',
                    }}>
                        🎓
                    </div>

                    <h1 style={{
                        color: 'white', fontSize: '2.5rem', fontWeight: 700,
                        letterSpacing: '0.1em', marginBottom: '1rem',
                    }}>
                        SmarTest
                    </h1>

                    <p style={{
                        color: 'rgba(255,255,255,0.8)', fontSize: '1rem',
                        lineHeight: '1.6', maxWidth: '320px', marginBottom: '3rem',
                    }}>
                        Plateforme intelligente et sécurisée de gestion des quiz et examens
                    </p>

                    <div className="space-y-3">
                        {[
                            { text: 'Examens sécurisés en temps réel' },
                            { text: 'Résultats instantanés' },
                            { text: 'Interface simple et intuitive' },
                        ].map((feature, i) => (
                            <div key={i} style={{
                                color: 'rgba(255,255,255,0.9)', fontSize: '1rem',
                                display: 'flex', alignItems: 'center', gap: '0.75rem',
                                padding: '0.5rem', borderRadius: '0.5rem', transition: 'all 0.3s',
                            }}
                                onMouseEnter={e => {
                                    e.currentTarget.style.backgroundColor = 'rgba(255,255,255,0.1)'
                                    e.currentTarget.style.transform = 'translateX(5px)'
                                }}
                                onMouseLeave={e => {
                                    e.currentTarget.style.backgroundColor = 'transparent'
                                    e.currentTarget.style.transform = 'translateX(0)'
                                }}
                            >
                                <span>{feature.text}</span>
                            </div>
                        ))}
                    </div>
                </div>
            </div>

            {/* ── DROITE ───────────────────────────────────── */}
            <div className="flex flex-col justify-center items-center w-full md:w-1/2 p-6 bg-background">
                <div className="w-full max-w-[450px]">

                    {/* Header mobile */}
                    <div className="text-center mb-6 md:hidden">
                        <h1 className="text-3xl font-bold tracking-widest" style={{ color: '#1a2e5a' }}>
                            SmarTest
                        </h1>
                        <p className="text-muted text-sm mt-2">Plateforme d'évaluation sécurisée</p>
                    </div>

                    {/* Titre */}
                    <div className="mb-8">
                        <h2 style={{ fontSize: '2rem', fontWeight: 700, color: 'var(--foreground)', marginBottom: '0.5rem' }}>
                            Créer un compte
                        </h2>
                        <p className="text-muted" style={{ fontSize: '0.875rem' }}>
                            Inscrivez-vous avec votre email académique !
                        </p>
                        <div className="mt-3 h-1 w-20 rounded-full" style={{ backgroundColor: '#1a2e5a' }} />
                    </div>

                    {/* Erreur */}
                    {error && (
                        <div className="mb-4 px-4 py-3 rounded-lg flex items-start gap-2 text-sm"
                            style={{
                                backgroundColor: '#fef2f2',
                                color: '#dc2626',
                                border: '1px solid #fecaca',
                            }}>
                            <AlertCircle size={16} className="shrink-0 mt-0.5" />
                            <span>{error}</span>
                        </div>
                    )}

                    {/* Formulaire */}
                    <form onSubmit={handleSubmit(onSubmit)} className="space-y-5" noValidate>

                        {/* Nom */}
                        <div>
                            <label htmlFor="nom" className="label">Nom complet</label>
                            <input
                                {...register('nom')}
                                id="nom"
                                type="text"
                                placeholder="Nom et prénom"
                                autoComplete="name"
                                disabled={isLoading}
                                className={`input ${errors.nom ? 'error' : ''}`}
                            />
                            {errors.nom && <p className="error-msg">{errors.nom.message}</p>}
                        </div>

                        {/* Email */}
                        <div>
                            <label htmlFor="email" className="label">Email </label>
                            <input
                                {...register('email')}
                                id="email"
                                type="email"
                                placeholder="votre.email@ump.ac.ma"
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
                                    placeholder="* * * * * * * * *"
                                    autoComplete="new-password"
                                    disabled={isLoading}
                                    onChange={(e) => {
                                        register('password').onChange(e)
                                        trigger('password')
                                    }}
                                    className={`input pr-10 ${errors.password ? 'error' : ''}`}
                                />
                                <button
                                    type="button"
                                    onClick={() => setShowPassword(p => !p)}
                                    className="absolute right-3 top-1/2 -translate-y-1/2"
                                    style={{ color: 'var(--muted-foreground)', background: 'none', border: 'none', cursor: 'pointer' }}
                                    tabIndex={-1}
                                    aria-label={showPassword ? 'Cacher' : 'Afficher'}
                                >
                                    {showPassword ? <EyeOff size={16} /> : <Eye size={16} />}
                                </button>
                            </div>
                            {errors.password && <p className="error-msg">{errors.password.message}</p>}
                            <PasswordRequirements password={passwordValue} />
                        </div>

                        {/* Confirm Password */}
                        <div>
                            <label htmlFor="confirmPassword" className="label">Confirmer le mot de passe</label>
                            <div className="relative">
                                <input
                                    {...register('confirmPassword')}
                                    id="confirmPassword"
                                    type={showConfirm ? 'text' : 'password'}
                                    placeholder="* * * * * * * * *"
                                    autoComplete="new-password"
                                    disabled={isLoading}
                                    onChange={(e) => {
                                        register('confirmPassword').onChange(e)
                                        trigger('confirmPassword')
                                    }}
                                    className={`input pr-10 ${errors.confirmPassword ? 'error' : ''}`}
                                />
                                <button
                                    type="button"
                                    onClick={() => setShowConfirm(p => !p)}
                                    className="absolute right-3 top-1/2 -translate-y-1/2"
                                    style={{ color: 'var(--muted-foreground)', background: 'none', border: 'none', cursor: 'pointer' }}
                                    tabIndex={-1}
                                    aria-label={showConfirm ? 'Cacher' : 'Afficher'}
                                >
                                    {showConfirm ? <EyeOff size={16} /> : <Eye size={16} />}
                                </button>
                            </div>
                            {errors.confirmPassword && <p className="error-msg">{errors.confirmPassword.message}</p>}
                        </div>

                        {/* Bouton */}
                        <button
                            type="submit"
                            disabled={isLoading}
                            className="btn-primary"
                        >
                            {isLoading
                                ? <span className="flex items-center justify-center gap-2">
                                    <Loader2 size={18} className="animate-spin" />
                                    Inscription en cours...
                                  </span>
                                : "S'inscrire"
                            }
                        </button>
                    </form>

                    {/* Séparateur */}
                    <div className="relative my-6">
                        <div className="absolute inset-0 flex items-center">
                            <div className="w-full border-t" style={{ borderColor: 'var(--border)' }} />
                        </div>
                        <div className="relative flex justify-center text-xs">
                            <span className="px-2 bg-background text-muted">ou</span>
                        </div>
                    </div>

                    {/* Lien login */}
                    <p className="text-center text-muted" style={{ fontSize: '0.875rem' }}>
                        Déjà un compte ?{' '}
                        <Link to="/login"
                            style={{ color: '#1a2e5a', fontWeight: 600, textDecoration: 'none' }}
                            onMouseEnter={e => e.currentTarget.style.textDecoration = 'underline'}
                            onMouseLeave={e => e.currentTarget.style.textDecoration = 'none'}
                        >
                            Se connecter
                        </Link>
                    </p>
                </div>
            </div>

            <style>{`
                @keyframes pulse {
                    0%, 100% { transform: scale(1); }
                    50% { transform: scale(1.05); }
                }
            `}</style>
        </main>
    )
}