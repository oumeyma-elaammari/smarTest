import { Link } from 'react-router-dom'
import { Loader2, AlertCircle, CheckCircle, ArrowLeft, Eye, EyeOff, Mail } from 'lucide-react'

// ════════════════════════════════════════════════════
//  STYLES — objets React.CSSProperties
// ════════════════════════════════════════════════════

export const pageStyle: React.CSSProperties = {
    minHeight: '100vh',
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    background: '#f0f3f9',
    padding: '1.5rem',
    fontFamily: "'DM Sans', sans-serif",
}

export const cardStyle: React.CSSProperties = {
    background: '#fff',
    borderRadius: 20,
    border: '1px solid #e2e8f4',
    padding: '2.25rem 1.875rem',
    width: '100%',
    maxWidth: 400,
}

export const brandStyle: React.CSSProperties = {
    fontFamily: "'DM Serif Display', serif",
    fontSize: '1.6rem',
    color: '#0f1e3d',
    letterSpacing: '-0.02em',
    textAlign: 'center',
    marginBottom: 4,
}

export const brandSubStyle: React.CSSProperties = {
    fontSize: '0.68rem',
    color: '#8899b8',
    letterSpacing: '0.1em',
    textTransform: 'uppercase',
    textAlign: 'center',
    marginBottom: '1.5rem',
}

export const pageLabelStyle: React.CSSProperties = {
    display: 'inline-block',
    fontSize: '0.65rem',
    fontWeight: 600,
    letterSpacing: '0.1em',
    textTransform: 'uppercase',
    padding: '3px 12px',
    borderRadius: 20,
    background: '#e8eef8',
    color: '#1a2e5a',
    marginBottom: '0.75rem',
}

export const titleStyle: React.CSSProperties = {
    fontFamily: "'DM Serif Display', serif",
    fontSize: '1.35rem',
    color: '#0f1e3d',
    marginBottom: 6,
}

export const accentLine: React.CSSProperties = {
    width: 36,
    height: 2,
    background: '#1a2e5a',
    borderRadius: 2,
    margin: '0.6rem 0',
}

export const submitBtnStyle: React.CSSProperties = {
    width: '100%',
    height: 42,
    background: '#0f1e3d',
    color: '#fff',
    border: 'none',
    borderRadius: 9,
    fontFamily: "'DM Sans', sans-serif",
    fontSize: '0.875rem',
    fontWeight: 600,
    cursor: 'pointer',
    transition: 'opacity 0.15s',
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
}

export const linkStyle: React.CSSProperties = {
    color: '#1a2e5a',
    fontWeight: 600,
    fontSize: '0.8rem',
    textDecoration: 'none',
}

export const backLinkStyle: React.CSSProperties = {
    display: 'inline-flex',
    alignItems: 'center',
    gap: 5,
    fontSize: '0.78rem',
    color: '#1a2e5a',
    fontWeight: 600,
    textDecoration: 'none',
    padding: '6px 14px',
    borderRadius: 8,
    border: '1px solid #d4dce8',
    transition: 'background 0.15s',
}

export const footerStyle: React.CSSProperties = {
    marginTop: '1.25rem',
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    gap: 5,
    fontSize: '0.68rem',
    color: '#aab4cc',
}

export const dividerStyle: React.CSSProperties = {
    display: 'flex',
    alignItems: 'center',
    gap: '0.875rem',
    margin: '1.125rem 0',
    color: '#8899b8',
    fontSize: '0.75rem',
}

export const iconRingStyle = (color: string = '#e8eef8'): React.CSSProperties => ({
    width: 64,
    height: 64,
    borderRadius: '50%',
    background: color,
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    margin: '0 auto 1.25rem',
})

