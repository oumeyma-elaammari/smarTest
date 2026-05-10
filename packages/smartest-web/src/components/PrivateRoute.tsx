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
        /* Page réservée au professeur : ne pas renvoyer discrètement vers l’espace étudiant — forcer une reconnexion prof. */
        const professeurOnly =
            allowedRoles.length > 0 && allowedRoles.every((allowed) => allowed === 'PROFESSEUR')
        if (r === 'ETUDIANT' && professeurOnly) {
            const redirect = encodeURIComponent(`${location.pathname}${location.search}`)
            return <Navigate to={`/login?redirect=${redirect}&profSession=1`} replace />
        }
        if (r === 'ETUDIANT') return <Navigate to="/dashboard" replace />
        return <Navigate to="/login" replace />
    }

    return <>{children}</>
}