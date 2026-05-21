import { describe, expect, it } from 'vitest'

/**
 * Garde-fou léger « perf » : évite les régressions grossières sur du calcul pur
 * (sans réseau ni DOM). Les seuils restent larges pour la CI partagée.
 */
describe('perf smoke (CPU)', () => {
    it('tri de 20k entiers reste sous un plafond raisonnable', () => {
        const n = 20_000
        const arr = Array.from({ length: n }, (_, i) => (n - i) % 997)
        const t0 = performance.now()
        arr.sort((a, b) => a - b)
        const ms = performance.now() - t0
        expect(arr[0]).toBeLessThanOrEqual(arr[arr.length - 1])
        expect(ms).toBeLessThan(5000)
    })
})
