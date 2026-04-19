import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Loader2, Mail } from 'lucide-react'
import api from '../api/axiosConfig'
import {
    pageStyle, cardStyle, brandStyle, brandSubStyle,
    inputStyle, submitBtnStyle,
    Alert, Field, BackLink, Footer,
} from '../styles/AuthStyles'

const schema = z.object({
    email: z.string().min(1, "L'email est obligatoire").email("Format email invalide"),
})
type ForgotForm = z.infer<typeof schema>

export default function ForgotPassword() {
    const [isLoading, setIsLoading] = useState(false)
    const [success, setSuccess]     = useState(false)
    const [error, setError]         = useState<string | null>(null)

    const { register, handleSubmit, formState: { errors } } = useForm<ForgotForm>({
        resolver: zodResolver(schema),
        mode: 'onSubmit',
    })

    const onSubmit = async (data: ForgotForm) => {
        setIsLoading(true)
        setError(null)
        try {
            await api.post('/auth/forgot-password/etudiant', { email: data.email.trim().toLowerCase() })
            setSuccess(true)
        } catch (err: any) {
            setError(!err?.response ? 'Impossible de contacter le serveur' : 'Une erreur est survenue. Réessayez.')
        } finally {
            setIsLoading(false)
        }
    }

    return (
        <main style={pageStyle}>
            <div style={cardStyle}>

                <h1 style={brandStyle}>SmarTest</h1>
                <p style={brandSubStyle}>Plateforme d'évaluation</p>

                <div style={{
                    width: 64, height: 64, borderRadius: '50%',
                    background: '#e8eef8', display: 'flex',
                    alignItems: 'center', justifyContent: 'center',
                    margin: '0 auto 1.25rem',
                }}>
                    <Mail size={28} color="#1a2e5a" strokeWidth={1.8} />
                </div>

                <div style={{ textAlign: 'center', marginBottom: '1.25rem' }}>
                    <h2 style={{ fontFamily: "'DM Serif Display', serif", fontSize: '1.35rem', color: '#0f1e3d', marginBottom: 6 }}>
                        Mot de passe oublié ?
                    </h2>
                    <div style={{ width: 36, height: 2, background: '#1a2e5a', borderRadius: 2, margin: '0.6rem auto 0.875rem' }} />
                    <p style={{ fontSize: '0.8rem', color: '#6b7a99', lineHeight: 1.6 }}>
                        Entrez votre email pour recevoir un lien de réinitialisation.
                    </p>
                </div>

                {success && (
                    <Alert type="success">
                        <strong>Email envoyé !</strong><br />
                        <span style={{ fontSize: '0.75rem' }}>Vérifiez votre boîte mail dans quelques instants.</span>
                    </Alert>
                )}
                {error && <Alert type="error">{error}</Alert>}

                {!success && (
                    <form onSubmit={handleSubmit(onSubmit)} noValidate style={{ display: 'flex', flexDirection: 'column', gap: '0.875rem' }}>
                        <Field label="Email" error={errors.email?.message}>
                            <input {...register('email')} type="email" placeholder="email@exemple.com"
                                autoComplete="email" disabled={isLoading} style={inputStyle(!!errors.email)} />
                        </Field>

                        <button type="submit" disabled={isLoading}
                            style={{ ...submitBtnStyle, opacity: isLoading ? 0.75 : 1 }}
                            onMouseEnter={e => { if (!isLoading) e.currentTarget.style.opacity = '0.88' }}
                            onMouseLeave={e => { e.currentTarget.style.opacity = '1' }}
                        >
                            {isLoading
                                ? <span style={{ display: 'flex', alignItems: 'center', justifyContent: 'center', gap: 6 }}>
                                    <Loader2 size={16} className="animate-spin" /> Envoi en cours...
                                  </span>
                                : 'Envoyer le lien'
                            }
                        </button>
                    </form>
                )}

                <BackLink to="/login" />
                <Footer />
            </div>
        </main>
    )
}