/** Affichage cohérent examen (pages web étudiant). */

/** Durée indicative par question (secondes) → libellé lisible (« 1 min », « 1 min 30 s »). */
export function formatDureeIndicativePourQuestion(seconds: number): string {
    const s = Math.max(0, Math.floor(seconds))
    if (!Number.isFinite(s) || s <= 0) return '—'
    if (s < 60) return `${s} s`
    if (s % 60 === 0) return `${s / 60} min`
    const whole = Math.floor(s / 60)
    const rest = s % 60
    return `${whole} min ${rest} s`
}

/**
 * Instant du créneau de lancement (metadata / liste examens).
 * Retourne null si inconnu ou non parsable.
 */
export function parseDebutExamenMs(dateDebut: unknown): number | null {
    if (dateDebut == null) return null
    if (typeof dateDebut === 'string') {
        const t = Date.parse(dateDebut)
        return Number.isFinite(t) ? t : null
    }
    const arr = dateDebut as number[]
    if (Array.isArray(arr) && arr.length >= 3) {
        const d = new Date(
            Number(arr[0]),
            Number(arr[1]) - 1,
            Number(arr[2]),
            arr.length > 3 ? Number(arr[3]) : 0,
            arr.length > 4 ? Number(arr[4]) : 0,
        )
        const t = d.getTime()
        return Number.isFinite(t) ? t : null
    }
    return null
}

/** Formatte une date d’examen quelle que soit la forme API (chaîne ISO, tableau localdatetime…). */
export function formatDateTimeUnknown(value: unknown): string {
    const ms = parseDebutExamenMs(value)
    if (ms == null) return '—'
    const d = new Date(ms)
    if (Number.isNaN(d.getTime())) return '—'
    return d.toLocaleString('fr-FR', {
        weekday: 'short',
        year: 'numeric',
        month: 'short',
        day: 'numeric',
        hour: '2-digit',
        minute: '2-digit',
    })
}

export function formatDateTime(value?: string): string {
    if (!value) return '—'
    const d = new Date(value)
    if (Number.isNaN(d.getTime())) return value
    return d.toLocaleString('fr-FR', {
        weekday: 'short',
        year: 'numeric',
        month: 'short',
        day: 'numeric',
        hour: '2-digit',
        minute: '2-digit',
    })
}

export function formatTime(value?: string): string {
    if (!value) return '—'
    const d = new Date(value)
    if (Number.isNaN(d.getTime())) return '—'
    return d.toLocaleTimeString('fr-FR', { hour: '2-digit', minute: '2-digit' })
}

export function formatStatutExamen(s?: string): string {
    switch ((s || '').toUpperCase()) {
        case 'PLANIFIE':
            return 'Planifié'
        case 'EN_COURS':
            return 'En cours'
        case 'EN_PAUSE':
            return 'En pause'
        case 'TERMINE':
            return 'Terminé'
        case 'ANNULE':
            return 'Annulé'
        default:
            return s || '—'
    }
}

/** Libellé affiché pour l’étudiant : métadonnées, puis snapshot, sinon repli technique. */
export function resolveExamenDisplayTitre(
    meta: { titre?: string } | null | undefined,
    snap: { titre?: string | null } | null | undefined,
    examId: number,
): string {
    const fromMeta = meta?.titre?.trim() ?? ''
    if (fromMeta.length > 0) return fromMeta
    const fromSnap = typeof snap?.titre === 'string' ? snap.titre.trim() : ''
    if (fromSnap.length > 0) return fromSnap
    return `Examen #${examId}`
}

export function getEtatSessionLabel(etat?: string): string {
    switch ((etat || '').toUpperCase()) {
        case 'EN_ATTENTE':
        case 'PLANIFIE':
            return 'Salle d’attente'
        case 'EN_COURS':
            return 'En cours'
        case 'EN_PAUSE':
        case 'PAUSE':
            return 'En pause'
        case 'TERMINE':
            return 'Terminé'
        case 'ARRETE':
            return 'Arrêté'
        default:
            return etat || 'Connexion…'
    }
}
