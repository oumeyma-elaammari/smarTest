import { Navigate, useLocation } from 'react-router-dom'
import useAuth from '../hooks/useAuth'

interface PrivateRouteProps {
    children: React.ReactNode
    /** Par défaut : espace étudiant uniquement. */
    allowedRoles?: readonly ('ETUDIANT' | 'PROFESSEUR')[]
}

export default function PrivateRoute({
    children,
    allowedRoles = ['ETUDIANT'],
}: PrivateRouteProps) {
    const { isAuthenticated, role } = useAuth()
    const location = useLocation()

    if (!isAuthenticated) {
        const redirect = encodeURIComponent(`${location.pathname}${location.search}`)
        return <Navigate to={`/login?redirect=${redirect}`} replace />
    }

    const r = role ?? ''
    if (!allowedRoles.includes(r as 'ETUDIANT' | 'PROFESSEUR')) {
        if (r === 'ETUDIANT') return <Navigate to="/dashboard" replace />
        return <Navigate to="/login" replace />
    }

    return <>{children}</>
}