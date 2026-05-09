/** Fisher–Yates shuffle using crypto.getRandomValues (non-security-sensitive bias acceptable for UI ordering). */
export function shuffleCopy<T>(items: T[]): T[] {
    const copie = [...items]
    for (let i = copie.length - 1; i > 0; i--) {
        const j = randomIntInclusive(i)
        const temp = copie[i]
        copie[i] = copie[j]
        copie[j] = temp
    }
    return copie
}

function randomIntInclusive(max: number): number {
    if (max <= 0) return 0
    const buf = new Uint32Array(1)
    crypto.getRandomValues(buf)
    return buf[0] % (max + 1)
}
