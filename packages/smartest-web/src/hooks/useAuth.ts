import { create } from 'zustand'

interface AuthResponse {
    token: string
    role: string
    nom: string
    email: string
}

interface AuthState {
    token: string | null
    role: string | null
    nom: string | null
    email: string | null
    isAuthenticated: boolean
    login: (data: AuthResponse) => void
    logout: () => void
}

const useAuth = create<AuthState>((set) => ({
    token: localStorage.getItem('token'),
    role: localStorage.getItem('role'),
    nom: localStorage.getItem('nom'),
    email: localStorage.getItem('email'),
    isAuthenticated: !!localStorage.getItem('token'),

    login: (data: AuthResponse) => {
        localStorage.setItem('token', data.token)
        localStorage.setItem('role', data.role)
        localStorage.setItem('nom', data.nom)
        localStorage.setItem('email', data.email)
        set({
            token: data.token,
            role: data.role,
            nom: data.nom,
            email: data.email,
            isAuthenticated: true,
        })
    },

    logout: () => {
        // Confirmation native du navigateur
        const confirmed = window.confirm('Êtes-vous sûr de vouloir vous déconnecter ?')

        if (!confirmed) return
        // Si l'utilisateur clique "Annuler" → rien ne se passe

        localStorage.clear()
        set({
            token: null,
            role: null,
            nom: null,
            email: null,
            isAuthenticated: false,
        })
        window.location.href = '/login'
    },
}))

export default useAuth