import { create } from 'zustand'

interface AuthResponse {
    token: string
    role: string
    nom: string
    email: string
    userId?: number | null
}

interface AuthState {
    token: string | null
    role: string | null
    nom: string | null
    email: string | null
    userId: string | null
    isAuthenticated: boolean
    login: (data: AuthResponse) => void
    logout: () => void
    rehydrateFromStorage: () => void
    /** Déconnexion sans boîte de dialogue ni rechargement (ex. passer de l’espace étudiant à une page réservée au professeur). */
    clearSessionWithoutConfirm: () => void
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

const AUTH_KEYS = ['token', 'role', 'nom', 'email', 'userId'] as const

function userIdToLs(v: number | null | undefined): string | null {
    if (v == null || !Number.isFinite(v) || v <= 0) return null
    return String(Math.trunc(v))
}

const useAuth = create<AuthState>((set) => ({
    token: readLs('token'),
    role: readLs('role'),
    nom: readLs('nom'),
    email: readLs('email'),
    userId: readLs('userId'),
    isAuthenticated: !!readLs('token'),

    login: (data: AuthResponse) => {
        writeLs('token', data.token)
        writeLs('role', data.role)
        writeLs('nom', data.nom)
        writeLs('email', data.email)
        const uid = userIdToLs(data.userId ?? undefined)
        if (uid != null) writeLs('userId', uid)
        else {
            try {
                localStorage.removeItem('userId')
            } catch {
                /* ignore */
            }
        }
        set({
            token: data.token,
            role: data.role,
            nom: data.nom,
            email: data.email,
            userId: uid,
            isAuthenticated: true,
        })
    },

    logout: () => {
        const confirmed = globalThis.confirm('Êtes-vous sûr de vouloir vous déconnecter ?')
        if (confirmed) {
            if (!clearLs()) {
                removeLsKeys([...AUTH_KEYS])
            }

            set({
                token: null,
                role: null,
                nom: null,
                email: null,
                userId: null,
                isAuthenticated: false,
            })

            try {
                globalThis.location.href = '/login'
            } catch {
                /* navigate fallback si location inaccessible */
            }
        }
    },

    rehydrateFromStorage: () => {
        const token = readLs('token')
        set({
            token,
            role: readLs('role'),
            nom: readLs('nom'),
            email: readLs('email'),
            userId: readLs('userId'),
            isAuthenticated: !!token,
        })
    },

    clearSessionWithoutConfirm: () => {
        localStorage.removeItem('token')
        localStorage.removeItem('role')
        localStorage.removeItem('nom')
        localStorage.removeItem('email')
        localStorage.removeItem('userId')
        set({
            token: null,
            role: null,
            nom: null,
            email: null,
            userId: null,
            isAuthenticated: false,
        })
    },
}))

export default useAuth
