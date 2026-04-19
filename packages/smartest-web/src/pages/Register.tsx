import { useState } from 'react'
import { useNavigate, Link } from 'react-router-dom'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Loader2 } from 'lucide-react'
import api from '../api/axiosConfig'
import { inputStyle, submitBtnStyle, Alert, Field, EyeBtn } from '../styles/AuthStyles'

const registerSchema = z.object({
    nom: z
        .string()
        .min(1, "Le nom est obligatoire")
        .min(2, "Le nom doit contenir au moins 2 caractères")
        .max(50, "Le nom ne doit pas dépasser 50 caractères")
        .regex(/^[a-zA-ZÀ-ÿ\s\-']+$/, "Lettres, espaces, tirets ou apostrophes uniquement"),
    email: z
        .string()
        .min(1, "L'email est obligatoire")
        .email("Format email invalide")
        .regex(/^[a-zA-Z0-9._%+\-]+@ump\.ac\.ma$/, "Email académique @ump.ac.ma requis"),
    password: z
        .string()
        .min(1, "Le mot de passe est obligatoire")
        .min(8, "Au moins 8 caractères")
        .regex(/[A-Z]/, "Au moins une majuscule")
        .regex(/[0-9]/, "Au moins un chiffre")
        .regex(/[!@#$%^&*()_+\-=[\]{};':"\\|,.<>/?]/, "Au moins un caractère spécial"),
    confirmPassword: z.string().min(1, "La confirmation est obligatoire"),
}).refine(data => data.password === data.confirmPassword, {
    message: "Les mots de passe ne correspondent pas",
    path: ["confirmPassword"],
})
type RegisterForm = z.infer<typeof registerSchema>

const pwRequirements = [
    { label: "8 caractères",      test: (p: string) => p.length >= 8 },
    { label: "Majuscule",         test: (p: string) => /[A-Z]/.test(p) },
    { label: "Chiffre",           test: (p: string) => /[0-9]/.test(p) },
    { label: "Caractère spécial", test: (p: string) => /[!@#$%^&*()_+\-=[\]{};':"\\|,.<>/?]/.test(p) },
]

export default function Register() {
    const navigate = useNavigate()
    const [isLoading, setIsLoading]         = useState(false)
    const [showPassword, setShowPassword]   = useState(false)
    const [showConfirm, setShowConfirm]     = useState(false)
    const [error, setError]                 = useState<string | null>(null)
    const [passwordValue, setPasswordValue] = useState('')

    const { register, handleSubmit, formState: { errors } } = useForm<RegisterForm>({
        resolver: zodResolver(registerSchema),
        mode: 'onSubmit',
        reValidateMode: 'onSubmit',
    })

    const onSubmit = async (data: RegisterForm) => {
        setError(null)
        setIsLoading(true)
        try {
            await api.post('/auth/register/etudiant', {
                nom:             data.nom.trim(),
                email:           data.email.trim().toLowerCase(),
                password:        data.password,
                confirmPassword: data.confirmPassword,
            })
            navigate('/email-sent')
        } catch (err: any) {
            if (!err?.response)                   setError('Impossible de contacter le serveur.')
            else if (err.response.status === 409) setError('Cet email est déjà utilisé.')
            else if (err.response.status >= 500)  setError('Erreur serveur. Réessayez plus tard.')
            else                                  setError('Une erreur inattendue est survenue.')
            setIsLoading(false)
        }
    }

    return (
        <main style={{ display: 'flex', minHeight: '100vh', fontFamily: "'DM Sans', sans-serif" }}>

            {/* ── PANEL GAUCHE ── */}
            <div style={{
                width: '42%', flexShrink: 0, background: '#0f1e3d',
                display: 'flex', flexDirection: 'column',
                justifyContent: 'center', alignItems: 'center',
                padding: '3rem', position: 'relative', overflow: 'hidden',
            }}>
                <div style={{
                    position: 'absolute', inset: 0,
                    backgroundImage: 'linear-gradient(rgba(255,255,255,0.03) 1px, transparent 1px), linear-gradient(90deg, rgba(255,255,255,0.03) 1px, transparent 1px)',
                    backgroundSize: '40px 40px',
                }} />
                <div style={{ position: 'absolute', top: -80, right: -80, width: 320, height: 320, borderRadius: '50%', background: 'radial-gradient(circle, rgba(79,142,247,0.15) 0%, transparent 70%)' }} />
                <div style={{ position: 'absolute', bottom: -80, left: -60, width: 280, height: 280, borderRadius: '50%', background: 'radial-gradient(circle, rgba(201,168,76,0.07) 0%, transparent 70%)' }} />
                <div style={{ position: 'absolute', width: 360, height: 360, borderRadius: '50%', border: '1px solid rgba(255,255,255,0.06)', top: '50%', left: '50%', transform: 'translate(-50%, -50%)' }} />

                <div style={{ position: 'relative', zIndex: 2, textAlign: 'center', width: '100%' }}>
                    <div style={{
                        width: 72, height: 72, borderRadius: 20,
                        background: 'linear-gradient(135deg, #4f8ef7 0%, #2563eb 100%)',
                        display: 'flex', alignItems: 'center', justifyContent: 'center',
                        fontSize: '2rem', margin: '0 auto 1.75rem',
                        boxShadow: '0 8px 32px rgba(79,142,247,0.35)',
                    }}>🎓</div>

                    <h1 style={{ fontFamily: "'DM Serif Display', serif", fontSize: '2.8rem', color: '#fff', letterSpacing: '-0.02em', lineHeight: 1, marginBottom: '0.4rem' }}>
                        SmarTest
                    </h1>
                    <p style={{ color: 'rgba(255,255,255,0.45)', fontSize: '0.75rem', fontWeight: 300, letterSpacing: '0.1em', textTransform: 'uppercase', marginBottom: '2.5rem' }}>
                        Plateforme d'évaluation
                    </p>

                    {['Examens sécurisés en temps réel', 'Résultats et analyses instantanés', 'Interface simple et intuitive'].map((f, i) => (
                        <div key={i} style={{
                            display: 'flex', alignItems: 'center', gap: '0.875rem',
                            padding: '0.875rem 1.25rem', borderRadius: 12,
                            background: 'rgba(255,255,255,0.05)', border: '1px solid rgba(255,255,255,0.08)',
                            maxWidth: 280, margin: '0 auto 0.75rem', transition: 'background 0.2s',
                        }}
                            onMouseEnter={e => e.currentTarget.style.background = 'rgba(255,255,255,0.09)'}
                            onMouseLeave={e => e.currentTarget.style.background = 'rgba(255,255,255,0.05)'}
                        >
                            <div style={{ width: 8, height: 8, borderRadius: '50%', background: '#4f8ef7', flexShrink: 0, boxShadow: '0 0 8px #4f8ef7' }} />
                            <span style={{ color: 'rgba(255,255,255,0.8)', fontSize: '0.875rem' }}>{f}</span>
                        </div>
                    ))}
                </div>

                <p style={{ position: 'absolute', bottom: '1.5rem', color: 'rgba(255,255,255,0.18)', fontSize: '0.68rem', letterSpacing: '0.12em', textTransform: 'uppercase' }}>
                    Université Mohammed Premier · Oujda
                </p>
            </div>

            {/* ── PANEL DROIT ── */}
            <div style={{ flex: 1, display: 'flex', alignItems: 'center', justifyContent: 'center', padding: '2rem', background: '#fff', overflowY: 'auto', position: 'relative' }}>
                <div style={{ position: 'absolute', top: -100, right: -100, width: 400, height: 400, borderRadius: '50%', pointerEvents: 'none', background: 'radial-gradient(circle, rgba(79,142,247,0.06) 0%, transparent 70%)' }} />

                <div style={{ width: '100%', maxWidth: 440, position: 'relative' }}>

                    <div style={{ marginBottom: '1.75rem' }}>
                        <p style={{ fontSize: '0.7rem', fontWeight: 600, letterSpacing: '0.15em', textTransform: 'uppercase', color: '#4f8ef7', marginBottom: '0.5rem' }}>
                            Espace étudiant
                        </p>
                        <h2 style={{ fontFamily: "'DM Serif Display', serif", fontSize: '2.2rem', color: '#0f1e3d', lineHeight: 1.1, marginBottom: '0.5rem' }}>
                            Créer un compte
                        </h2>
                        <p style={{ color: '#6b7a99', fontSize: '0.875rem' }}>
                            Inscrivez-vous avec votre email académique{' '}
                            <strong style={{ color: '#0f1e3d' }}>@ump.ac.ma</strong>
                        </p>
                    </div>

                    {error && <Alert type="error">{error}</Alert>}

                    <form onSubmit={handleSubmit(onSubmit)} noValidate style={{ display: 'flex', flexDirection: 'column', gap: '1rem' }}>

                        <Field label="Nom complet" error={errors.nom?.message}>
                            <input {...register('nom')} type="text" placeholder="Nom et prénom"
                                autoComplete="name" disabled={isLoading} style={inputStyle(!!errors.nom)} />
                        </Field>

                        <Field label="Email académique" error={errors.email?.message}>
                            <input {...register('email')} type="email" placeholder="prenom.nom@ump.ac.ma"
                                autoComplete="email" disabled={isLoading} style={inputStyle(!!errors.email)} />
                        </Field>

                        <Field label="Mot de passe" error={errors.password?.message}>
                            <div style={{ position: 'relative' }}>
                                <input {...register('password')} type={showPassword ? 'text' : 'password'}
                                    placeholder="• • • • • • • •" autoComplete="new-password"
                                    disabled={isLoading}
                                    onChange={e => { register('password').onChange(e); setPasswordValue(e.target.value) }}
                                    style={{ ...inputStyle(!!errors.password), paddingRight: 42 }} />
                                <EyeBtn show={showPassword} onClick={() => setShowPassword(p => !p)} />
                            </div>
                            {passwordValue && (
                                <div style={{ marginTop: '0.5rem', padding: '0.625rem 0.875rem', background: '#f8fafd', border: '1px solid #d8e0f0', borderRadius: 8, display: 'flex', flexWrap: 'wrap', gap: '0.4rem 1rem' }}>
                                    {pwRequirements.map((req, i) => {
                                        const ok = req.test(passwordValue)
                                        return (
                                            <span key={i} style={{ fontSize: '0.72rem', display: 'flex', alignItems: 'center', gap: '0.3rem', color: ok ? '#16a34a' : '#94a3b8', transition: 'color 0.2s' }}>
                                                <span style={{ width: 6, height: 6, borderRadius: '50%', background: 'currentColor', flexShrink: 0 }} />
                                                {req.label}
                                            </span>
                                        )
                                    })}
                                </div>
                            )}
                        </Field>

                        <Field label="Confirmer le mot de passe" error={errors.confirmPassword?.message}>
                            <div style={{ position: 'relative' }}>
                                <input {...register('confirmPassword')} type={showConfirm ? 'text' : 'password'}
                                    placeholder="• • • • • • • •" autoComplete="new-password"
                                    disabled={isLoading}
                                    style={{ ...inputStyle(!!errors.confirmPassword), paddingRight: 42 }} />
                                <EyeBtn show={showConfirm} onClick={() => setShowConfirm(p => !p)} />
                            </div>
                        </Field>

                        <button type="submit" disabled={isLoading}
                            style={{ ...submitBtnStyle, height: 48, marginTop: '0.5rem', opacity: isLoading ? 0.75 : 1 }}
                            onMouseEnter={e => { if (!isLoading) e.currentTarget.style.opacity = '0.88' }}
                            onMouseLeave={e => { e.currentTarget.style.opacity = '1' }}
                        >
                            {isLoading
                                ? <span style={{ display: 'flex', alignItems: 'center', justifyContent: 'center', gap: 6 }}>
                                    <Loader2 size={18} className="animate-spin" /> Inscription en cours...
                                  </span>
                                : "S'inscrire"
                            }
                        </button>
                    </form>

                    <div style={{ display: 'flex', alignItems: 'center', gap: '1rem', margin: '1.25rem 0', color: '#6b7a99', fontSize: '0.78rem' }}>
                        <div style={{ flex: 1, height: 1, background: '#d8e0f0' }} />
                        ou
                        <div style={{ flex: 1, height: 1, background: '#d8e0f0' }} />
                    </div>

                    <p style={{ textAlign: 'center', fontSize: '0.875rem', color: '#6b7a99' }}>
                        Déjà un compte ?{' '}
                        <Link to="/login" style={{ color: '#0f1e3d', fontWeight: 600, textDecoration: 'none', borderBottom: '1.5px solid transparent', transition: 'border-color 0.15s' }}
                            onMouseEnter={e => e.currentTarget.style.borderBottomColor = '#0f1e3d'}
                            onMouseLeave={e => e.currentTarget.style.borderBottomColor = 'transparent'}
                        >
                            Se connecter
                        </Link>
                    </p>
                </div>
            </div>
        </main>
    )
}