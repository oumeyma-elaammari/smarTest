import axios from 'axios'

export function joinedStorageKey(examenId: number): string {
    return `smartest.examen.web.joined.${examenId}`
}

export function readEtudiantId(): number {
    try {
        const token = localStorage.getItem('token')
        if (token) {
            const payload = token.split('.')[1]
            if (payload) {
                const decoded = JSON.parse(
                    atob(payload.replaceAll('-', '+').replaceAll('_', '/')),
                ) as { userId?: unknown; id?: unknown; sub?: unknown }
                const candidate = Number(decoded.userId ?? decoded.id ?? decoded.sub)
                if (Number.isFinite(candidate) && candidate > 0) return candidate
            }
        }
    } catch {
        // fallback localStorage userId
    }
    const raw = localStorage.getItem('userId')
    const n = Number(raw)
    return Number.isFinite(n) && n > 0 ? n : 1
}

export function extractApiMessage(e: unknown, fallback: string): string {
    if (axios.isAxiosError(e)) {
        const d = e.response?.data
        if (d && typeof d === 'object' && 'message' in d) {
            const m = (d as { message?: unknown }).message
            if (typeof m === 'string' && m.trim()) return m.trim()
        }
    }
    if (e instanceof Error && e.message.trim()) return e.message.trim()
    return fallback
}
