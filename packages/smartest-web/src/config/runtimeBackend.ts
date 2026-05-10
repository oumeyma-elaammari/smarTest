/**
 * Base HTTP pour Axios (sans slash final).
 * - Dev Vite : chaîne vide → URLs relatives, proxifiées vers le backend (évite CORS / OPTIONS bloqués).
 * - Tests Vitest : http://localhost:8081 (aligné MSW).
 * - Prod sans variable : même défaut qu’avant (backend local) ; déployer avec VITE_API_URL pour une API distante.
 */
export function resolveHttpApiBase(): string {
    const v = import.meta.env.VITE_API_URL as string | undefined
    if (v?.trim()) return v.trim().replace(/\/$/, '')
    if (import.meta.env.MODE === 'test') return 'http://localhost:8081'
    if (import.meta.env.DEV) return ''
    return 'http://localhost:8081'
}

/** Endpoint STOMP complet (ex. ws://hôte/ws). */
export function stompBrokerUrl(): string {
    const w = import.meta.env.VITE_WS_BASE_URL as string | undefined
    if (w?.trim()) return w.trim()
    const api = import.meta.env.VITE_API_URL as string | undefined
    if (api?.trim()) return `${api.trim().replace(/^http/, 'ws').replace(/\/$/, '')}/ws`
    if (import.meta.env.MODE === 'test') return 'ws://localhost:8081/ws'
    if (import.meta.env.DEV && typeof globalThis.location?.host === 'string' && globalThis.location.host) {
        const wsProto = globalThis.location.protocol === 'https:' ? 'wss:' : 'ws:'
        return `${wsProto}//${globalThis.location.host}/ws`
    }
    return 'ws://localhost:8081/ws'
}
