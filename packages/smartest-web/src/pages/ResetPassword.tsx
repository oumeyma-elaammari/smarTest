import { useState } from 'react'
import { useNavigate, useSearchParams, Link } from 'react-router-dom'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Eye, EyeOff, Loader2, AlertCircle, CheckCircle, ArrowLeft } from 'lucide-react'
import api from '../api/axiosConfig'

const schema = z.object({
    newPassword: z
        .string()
        .min(8, "Minimum 8 caractères")
        .regex(/[A-Z]/, "Doit contenir au moins une majuscule")
        .regex(/[0-9]/, "Doit contenir au moins un chiffre")
        .regex(/[!@#$%^&*()_+\-=[\]{};':"\\|,.<>/?]/, "Doit contenir au moins un caractère spécial"),
    confirmPassword: z.string().min(1, "La confirmation est obligatoire"),
}).refine(data => data.newPassword === data.confirmPassword, {
    message: "Les mots de passe ne correspondent pas",
    path: ["confirmPassword"],
})

type ResetForm = z.infer<typeof schema>

export default function ResetPassword() {
    const navigate = useNavigate()
    const [searchParams] = useSearchParams()
    const token = searchParams.get('token')

    const [isLoading, setIsLoading] = useState(false)
    const [success, setSuccess] = useState(false)
    const [error, setError] = useState<string | null>(null)
    const [showPassword, setShowPassword] = useState(false)
    const [showConfirm, setShowConfirm] = useState(false)

    const { register, handleSubmit, formState: { errors } } = useForm<ResetForm>({
        resolver: zodResolver(schema),
        mode: 'onSubmit',
    })

    // Token manquant
    if (!token) {
        return (
            <main className="flex min-h-screen items-center justify-center bg-background p-4">
                <div className="card w-full max-w-[400px] text-center">
                    <AlertCircle size={40} color="#dc2626" className="mx-auto mb-4" />
                    <h1 className="font-bold text-lg mb-2">Lien invalide</h1>
                    <p className="text-muted text-sm mb-4">
                        Ce lien de réinitialisation est invalide ou a expiré.
                    </p>
                    <Link to="/forgot-password" className="btn-primary"
                        style={{ display: 'inline-block', textDecoration: 'none' }}>
                        Demander un nouveau lien
                    </Link>
                </div>
            </main>
        )
    }

    const onSubmit = async (data: ResetForm) => {
        setIsLoading(true)
        setError(null)

        try {
            await api.post('/auth/reset-password/etudiant', {
                token,
                newPassword: data.newPassword,
                confirmPassword: data.confirmPassword,
            })

            setSuccess(true)
            setTimeout(() => navigate('/login'), 3000)

        } catch (err: any) {
            setIsLoading(false)

            if (!err?.response) {
                setError('Impossible de contacter le serveur')
                return
            }

            const status = err.response?.status
            if (status === 400) {
                setError('Lien invalide ou expiré. Demandez un nouveau lien.')
            } else if (status >= 500) {
                setError('Erreur serveur. Réessayez plus tard.')
            } else {
                setError('Une erreur inattendue est survenue.')
            }
        }
    }

    return (
        <main className="flex min-h-screen items-center justify-center bg-background p-4">
            <div className="card w-full max-w-[400px]">

                {/* Header */}
                <div className="text-center mb-6 space-y-1">
                    <h1 className="text-xl font-bold" style={{ color: '#1a2e5a' }}>
                        Nouveau mot de passe
                    </h1>
                    <p className="text-muted" style={{ fontSize: '0.875rem' }}>
                        Choisissez un nouveau mot de passe sécurisé
                    </p>
                </div>

                {/* Succès */}
                {success && (
                    <div className="mb-4 px-4 py-3 rounded-lg flex items-start gap-2 text-sm"
                        style={{
                            backgroundColor: '#f0fdf4',
                            color: '#16a34a',
                            border: '1px solid #bbf7d0',
                        }}>
                        <CheckCircle size={16} className="shrink-0 mt-0.5" />
                        <div>
                            <p style={{ fontWeight: 600 }}>Mot de passe réinitialisé !</p>
                            <p style={{ fontSize: '0.8rem', marginTop: '0.25rem' }}>
                                Redirection vers la connexion dans 3 secondes...
                            </p>
                        </div>
                    </div>
                )}

                {/* Erreur */}
                {error && (
                    <div className="mb-4 px-4 py-3 rounded-lg flex items-center gap-2 text-sm"
                        style={{
                            backgroundColor: '#fef2f2',
                            color: '#dc2626',
                            border: '1px solid #fecaca',
                        }}>
                        <AlertCircle size={16} className="shrink-0" />
                        {error}
                    </div>
                )}

                {/* Formulaire */}
                {!success && (
                    <form onSubmit={handleSubmit(onSubmit)} className="space-y-4" noValidate>

                        {/* Nouveau mot de passe */}
                        <div>
                            <label htmlFor="newPassword" className="label">
                                Nouveau mot de passe
                            </label>
                            <div className="relative">
                                <input
                                    {...register('newPassword')}
                                    id="newPassword"
                                    type={showPassword ? 'text' : 'password'}
                                    placeholder="* * * * * * * * *"
                                    disabled={isLoading}
                                    className={`input pr-10 ${errors.newPassword ? 'error' : ''}`}
                                />
                                <button
                                    type="button"
                                    onClick={() => setShowPassword(p => !p)}
                                    className="absolute right-3 top-1/2 -translate-y-1/2"
                                    style={{ color: 'var(--muted-foreground)', background: 'none', border: 'none', cursor: 'pointer' }}
                                    tabIndex={-1}
                                >
                                    {showPassword ? <EyeOff size={16} /> : <Eye size={16} />}
                                </button>
                            </div>
                            {errors.newPassword && (
                                <p className="error-msg">{errors.newPassword.message}</p>
                            )}
                        </div>

                        {/* Confirmer mot de passe */}
                        <div>
                            <label htmlFor="confirmPassword" className="label">
                                Confirmer le mot de passe
                            </label>
                            <div className="relative">
                                <input
                                    {...register('confirmPassword')}
                                    id="confirmPassword"
                                    type={showConfirm ? 'text' : 'password'}
                                    placeholder="* * * * * * * * *"
                                    disabled={isLoading}
                                    className={`input pr-10 ${errors.confirmPassword ? 'error' : ''}`}
                                />
                                <button
                                    type="button"
                                    onClick={() => setShowConfirm(p => !p)}
                                    className="absolute right-3 top-1/2 -translate-y-1/2"
                                    style={{ color: 'var(--muted-foreground)', background: 'none', border: 'none', cursor: 'pointer' }}
                                    tabIndex={-1}
                                >
                                    {showConfirm ? <EyeOff size={16} /> : <Eye size={16} />}
                                </button>
                            </div>
                            {errors.confirmPassword && (
                                <p className="error-msg">{errors.confirmPassword.message}</p>
                            )}
                        </div>

                        <button type="submit" disabled={isLoading} className="btn-primary">
                            {isLoading
                                ? <span className="flex items-center justify-center gap-2">
                                    <Loader2 size={16} className="animate-spin" />
                                    Réinitialisation...
                                  </span>
                                : 'Réinitialiser le mot de passe'
                            }
                        </button>
                    </form>
                )}

                {/* Retour */}
                <div className="mt-5 text-center">
                    <Link to="/login"
                        style={{
                            display: 'inline-flex', alignItems: 'center', gap: '0.4rem',
                            fontSize: '0.875rem', color: '#1a2e5a',
                            fontWeight: 600, textDecoration: 'none',
                        }}
                        onMouseEnter={e => e.currentTarget.style.textDecoration = 'underline'}
                        onMouseLeave={e => e.currentTarget.style.textDecoration = 'none'}
                    >
                        <ArrowLeft size={16} />
                        Retour à la connexion
                    </Link>
                </div>
            </div>
        </main>
    )
}