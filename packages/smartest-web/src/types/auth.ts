export  interface LoginRequest {
    email: string
    password: string
}

export interface AuthResponse {
    token: string
    role: string
    nom: string
    email: string
    /** Présent après login API récent (aligné sur le claim JWT `userId`). */
    userId?: number | null
}