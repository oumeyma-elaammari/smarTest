import { Navigate } from 'react-router-dom'
import useAuth from '../hooks/useAuth'

interface PrivateRouteProps {
    children: React.ReactNode
}

export default function PrivateRoute({ children }: PrivateRouteProps) {
    const { isAuthenticated, role } = useAuth()

    // Pas connecté → login
    if (!isAuthenticated) {
        return <Navigate to="/login" replace />
    }

    // Pas étudiant → login
    if (role !== 'ETUDIANT') {
        return <Navigate to="/login" replace />
    }

    return <>{children}</>
}