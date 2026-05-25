import { useState, useEffect } from 'react'
import { useNavigate, Link } from 'react-router-dom'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Loader2, CheckCircle2, Circle, Eye, EyeOff, ArrowRight, Shield, Zap, BarChart3 } from 'lucide-react'
import api from '../api/axiosConfig'

/* ─── Validation ─────────────────────────────────────────── */
const registerSchema = z.object({
    nom: z
        .string()
        .min(1, "Le nom est obligatoire")
        .min(2, "Au moins 2 caractères")
        .max(50, "50 caractères max")
        .regex(/^[a-zA-ZÀ-ÿ\s'-]+$/, "Lettres, espaces, tirets ou apostrophes uniquement"),
    email: z
        .string()
        .min(1, "L'email est obligatoire")
        .pipe(z.email("Format email invalide"))
        .refine((v) => /^[a-zA-Z0-9._%+-]+@ump\.ac\.ma$/.test(v), "Email académique @ump.ac.ma requis"),
    password: z
        .string()
        .min(1, "Le mot de passe est obligatoire")
        .min(8, "Au moins 8 caractères")
        .regex(/[A-Z]/, "Au moins une majuscule")
        .regex(/\d/, "Au moins un chiffre")
        .regex(/[!@#$%^&*()_+\-=[\]{};':"\\|,.<>/?]/, "Au moins un caractère spécial"),
    confirmPassword: z.string().min(1, "La confirmation est obligatoire"),
}).refine(d => d.password === d.confirmPassword, {
    message: "Les mots de passe ne correspondent pas",
    path: ["confirmPassword"],
})
type RegisterForm = z.infer<typeof registerSchema>

const pwRequirements = [
    { label: "8 caractères",      test: (p: string) => p.length >= 8 },
    { label: "Majuscule",         test: (p: string) => /[A-Z]/.test(p) },
    { label: "Chiffre",           test: (p: string) => /\d/.test(p) },
    { label: "Caractère spécial", test: (p: string) => /[!@#$%^&*()_+\-=[\]{};':"\\|,.<>/?]/.test(p) },
]

const features = [
    { icon: Shield,   label: "Examens sécurisés en temps réel" },
    { icon: BarChart3,label: "Résultats et analyses instantanés" },
    { icon: Zap,      label: "Interface simple et intuitive"   },
]

/* ─── Styles ─────────────────────────────────────────────── */
const css = `
@import url('https://fonts.googleapis.com/css2?family=Sora:wght@300;400;500;600;700&family=Playfair+Display:ital,wght@0,700;1,400&display=swap');

*, *::before, *::after { box-sizing: border-box; margin: 0; padding: 0; }

:root {
  --navy:        #0b1730;
  --navy-mid:    #0f1e3d;
  --navy-light:  #1a2f5a;
  --blue:        #3b82f6;
  --sky:         #38b6ff;
  --sky-light:   #e8f4ff;
  --white:       #ffffff;
  --gray-50:     #f8fafc;
  --gray-100:    #f1f5f9;
  --gray-200:    #e2e8f0;
  --gray-400:    #94a3b8;
  --gray-600:    #64748b;
  --gray-800:    #1e293b;
  --success:     #10b981;
  --error:       #ef4444;
  --error-bg:    #fef2f2;
  --error-border:#fecaca;
  --radius-sm:   8px;
  --radius-md:   12px;
  --radius-lg:   16px;
  --radius-xl:   24px;
  --shadow-sm:   0 1px 3px rgba(0,0,0,.08), 0 1px 2px rgba(0,0,0,.05);
  --shadow-md:   0 4px 16px rgba(0,0,0,.08);
  --shadow-lg:   0 8px 32px rgba(0,0,0,.12);
  --shadow-blue: 0 4px 20px rgba(59,130,246,.25);
  --transition:  all .2s ease;
}

body { font-family: 'Sora', sans-serif; }

.register-root {
  display: flex;
  min-height: 100vh;
  background: var(--gray-50);
}

/* ── LEFT PANEL ── */
.left-panel {
  width: 420px;
  flex-shrink: 0;
  background: var(--navy);
  display: flex;
  flex-direction: column;
  padding: 3rem 2.5rem;
  position: relative;
  overflow: hidden;
}

.left-grid {
  position: absolute;
  inset: 0;
  background-image:
    linear-gradient(rgba(255,255,255,.025) 1px, transparent 1px),
    linear-gradient(90deg, rgba(255,255,255,.025) 1px, transparent 1px);
  background-size: 36px 36px;
}

.left-glow-1 {
  position: absolute;
  top: -120px; right: -120px;
  width: 400px; height: 400px;
  border-radius: 50%;
  background: radial-gradient(circle, rgba(59,130,246,.18) 0%, transparent 70%);
  pointer-events: none;
}
.left-glow-2 {
  position: absolute;
  bottom: -80px; left: -80px;
  width: 320px; height: 320px;
  border-radius: 50%;
  background: radial-gradient(circle, rgba(56,182,255,.1) 0%, transparent 70%);
  pointer-events: none;
}

.left-content {
  position: relative;
  z-index: 2;
  display: flex;
  flex-direction: column;
  flex: 1;
}

.logo-wrap {
  display: flex;
  align-items: center;
  gap: .75rem;
  margin-bottom: 3rem;
}

.logo-img {
  height: 36px;
  width: auto;
  object-fit: contain;
}

.left-hero {
  flex: 1;
  display: flex;
  flex-direction: column;
  justify-content: center;
}

.left-tagline {
  font-size: .65rem;
  font-weight: 600;
  letter-spacing: .18em;
  text-transform: uppercase;
  color: var(--sky);
  margin-bottom: .75rem;
}

.left-title {
  font-family: 'Playfair Display', serif;
  font-size: 2.6rem;
  font-weight: 700;
  line-height: 1.1;
  color: var(--white);
  margin-bottom: .75rem;
}

.left-title em {
  font-style: italic;
  color: var(--sky);
}

.left-subtitle {
  color: rgba(255,255,255,.45);
  font-size: .825rem;
  font-weight: 300;
  line-height: 1.7;
  margin-bottom: 2.5rem;
  max-width: 300px;
}

.feature-list {
  list-style: none;
  display: flex;
  flex-direction: column;
  gap: .625rem;
}

.feature-item {
  display: flex;
  align-items: center;
  gap: .875rem;
  padding: .875rem 1rem;
  border-radius: var(--radius-md);
  background: rgba(255,255,255,.05);
  border: 1px solid rgba(255,255,255,.07);
  transition: var(--transition);
  cursor: default;
}

.feature-item:hover {
  background: rgba(255,255,255,.09);
  border-color: rgba(56,182,255,.2);
  transform: translateX(3px);
}

.feature-icon {
  width: 32px;
  height: 32px;
  border-radius: var(--radius-sm);
  background: rgba(56,182,255,.12);
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
}

.feature-label {
  color: rgba(255,255,255,.8);
  font-size: .825rem;
  font-weight: 400;
}

.left-footer {
  margin-top: 2.5rem;
  padding-top: 1.5rem;
  border-top: 1px solid rgba(255,255,255,.08);
  color: rgba(255,255,255,.2);
  font-size: .65rem;
  letter-spacing: .1em;
  text-transform: uppercase;
}

/* ── RIGHT PANEL ── */
.right-panel {
  flex: 1;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 2rem;
  background: var(--white);
  overflow-y: auto;
  position: relative;
}

.right-glow {
  position: absolute;
  top: -150px; right: -150px;
  width: 500px; height: 500px;
  border-radius: 50%;
  background: radial-gradient(circle, rgba(59,130,246,.05) 0%, transparent 70%);
  pointer-events: none;
}

.form-card {
  width: 100%;
  max-width: 460px;
  position: relative;
}

/* Header */
.form-header {
  margin-bottom: 2rem;
}

.form-badge {
  display: inline-flex;
  align-items: center;
  gap: .4rem;
  padding: .3rem .75rem;
  border-radius: 999px;
  background: var(--sky-light);
  border: 1px solid rgba(56,182,255,.2);
  font-size: .65rem;
  font-weight: 700;
  letter-spacing: .12em;
  text-transform: uppercase;
  color: #0284c7;
  margin-bottom: .875rem;
}

.form-title {
  font-family: 'Playfair Display', serif;
  font-size: 2rem;
  font-weight: 700;
  color: var(--navy);
  line-height: 1.15;
  margin-bottom: .5rem;
}

.form-desc {
  color: var(--gray-600);
  font-size: .85rem;
  line-height: 1.6;
}

.form-desc strong { color: var(--navy); font-weight: 600; }

/* Alert */
.alert {
  display: flex;
  align-items: flex-start;
  gap: .625rem;
  padding: .875rem 1rem;
  border-radius: var(--radius-md);
  font-size: .825rem;
  margin-bottom: 1.25rem;
  line-height: 1.5;
}

.alert-error {
  background: var(--error-bg);
  border: 1px solid var(--error-border);
  color: #b91c1c;
}

.alert-dot {
  width: 6px;
  height: 6px;
  border-radius: 50%;
  background: currentColor;
  margin-top: .35rem;
  flex-shrink: 0;
}

/* Form */
.form-body {
  display: flex;
  flex-direction: column;
  gap: 1.1rem;
}

.field-wrap {
  display: flex;
  flex-direction: column;
  gap: .375rem;
}

.field-label {
  font-size: .75rem;
  font-weight: 600;
  color: var(--gray-800);
  letter-spacing: .02em;
}

.field-input {
  width: 100%;
  height: 46px;
  padding: 0 .875rem;
  border-radius: var(--radius-md);
  border: 1.5px solid var(--gray-200);
  background: var(--gray-50);
  font-family: 'Sora', sans-serif;
  font-size: .875rem;
  color: var(--gray-800);
  transition: var(--transition);
  outline: none;
}

.field-input::placeholder { color: var(--gray-400); }

.field-input:focus {
  border-color: var(--blue);
  background: var(--white);
  box-shadow: 0 0 0 3px rgba(59,130,246,.1);
}

.field-input.has-error {
  border-color: var(--error);
  background: var(--error-bg);
}

.field-input.has-error:focus {
  box-shadow: 0 0 0 3px rgba(239,68,68,.1);
}

.field-error {
  font-size: .72rem;
  color: var(--error);
  display: flex;
  align-items: center;
  gap: .3rem;
}

.input-wrap { position: relative; }

.eye-btn {
  position: absolute;
  right: .75rem;
  top: 50%;
  transform: translateY(-50%);
  background: none;
  border: none;
  cursor: pointer;
  color: var(--gray-400);
  display: flex;
  align-items: center;
  padding: .25rem;
  border-radius: var(--radius-sm);
  transition: var(--transition);
}

.eye-btn:hover { color: var(--gray-800); }

/* Password strength */
.pw-strength {
  margin-top: .5rem;
  padding: .75rem;
  background: var(--gray-50);
  border: 1px solid var(--gray-200);
  border-radius: var(--radius-md);
  display: flex;
  flex-wrap: wrap;
  gap: .375rem .875rem;
}

.pw-req {
  display: flex;
  align-items: center;
  gap: .35rem;
  font-size: .7rem;
  transition: color .2s;
}

.pw-req.ok  { color: var(--success); }
.pw-req.nok { color: var(--gray-400); }

/* Submit */
.submit-btn {
  width: 100%;
  height: 50px;
  margin-top: .5rem;
  border: none;
  border-radius: var(--radius-md);
  background: var(--navy);
  color: var(--white);
  font-family: 'Sora', sans-serif;
  font-size: .875rem;
  font-weight: 600;
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: .5rem;
  letter-spacing: .02em;
  transition: var(--transition);
  position: relative;
  overflow: hidden;
}

.submit-btn::after {
  content: '';
  position: absolute;
  inset: 0;
  background: linear-gradient(135deg, rgba(255,255,255,.06) 0%, transparent 100%);
}

.submit-btn:hover:not(:disabled) {
  background: var(--navy-light);
  box-shadow: var(--shadow-blue);
  transform: translateY(-1px);
}

.submit-btn:active:not(:disabled) { transform: translateY(0); }
.submit-btn:disabled { opacity: .65; cursor: not-allowed; }

/* Divider */
.divider {
  display: flex;
  align-items: center;
  gap: 1rem;
  margin: 1.25rem 0;
  color: var(--gray-400);
  font-size: .75rem;
}

.divider-line {
  flex: 1;
  height: 1px;
  background: var(--gray-200);
}

/* Footer */
.form-footer {
  text-align: center;
  font-size: .85rem;
  color: var(--gray-600);
}

.form-footer a {
  color: var(--navy);
  font-weight: 600;
  text-decoration: none;
  border-bottom: 1.5px solid transparent;
  transition: border-color .15s;
}

.form-footer a:hover { border-bottom-color: var(--navy); }

/* Progress bar */
.pw-bar-wrap {
  display: flex;
  gap: 3px;
  margin-top: .375rem;
}

.pw-bar-seg {
  flex: 1;
  height: 3px;
  border-radius: 999px;
  background: var(--gray-200);
  transition: background .3s;
}

.pw-bar-seg.active-1 { background: var(--error); }
.pw-bar-seg.active-2 { background: #f97316; }
.pw-bar-seg.active-3 { background: #eab308; }
.pw-bar-seg.active-4 { background: var(--success); }

/* Responsive */
@media (max-width: 768px) {
  .left-panel { display: none; }
}
`

export default function Register() {
    const navigate = useNavigate()
    const [isLoading, setIsLoading]         = useState(false)
    const [showPassword, setShowPassword]   = useState(false)
    const [showConfirm, setShowConfirm]     = useState(false)
    const [error, setError]                 = useState<string | null>(null)
    const [passwordValue, setPasswordValue] = useState('')

    useEffect(() => {
        const style = document.createElement('style')
        style.textContent = css
        document.head.appendChild(style)
        return () => { document.head.removeChild(style) }
    }, [])

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
            navigate('/email-sent', { state: { email: data.email.trim().toLowerCase() } })
        } catch (err: unknown) {
            const axiosErr = err as { response?: { status: number } }
            setIsLoading(false)
            if (axiosErr?.response) {
                if (axiosErr.response.status === 403) {
                    navigate('/email-sent', { state: { email: data.email.trim().toLowerCase() } })
                    return
                }
                if (axiosErr.response.status === 409)      setError('Cet email est déjà utilisé.')
                else if (axiosErr.response.status >= 500)  setError('Erreur serveur. Réessayez plus tard.')
                else                                       setError('Une erreur inattendue est survenue.')
            } else {
                setError('Impossible de contacter le serveur.')
            }
        }
    }

    const pwScore = pwRequirements.filter(r => r.test(passwordValue)).length

    const inputCls = (hasErr: boolean) =>
        `field-input${hasErr ? ' has-error' : ''}`

    return (
        <main className="register-root">

            {/* ── LEFT PANEL ── */}
            <aside className="left-panel">
                <div className="left-grid" />
                <div className="left-glow-1" />
                <div className="left-glow-2" />

                <div className="left-content">
                    
                    {/* Hero */}
                    <div className="left-hero">
                        <p className="left-tagline">Espace étudiant</p>
                        <h1 className="left-title">
                            SmarTest
                        </h1>
                        <p className="left-subtitle">
                            Une plateforme académique sécurisée pour passer vos examens et suivre vos résultats.
                        </p>

                        <ul className="feature-list">
                            {features.map(({ icon: Icon, label }) => (
                                <li key={label} className="feature-item">
                                    <div className="feature-icon">
                                        <Icon size={15} color="#38b6ff" strokeWidth={2} />
                                    </div>
                                    <span className="feature-label">{label}</span>
                                </li>
                            ))}
                        </ul>
                    </div>

                    <div className="left-footer">
                        Université Mohammed Premier · Oujda
                    </div>
                </div>
            </aside>

            {/* ── RIGHT PANEL ── */}
            <section className="right-panel">
                <div className="right-glow" />

                <div className="form-card">

                    {/* Header */}
                    <div className="form-header">
                        <div className="form-badge">
                            <span style={{ width: 5, height: 5, borderRadius: '50%', background: 'currentColor' }} />
                            Nouveau compte
                        </div>
                        <h2 className="form-title">Créer votre compte</h2>
                        <p className="form-desc">
                            Inscrivez-vous avec votre email académique{' '}
                            <strong>@ump.ac.ma</strong> pour accéder à la plateforme.
                        </p>
                    </div>

                    {/* Error */}
                    {error && (
                        <div className="alert alert-error">
                            <span className="alert-dot" />
                            {error}
                        </div>
                    )}

                    {/* Form */}
                    <form onSubmit={handleSubmit(onSubmit)} noValidate className="form-body">

                        {/* Nom */}
                        <div className="field-wrap">
                            <label className="field-label">Nom complet</label>
                            <input
                                {...register('nom')}
                                type="text"
                                placeholder="Nom et prénom"
                                autoComplete="name"
                                disabled={isLoading}
                                className={inputCls(!!errors.nom)}
                            />
                            {errors.nom && (
                                <span className="field-error">
                                    <span style={{ width: 4, height: 4, borderRadius: '50%', background: 'currentColor', flexShrink: 0 }} />
                                    {errors.nom.message}
                                </span>
                            )}
                        </div>

                        {/* Email */}
                        <div className="field-wrap">
                            <label className="field-label">Email académique</label>
                            <input
                                {...register('email')}
                                type="email"
                                placeholder="prenom.nom@ump.ac.ma"
                                autoComplete="email"
                                disabled={isLoading}
                                className={inputCls(!!errors.email)}
                            />
                            {errors.email && (
                                <span className="field-error">
                                    <span style={{ width: 4, height: 4, borderRadius: '50%', background: 'currentColor', flexShrink: 0 }} />
                                    {errors.email.message}
                                </span>
                            )}
                        </div>

                        {/* Password */}
                        <div className="field-wrap">
                            <label className="field-label">Mot de passe</label>
                            <div className="input-wrap">
                                <input
                                    {...register('password')}
                                    type={showPassword ? 'text' : 'password'}
                                    placeholder="• • • • • • • •"
                                    autoComplete="new-password"
                                    disabled={isLoading}
                                    onChange={e => { register('password').onChange(e); setPasswordValue(e.target.value) }}
                                    className={inputCls(!!errors.password)}
                                    style={{ paddingRight: 42 }}
                                />
                                <button
                                    type="button"
                                    className="eye-btn"
                                    onClick={() => setShowPassword(p => !p)}
                                    tabIndex={-1}
                                >
                                    {showPassword ? <EyeOff size={16} /> : <Eye size={16} />}
                                </button>
                            </div>

                            {/* Strength bar */}
                            {passwordValue && (
                                <div className="pw-bar-wrap">
                                    {[0, 1, 2, 3].map(i => (
                                        <div
                                            key={i}
                                            className={`pw-bar-seg${i < pwScore ? ` active-${pwScore}` : ''}`}
                                        />
                                    ))}
                                </div>
                            )}

                            {errors.password && (
                                <span className="field-error">
                                    <span style={{ width: 4, height: 4, borderRadius: '50%', background: 'currentColor', flexShrink: 0 }} />
                                    {errors.password.message}
                                </span>
                            )}

                            {/* Requirements */}
                            {passwordValue && (
                                <div className="pw-strength">
                                    {pwRequirements.map(req => {
                                        const ok = req.test(passwordValue)
                                        return (
                                            <span key={req.label} className={`pw-req ${ok ? 'ok' : 'nok'}`}>
                                                {ok
                                                    ? <CheckCircle2 size={11} strokeWidth={2.5} />
                                                    : <Circle size={11} strokeWidth={2} />
                                                }
                                                {req.label}
                                            </span>
                                        )
                                    })}
                                </div>
                            )}
                        </div>

                        {/* Confirm */}
                        <div className="field-wrap">
                            <label className="field-label">Confirmer le mot de passe</label>
                            <div className="input-wrap">
                                <input
                                    {...register('confirmPassword')}
                                    type={showConfirm ? 'text' : 'password'}
                                    placeholder="• • • • • • • •"
                                    autoComplete="new-password"
                                    disabled={isLoading}
                                    className={inputCls(!!errors.confirmPassword)}
                                    style={{ paddingRight: 42 }}
                                />
                                <button
                                    type="button"
                                    className="eye-btn"
                                    onClick={() => setShowConfirm(p => !p)}
                                    tabIndex={-1}
                                >
                                    {showConfirm ? <EyeOff size={16} /> : <Eye size={16} />}
                                </button>
                            </div>
                            {errors.confirmPassword && (
                                <span className="field-error">
                                    <span style={{ width: 4, height: 4, borderRadius: '50%', background: 'currentColor', flexShrink: 0 }} />
                                    {errors.confirmPassword.message}
                                </span>
                            )}
                        </div>

                        {/* Submit */}
                        <button
                            type="submit"
                            disabled={isLoading}
                            className="submit-btn"
                        >
                            {isLoading ? (
                                <>
                                    <Loader2 size={17} style={{ animation: 'spin 1s linear infinite' }} />
                                    Inscription en cours…
                                </>
                            ) : (
                                <>
                                    Créer mon compte
                                    <ArrowRight size={16} strokeWidth={2.5} />
                                </>
                            )}
                        </button>
                    </form>

                    <div className="divider">
                        <div className="divider-line" />
                        ou
                        <div className="divider-line" />
                    </div>

                    <p className="form-footer">
                        Déjà un compte ?{' '}
                        <Link to="/login">Se connecter</Link>
                    </p>
                </div>
            </section>
        </main>
    )
}