export function inputStyle(hasError: boolean): React.CSSProperties {
    return {
        width: '100%',
        height: 42,
        padding: '0 12px',
        border: `1.5px solid ${hasError ? '#dc2626' : '#d8e0f0'}`,
        borderRadius: 9,
        background: '#fff',
        color: '#0f1e3d',
        fontFamily: "'DM Sans', sans-serif",
        fontSize: '0.875rem',
        outline: 'none',
        boxShadow: hasError ? '0 0 0 3px rgba(220,38,38,0.08)' : 'none',
        transition: 'border-color 0.2s, box-shadow 0.2s',
    }
}

// ════════════════════════════════════════════════════
//  COMPOSANTS RÉUTILISABLES
// ════════════════════════════════════════════════════

export function Alert({ type, children }: {
    type: 'success' | 'error'
    children: React.ReactNode
}) {
    const ok = type === 'success'
    return (
        <div style={{
            display: 'flex', alignItems: 'flex-start', gap: 8,
            padding: '0.65rem 0.875rem', borderRadius: 8,
            background: ok ? '#eaf3de' : '#fcebeb',
            border: `1px solid ${ok ? '#c0dd97' : '#f7c1c1'}`,
            color: ok ? '#27500a' : '#791f1f',
            fontSize: '0.8rem', marginBottom: '0.875rem',
        }}>
            {ok
                ? <CheckCircle size={15} style={{ flexShrink: 0, marginTop: 2 }} />
                : <AlertCircle size={15} style={{ flexShrink: 0, marginTop: 2 }} />
            }
            <div>{children}</div>
        </div>
    )
}

export function Field({ label, error, children }: {
    label: string
    error?: string
    children: React.ReactNode
}) {
    return (
        <div style={{ display: 'flex', flexDirection: 'column', gap: '0.375rem' }}>
            <label style={{ fontSize: '0.78rem', fontWeight: 600, color: '#0f1e3d' }}>
                {label}
            </label>
            {children}
            {error && (
                <span style={{ fontSize: '0.72rem', color: '#dc2626', display: 'flex', alignItems: 'center', gap: 4 }}>
                    <AlertCircle size={11} /> {error}
                </span>
            )}
        </div>
    )
}

export function EyeBtn({ show, onClick }: { show: boolean; onClick: () => void }) {
    return (
        <button type="button" onClick={onClick} tabIndex={-1} style={{
            position: 'absolute', right: 10, top: '50%',
            transform: 'translateY(-50%)',
            background: 'none', border: 'none', cursor: 'pointer',
            color: '#8899b8', display: 'flex', alignItems: 'center',
            padding: 4, transition: 'color 0.15s',
        }}
            onMouseEnter={e => e.currentTarget.style.color = '#0f1e3d'}
            onMouseLeave={e => e.currentTarget.style.color = '#8899b8'}
        >
            {show ? <EyeOff size={15} /> : <Eye size={15} />}
        </button>
    )
}

export function BackLink({ to }: { to: string }) {
    return (
        <div style={{ textAlign: 'center', marginTop: '1.125rem' }}>
            <Link to={to} style={backLinkStyle}
                onMouseEnter={e => e.currentTarget.style.background = '#f0f3f9'}
                onMouseLeave={e => e.currentTarget.style.background = 'transparent'}
            >
                <ArrowLeft size={14} /> Retour à la connexion
            </Link>
        </div>
    )
}

export function NavLink({ to, children }: { to: string; children: React.ReactNode }) {
    return (
        <Link to={to} style={linkStyle}
            onMouseEnter={e => e.currentTarget.style.textDecoration = 'underline'}
            onMouseLeave={e => e.currentTarget.style.textDecoration = 'none'}
        >
            {children}
        </Link>
    )
}

export function Divider() {
    return (
        <div style={dividerStyle}>
            <div style={{ flex: 1, height: 1, background: '#e2e8f4' }} />
            ou
            <div style={{ flex: 1, height: 1, background: '#e2e8f4' }} />
        </div>
    )
}

export function Footer() {
    return (
        <div style={footerStyle}>
            <Mail size={11} /> SmarTest — ENSA Oujda
        </div>
    )
}
