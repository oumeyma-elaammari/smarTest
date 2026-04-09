import { useState } from 'react'
import { Link } from 'react-router-dom'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Loader2, AlertCircle, CheckCircle, ArrowLeft, Mail } from 'lucide-react'
import api from '../api/axiosConfig'

const schema = z.object({
    email: z.string().min(1, "L'email est obligatoire").email("Format email invalide"),
})

type ForgotForm = z.infer<typeof schema>

export default function ForgotPassword() {
    const [isLoading, setIsLoading] = useState(false)
    const [success, setSuccess] = useState(false)
    const [error, setError] = useState<string | null>(null)

    const { register, handleSubmit, formState: { errors } } = useForm<ForgotForm>({
        resolver: zodResolver(schema),
        mode: 'onSubmit',
    })

    const onSubmit = async (data: ForgotForm) => {
        setIsLoading(true)
        setError(null)

        try {
            await api.post('/auth/forgot-password/etudiant', {
                email: data.email.trim().toLowerCase(),
            })
            setSuccess(true)
        } catch (err: any) {
            if (!err?.response) {
                setError('Impossible de contacter le serveur')
            } else {
                setError('Une erreur est survenue. Réessayez.')
            }
        } finally {
            setIsLoading(false)
        }
    }

    return (
        <main className="flex min-h-screen items-center justify-center bg-background p-4">
            <div className="card w-full max-w-[400px]">

                {/* Header */}
                <div className="text-center mb-6">
                    <div className="flex justify-center mb-4">
                        <div style={{
                            width: '56px', height: '56px', borderRadius: '50%',
                            backgroundColor: '#eff6ff',
                            display: 'flex', alignItems: 'center', justifyContent: 'center',
                        }}>
                            <Mail size={24} color="#1a2e5a" />
                        </div>
                    </div>
                    <h1 className="text-xl font-bold" style={{ color: '#1a2e5a' }}>
                        Mot de passe oublié ?
                    </h1>
                    <p className="text-muted mt-1" style={{ fontSize: '0.875rem' }}>
                        Entrez votre email pour recevoir un lien de réinitialisation
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
                            <p style={{ fontWeight: 600 }}>Email envoyé !</p>
                            <p style={{ fontSize: '0.8rem', marginTop: '0.25rem' }}>
                                Si cet email existe, vous recevrez un lien de réinitialisation dans quelques instants.
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

                        <button type="submit" disabled={isLoading} className="btn-primary">
                            {isLoading
                                ? <span className="flex items-center justify-center gap-2">
                                    <Loader2 size={16} className="animate-spin" />
                                    Envoi en cours...
                                  </span>
                                : 'Envoyer le lien'
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