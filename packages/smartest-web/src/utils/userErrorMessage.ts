import axios from 'axios'

function fromErrorCode(code: string): string | null {
    const normalized = code.trim().toUpperCase()
    if (!normalized) return null
    if (normalized === 'INVALID_QUIZ_STATE') return 'Le quiz ne peut pas etre modifie dans son etat actuel.'
    if (normalized.startsWith('INVALID_')) return 'Les informations fournies sont invalides.'
    if (normalized.includes('UNAUTHORIZED') || normalized.includes('FORBIDDEN')) return 'Acces refuse.'
    if (normalized.includes('NOT_FOUND')) return 'Ressource introuvable.'
    return null
}

function fromStatus(status: number | undefined): string | null {
    if (!status) return null
    if (status === 400) return 'Les informations envoyees sont invalides.'
    if (status === 401) return 'Session expiree ou non autorisee.'
    if (status === 403) return 'Acces refuse.'
    if (status === 404) return 'Ressource introuvable.'
    if (status === 429) return 'Trop de requetes. Reessayez dans quelques instants.'
    if (status >= 500) return 'Erreur serveur. Reessayez plus tard.'
    return null
}

function fromAxiosData(data: unknown, fallback: string): string | null {
    if (typeof data === 'string') {
        return sanitizeUserMessage(data, fallback)
    }
    if (!data || typeof data !== 'object') {
        return null
    }

    const record = data as Record<string, unknown>
    const directMessage = record.message ?? record.error_description ?? record.detail
    if (typeof directMessage === 'string' && directMessage.trim()) {
        const sanitized = sanitizeUserMessage(directMessage, fallback)
        if (sanitized !== fallback || directMessage.length <= 220) {
            return sanitized
        }
    }

    const errorCode = record.error ?? record.code
    if (typeof errorCode === 'string' && errorCode.trim()) {
        const fromCode = fromErrorCode(errorCode)
        if (fromCode && fromCode !== 'Acces refuse.') return fromCode
    }

    for (const value of Object.values(record)) {
        if (typeof value === 'string' && value.trim()) {
            return sanitizeUserMessage(value, fallback)
        }
    }
    return null
}

export function sanitizeUserMessage(raw: unknown, fallback: string): string {
    if (typeof raw !== 'string') return fallback
    const text = raw.trim()
    if (!text) return fallback

    // Hide stack traces / exception names / java-like package names
    if (
        /exception|stack|^\s*at\s+/i.test(text) ||
        /(?:java|org|com|sun|system)\.[a-z0-9_.]+/i.test(text)
    ) {
        return fallback
    }

    if (/^[A-Z0-9_]+$/.test(text)) {
        return fromErrorCode(text) ?? fallback
    }

    if (text.length > 220) return fallback
    return text
}

export function getUserErrorMessage(error: unknown, fallback: string): string {
    if (axios.isAxiosError(error)) {
        const status = error.response?.status
        const fromData = fromAxiosData(error.response?.data, fallback)
        if (fromData) return fromData

        if (!error.response) return 'Impossible de contacter le serveur.'
        const statusMessage = fromStatus(status)
        if (statusMessage) return statusMessage
    }

    if (error instanceof Error) {
        return sanitizeUserMessage(error.message, fallback)
    }

    return fallback
}
