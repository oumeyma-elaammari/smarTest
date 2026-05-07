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
    rehydrateFromStorage: () => void
}

function readLs(key: string): string | null {
    try {
        return localStorage.getItem(key)
    } catch {
        return null
    }
}

function writeLs(key: string, value: string): boolean {
    try {
        localStorage.setItem(key, value)
        return true
    } catch {
        return false
    }
}

function clearLs(): boolean {
    try {
        localStorage.clear()
        return true
    } catch {
        return false
    }
}

function removeLsKeys(keys: string[]): void {
    for (const k of keys) {
        try {
            localStorage.removeItem(k)
        } catch {
            /* ignore */
        }
    }
}

const AUTH_KEYS = ['token', 'role', 'nom', 'email'] as const

const useAuth = create<AuthState>((set) => ({
    token: readLs('token'),
    role: readLs('role'),
    nom: readLs('nom'),
    email: readLs('email'),
    isAuthenticated: !!readLs('token'),

    login: (data: AuthResponse) => {
        writeLs('token', data.token)
        writeLs('role', data.role)
        writeLs('nom', data.nom)
        writeLs('email', data.email)
        set({
            token: data.token,
            role: data.role,
            nom: data.nom,
            email: data.email,
            isAuthenticated: true,
        })
    },

    logout: () => {
        const confirmed = window.confirm('Êtes-vous sûr de vouloir vous déconnecter ?')
        if (!confirmed) return

        if (!clearLs()) {
            removeLsKeys([...AUTH_KEYS])
        }

        set({
            token: null,
            role: null,
            nom: null,
            email: null,
            isAuthenticated: false,
        })

        try {
            window.location.href = '/login'
        } catch {
            /* navigate fallback si location inaccessible */
        }
    },

    rehydrateFromStorage: () => {
        const token = readLs('token')
        set({
            token,
            role: readLs('role'),
            nom: readLs('nom'),
            email: readLs('email'),
            isAuthenticated: !!token,
        })
    },
}))

export default useAuth
