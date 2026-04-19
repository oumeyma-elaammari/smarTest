import { useState } from 'react'
import { useNavigate, Link, useSearchParams } from 'react-router-dom'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Loader2 } from 'lucide-react'
import api from '../api/axiosConfig'
import useAuth from '../hooks/useAuth'
import {
    pageStyle, cardStyle, brandStyle, brandSubStyle,
    inputStyle, submitBtnStyle,
    Alert, Field, EyeBtn, Footer, NavLink,
} from '../styles/AuthStyles'

const loginSchema = z.object({
    email:    z.string().min(1, "L'email est obligatoire").email("Format email invalide"),
    password: z.string().min(1, "Le mot de passe est obligatoire").min(8, "Minimum 8 caractères"),
})
type LoginForm = z.infer<typeof loginSchema>

const ERR = {
    INVALID_CREDENTIALS: 'Email ou mot de passe incorrect',
    NOT_STUDENT:         "Ce compte n'est pas un compte étudiant",
    EMAIL_NOT_VERIFIED:  'Veuillez confirmer votre email avant de vous connecter',
    NETWORK_ERROR:       'Impossible de contacter le serveur',
    SERVER_ERROR:        'Erreur serveur. Réessayez plus tard',
    UNKNOWN:             'Une erreur inattendue est survenue',
} as const

export default function Login() {
    const navigate = useNavigate()
    const { login } = useAuth()
    const [searchParams]  = useSearchParams()
    const verified        = searchParams.get('verified')

    const [isLoading, setIsLoading]       = useState(false)
    const [showPassword, setShowPassword] = useState(false)
    const [loginError, setLoginError]     = useState<string | null>(null)

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
                setLoginError(ERR.NOT_STUDENT)
                return
            }
            setLoginError(null)
            login(authData)
            navigate('/dashboard')
        } catch (error: any) {
            setIsLoading(false)
            if (!error?.response) { setLoginError(ERR.NETWORK_ERROR); return }
            const s = error.response.status
            if (s === 401)      setLoginError(ERR.INVALID_CREDENTIALS)
            else if (s === 403) setLoginError(ERR.EMAIL_NOT_VERIFIED)
            else if (s >= 500)  setLoginError(ERR.SERVER_ERROR)
            else                setLoginError(ERR.UNKNOWN)
        }
    }

    return (
        <main style={pageStyle}>
            <div style={cardStyle}>

                <h1 style={brandStyle}>SmarTest</h1>
                <p style={brandSubStyle}>Plateforme d'évaluation</p>

                <div style={{ textAlign: 'center', marginBottom: '1.5rem' }}>
                    <h2 style={{ fontFamily: "'DM Serif Display', serif", fontSize: '1.35rem', color: '#0f1e3d', marginBottom: 6 }}>
                        Bon retour !
                    </h2>
                    <div style={{ width: 36, height: 2, background: '#1a2e5a', borderRadius: 2, margin: '0.6rem auto 0' }} />
                </div>

                {verified === 'true'  && <Alert type="success">Email confirmé ! Vous pouvez maintenant vous connecter.</Alert>}
                {verified === 'false' && <Alert type="error">Lien invalide ou expiré. Réinscrivez-vous.</Alert>}
                {loginError           && <Alert type="error">{loginError}</Alert>}

                <form onSubmit={handleSubmit(onSubmit)} noValidate style={{ display: 'flex', flexDirection: 'column', gap: '0.875rem' }}>
                    <Field label="Email" error={errors.email?.message}>
                        <input {...register('email')} type="email" placeholder="email@exemple.com"
                            autoComplete="email" disabled={isLoading} style={inputStyle(!!errors.email)} />
                    </Field>

                    <Field label="Mot de passe" error={errors.password?.message}>
                        <div style={{ position: 'relative' }}>
                            <input {...register('password')} type={showPassword ? 'text' : 'password'}
                                placeholder="• • • • • • • •" autoComplete="current-password"
                                disabled={isLoading} style={{ ...inputStyle(!!errors.password), paddingRight: 40 }} />
                            <EyeBtn show={showPassword} onClick={() => setShowPassword(p => !p)} />
                        </div>
                    </Field>

                    <button type="submit" disabled={isLoading}
                        style={{ ...submitBtnStyle, opacity: isLoading ? 0.75 : 1, marginTop: 4 }}
                        onMouseEnter={e => { if (!isLoading) e.currentTarget.style.opacity = '0.88' }}
                        onMouseLeave={e => { e.currentTarget.style.opacity = '1' }}
                    >
                        {isLoading
                            ? <span style={{ display: 'flex', alignItems: 'center', justifyContent: 'center', gap: 6 }}>
                                <Loader2 size={16} className="animate-spin" /> Connexion en cours...
                              </span>
                            : 'Se connecter'
                        }
                    </button>
                </form>

                <div style={{ textAlign: 'center', marginTop: '1.25rem', display: 'flex', flexDirection: 'column', gap: '0.5rem' }}>
                    <span style={{ fontSize: '0.8rem', color: '#6b7a99' }}>
                        Pas de compte ? <NavLink to="/register">S'inscrire</NavLink>
                    </span>
                    <NavLink to="/forgot-password">Mot de passe oublié ?</NavLink>
                </div>

                <Footer />
            </div>
        </main>
    )
}