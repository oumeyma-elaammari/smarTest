/** Seuil de réussite sur une échelle /20. */
export const SEUIL_NOTE_REUSSITE_SUR_20 = 10

export function couleurNoteSur20(note: number, bareme = 20): string {
    const seuil = bareme <= 0 ? SEUIL_NOTE_REUSSITE_SUR_20 : (SEUIL_NOTE_REUSSITE_SUR_20 * bareme) / 20
    return note < seuil ? '#dc2626' : '#10b981'
}

export type NotePassageInfo = {
    noteProposee?: number | null
    noteFinale?: number | null
    valideeParProf?: boolean
}

const EPSILON_NOTE = 0.01

export function resoudreAffichageNote(passage: NotePassageInfo, bareme = 20): {
    libelle: string
    valeur: number | null
    couleur: string
    texteComplet: string | null
} {
    const np = passage.noteProposee
    const nf = passage.noteFinale
    const validee = passage.valideeParProf === true

    if (!validee) {
        if (typeof np !== 'number' || Number.isNaN(np)) {
            return { libelle: 'Note proposée', valeur: null, couleur: '#6b7280', texteComplet: null }
        }
        return {
            libelle: 'Note proposée',
            valeur: np,
            couleur: couleurNoteSur20(np, bareme),
            texteComplet: `${np.toFixed(2)}/${bareme.toFixed(0)}`,
        }
    }

    const corrige =
        typeof nf === 'number' &&
        !Number.isNaN(nf) &&
        typeof np === 'number' &&
        !Number.isNaN(np) &&
        Math.abs(nf - np) > EPSILON_NOTE

    if (corrige) {
        return {
            libelle: 'Note corrigée',
            valeur: nf,
            couleur: couleurNoteSur20(nf, bareme),
            texteComplet: `${nf.toFixed(2)}/${bareme.toFixed(0)}`,
        }
    }

    const v = typeof nf === 'number' && !Number.isNaN(nf) ? nf : np
    if (typeof v !== 'number' || Number.isNaN(v)) {
        return { libelle: 'Note validée', valeur: null, couleur: '#6b7280', texteComplet: null }
    }
    return {
        libelle: 'Note validée',
        valeur: v,
        couleur: couleurNoteSur20(v, bareme),
        texteComplet: `${v.toFixed(2)}/${bareme.toFixed(0)}`,
    }
}
