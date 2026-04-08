export  interface LoginRequest {
    email: string
    password: string
}

export  interface AuthResponse {
    token: string
    role: string
    nom: string
    email: string
}