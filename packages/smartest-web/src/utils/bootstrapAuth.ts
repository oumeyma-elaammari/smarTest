import axios from 'axios'
import { resolveHttpApiBase } from '../config/runtimeBackend'
import useAuth from '../hooks/useAuth'
import { readAccessToken } from './authSession'

/** Complète userId / JWT via refresh si la session locale est incomplète (anciens tokens). */
export async function ensureAuthUserIdFromRefresh(): Promise<void> {
    const token = readAccessToken()
    if (!token) return

    const { userId } = useAuth.getState()
    if (userId && userId !== '0') return

    try {
        const payload = token.split('.')[1]
        if (payload) {
            const decoded = JSON.parse(
                atob(payload.replaceAll('-', '+').replaceAll('_', '/')),
            ) as { userId?: unknown }
            const fromJwt = Number(decoded.userId)
            if (Number.isFinite(fromJwt) && fromJwt > 0) return
        }
    } catch {
        /* JWT illisible */
    }

    try {
        const { data } = await axios.post<{
            token: string
            role: string
            nom: string
            email: string
            userId?: number | null
        }>(`${resolveHttpApiBase()}/auth/refresh`, { token }, { timeout: 15_000 })
        if (data?.token?.trim()) {
            useAuth.getState().login({
                token: data.token.trim(),
                role: data.role,
                nom: data.nom,
                email: data.email,
                userId: data.userId ?? undefined,
            })
        }
    } catch {
        /* refresh impossible : l’utilisateur devra se reconnecter */
    }
}
