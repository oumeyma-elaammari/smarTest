import { useEffect, useState } from 'react'

/** S'abonne à une media query CSS (ex. `(min-width: 768px)`). */
export function useMatchMedia(query: string): boolean {
    const [matches, setMatches] = useState(() =>
        typeof globalThis.window !== 'undefined' ? globalThis.window.matchMedia(query).matches : false,
    )

    useEffect(() => {
        const mq = globalThis.window.matchMedia(query)
        const apply = () => setMatches(mq.matches)
        apply()
        mq.addEventListener('change', apply)
        return () => mq.removeEventListener('change', apply)
    }, [query])

    return matches
}

/** Grille dashboard : 2 colonnes à partir de 768px. */
export function useDashboardTwoColumn(): boolean {
    return useMatchMedia('(min-width: 768px)')
}
