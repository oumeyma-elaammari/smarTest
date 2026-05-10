import { act } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import useAuth from '../../src/hooks/useAuth'

describe('useAuth', () => {
    beforeEach(() => {
        localStorage.clear()
        act(() => {
            useAuth.setState({
                token: null,
                role: null,
                nom: null,
                email: null,
                isAuthenticated: false,
            })
        })
        vi.restoreAllMocks()
    })

    afterEach(() => {
        memoryLocationHref = '/'
    })

    let memoryLocationHref = '/'

    it('has null token and is not authenticated initially', () => {
        expect(useAuth.getState().token).toBeNull()
        expect(useAuth.getState().isAuthenticated).toBe(false)
    })

    it('login saves token and profile to Zustand and localStorage', () => {
        const payload = {
            token: 'tok-1',
            role: 'ETUDIANT',
            nom: 'Alice',
            email: 'alice@ump.ac.ma',
        }
        act(() => {
            useAuth.getState().login(payload)
        })
        expect(useAuth.getState().token).toBe('tok-1')
        expect(useAuth.getState().isAuthenticated).toBe(true)
        expect(localStorage.getItem('token')).toBe('tok-1')
        expect(localStorage.getItem('nom')).toBe('Alice')
    })

    it('logout clears the store and localStorage and redirects to login', () => {
        vi.spyOn(globalThis, 'confirm').mockReturnValue(true)
        const assignHref = vi.fn((v: string) => {
            memoryLocationHref = v
        })
        Object.defineProperty(globalThis.window, 'location', {
            configurable: true,
            value: {
                get href() {
                    return memoryLocationHref
                },
                set href(v: string) {
                    assignHref(v)
                },
                assign: vi.fn(),
                replace: vi.fn(),
                reload: vi.fn(),
            },
            writable: true,
        })

        act(() => {
            useAuth.getState().login({
                token: 'x',
                role: 'ETUDIANT',
                nom: 'N',
                email: 'n@ump.ac.ma',
            })
        })
        act(() => {
            useAuth.getState().logout()
        })
        expect(useAuth.getState().token).toBeNull()
        expect(localStorage.getItem('token')).toBeNull()
        expect(assignHref).toHaveBeenCalled()
        expect(memoryLocationHref).toBe('/login')
    })

    it('login still updates Zustand when localStorage throws QuotaExceededError', () => {
        const spy = vi.spyOn(Storage.prototype, 'setItem').mockImplementation(() => {
            const err = new Error('QuotaExceededError')
            err.name = 'QuotaExceededError'
            throw err
        })
        act(() => {
            useAuth.getState().login({
                token: 'mem-only',
                role: 'ETUDIANT',
                nom: 'Bob',
                email: 'bob@ump.ac.ma',
            })
        })
        expect(useAuth.getState().token).toBe('mem-only')
        expect(useAuth.getState().isAuthenticated).toBe(true)
        spy.mockRestore()
    })

    it('rehydrateFromStorage restores state after a simulated reload', () => {
        act(() => {
            useAuth.getState().login({
                token: 'persist',
                role: 'ETUDIANT',
                nom: 'Cara',
                email: 'cara@ump.ac.ma',
            })
        })
        act(() => {
            useAuth.setState({
                token: null,
                role: null,
                nom: null,
                email: null,
                isAuthenticated: false,
            })
        })
        act(() => {
            useAuth.getState().rehydrateFromStorage()
        })
        expect(useAuth.getState().token).toBe('persist')
        expect(useAuth.getState().nom).toBe('Cara')
        expect(useAuth.getState().isAuthenticated).toBe(true)
    })
})
