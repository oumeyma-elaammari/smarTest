import { useState } from 'react'
import { useNavigate, Link, useSearchParams } from 'react-router-dom'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Loader2, AlertCircle } from 'lucide-react'
import api from '../api/axiosConfig'
import {
    pageStyle, cardStyle, brandStyle, brandSubStyle,
    inputStyle, submitBtnStyle,
    Alert, Field, EyeBtn, BackLink, Footer,
} from '../styles/AuthStyles'

const schema = z.object({
    newPassword: z
        .string()
        .min(8, "Minimum 8 caractères")
        .regex(/[A-Z]/, "Au moins une majuscule")
        .regex(/[0-9]/, "Au moins un chiffre")
        .regex(/[!@#$%^&*()_+\-=[\]{};':"\\|,.<>/?]/, "Au moins un caractère spécial"),
    confirmPassword: z.string().min(1, "La confirmation est obligatoire"),
}).refine(d => d.newPassword === d.confirmPassword, {
    message: "Les mots de passe ne correspondent pas",
    path: ["confirmPassword"],
})
type ResetForm = z.infer<typeof schema>

export default function ResetPassword() {
    const navigate = useNavigate()
    const [searchParams]  = useSearchParams()
    const token           = searchParams.get('token')

    const [isLoading, setIsLoading]       = useState(false)
    const [success, setSuccess]           = useState(false)
    const [error, setError]               = useState<string | null>(null)
    const [showPassword, setShowPassword] = useState(false)
    const [showConfirm, setShowConfirm]   = useState(false)

    const { register, handleSubmit, formState: { errors } } = useForm<ResetForm>({
        resolver: zodResolver(schema),
        mode: 'onSubmit',
    })

    // ── Token manquant ──
    if (!token) return (
        <main style={pageStyle}>
            <div style={{ ...cardStyle, textAlign: 'center' }}>
                <h1 style={brandStyle}>SmarTest</h1>
                <p style={brandSubStyle}>Plateforme d'évaluation</p>
                <div style={{
                    width: 56, height: 56, borderRadius: '50%',
                    background: '#fcebeb', display: 'flex',
                    alignItems: 'center', justifyContent: 'center',
                    margin: '1rem auto',
                }}>
                    <AlertCircle size={26} color="#a32d2d" />
                </div>
                <h2 style={{ fontFamily: "'DM Serif Display', serif", fontSize: '1.35rem', color: '#0f1e3d', marginBottom: 8 }}>
                    Lien invalide
                </h2>
                <p style={{ fontSize: '0.8rem', color: '#6b7a99', marginBottom: '1.25rem', lineHeight: 1.6 }}>
                    Ce lien de réinitialisation est invalide ou a expiré.
                </p>
                <Link to="/forgot-password" style={{
                    display: 'block', ...submitBtnStyle,
                    textDecoration: 'none', textAlign: 'center',
                    lineHeight: '42px',
                }}>
                    Demander un nouveau lien
                </Link>
                <BackLink to="/login" />
            </div>
        </main>
    )

    const onSubmit = async (data: ResetForm) => {
        setIsLoading(true)
        setError(null)
        try {
            await api.post('/auth/reset-password/etudiant', {
                token,
                newPassword:     data.newPassword,
                confirmPassword: data.confirmPassword,
            })
            setSuccess(true)
            setTimeout(() => navigate('/login'), 3000)
        } catch (err: any) {
            setIsLoading(false)
            if (!err?.response)               setError('Impossible de contacter le serveur')
            else if (err.response.status === 400) setError('Lien invalide ou expiré. Demandez un nouveau lien.')
            else if (err.response.status >= 500)  setError('Erreur serveur. Réessayez plus tard.')
            else                                  setError('Une erreur inattendue est survenue.')
        }
    }

    return (
        <main style={pageStyle}>
            <div style={cardStyle}>

                <h1 style={brandStyle}>SmarTest</h1>
                <p style={brandSubStyle}>Plateforme d'évaluation</p>

                <div style={{ textAlign: 'center', marginBottom: '1.25rem' }}>
                    <span style={{
                        display: 'inline-block', fontSize: '0.65rem', fontWeight: 600,
                        letterSpacing: '0.1em', textTransform: 'uppercase',
                        padding: '3px 12px', borderRadius: 20,
                        background: '#e8eef8', color: '#1a2e5a', marginBottom: '0.75rem',
                    }}>Sécurité</span>
                    <h2 style={{ fontFamily: "'DM Serif Display', serif", fontSize: '1.35rem', color: '#0f1e3d', marginBottom: 6 }}>
                        Nouveau mot de passe
                    </h2>
                    <div style={{ width: 36, height: 2, background: '#1a2e5a', borderRadius: 2, margin: '0.6rem auto 0.875rem' }} />
                    <p style={{ fontSize: '0.8rem', color: '#6b7a99', lineHeight: 1.6 }}>
                        Choisissez un nouveau mot de passe sécurisé.
                    </p>
                </div>

                {success && (
                    <Alert type="success">
                        <strong>Mot de passe réinitialisé !</strong><br />
                        <span style={{ fontSize: '0.75rem' }}>Redirection vers la connexion dans 3 secondes...</span>
                    </Alert>
                )}
                {error && <Alert type="error">{error}</Alert>}

                {!success && (
                    <form onSubmit={handleSubmit(onSubmit)} noValidate style={{ display: 'flex', flexDirection: 'column', gap: '0.875rem' }}>

                        <Field label="Nouveau mot de passe" error={errors.newPassword?.message}>
                            <div style={{ position: 'relative' }}>
                                <input {...register('newPassword')} type={showPassword ? 'text' : 'password'}
                                    placeholder="• • • • • • • •" disabled={isLoading}
                                    style={{ ...inputStyle(!!errors.newPassword), paddingRight: 40 }} />
                                <EyeBtn show={showPassword} onClick={() => setShowPassword(p => !p)} />
                            </div>
                        </Field>

                        <Field label="Confirmer le mot de passe" error={errors.confirmPassword?.message}>
                            <div style={{ position: 'relative' }}>
                                <input {...register('confirmPassword')} type={showConfirm ? 'text' : 'password'}
                                    placeholder="• • • • • • • •" disabled={isLoading}
                                    style={{ ...inputStyle(!!errors.confirmPassword), paddingRight: 40 }} />
                                <EyeBtn show={showConfirm} onClick={() => setShowConfirm(p => !p)} />
                            </div>
                        </Field>

                        <button type="submit" disabled={isLoading}
                            style={{ ...submitBtnStyle, opacity: isLoading ? 0.75 : 1 }}
                            onMouseEnter={e => { if (!isLoading) e.currentTarget.style.opacity = '0.88' }}
                            onMouseLeave={e => { e.currentTarget.style.opacity = '1' }}
                        >
                            {isLoading
                                ? <span style={{ display: 'flex', alignItems: 'center', justifyContent: 'center', gap: 6 }}>
                                    <Loader2 size={16} className="animate-spin" /> Réinitialisation...
                                  </span>
                                : 'Réinitialiser le mot de passe'
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