import { Component, type ErrorInfo, type ReactNode } from 'react'
import { getUserErrorMessage } from '../utils/userErrorMessage'

type ErrorBoundaryProps = {
    children: ReactNode
}

type ErrorBoundaryState = {
    hasError: boolean
    message: string | null
}

export default class ErrorBoundary extends Component<ErrorBoundaryProps, ErrorBoundaryState> {
    constructor(props: ErrorBoundaryProps) {
        super(props)
        this.state = { hasError: false, message: null }
    }

    static getDerivedStateFromError(error: unknown): Partial<ErrorBoundaryState> {
        const message = getUserErrorMessage(error, 'Une erreur est survenue. Rechargez la page puis reessayez.')
        return { hasError: true, message }
    }

    componentDidCatch(error: unknown, info: ErrorInfo): void {
        console.error('[ErrorBoundary]', error, info.componentStack)
    }

    render(): ReactNode {
        if (this.state.hasError) {
            return (
                <main
                    style={{
                        minHeight: '100vh',
                        display: 'flex',
                        flexDirection: 'column',
                        alignItems: 'center',
                        justifyContent: 'center',
                        padding: 24,
                        fontFamily: "'DM Sans', system-ui, sans-serif",
                        background: '#f8fafc',
                        color: '#0f172a',
                        textAlign: 'center',
                        gap: 12,
                    }}
                >
                    <h1 style={{ margin: 0, fontSize: '1.35rem' }}>Une erreur inattendue s’est produite</h1>
                    <p style={{ margin: 0, color: '#64748b', maxWidth: 440, lineHeight: 1.5 }}>
                        {this.state.message ?? 'Rechargez la page ou réessayez plus tard.'}
                    </p>
                    <button
                        type="button"
                        onClick={() => window.location.reload()}
                        style={{
                            marginTop: 8,
                            padding: '8px 16px',
                            borderRadius: 8,
                            border: 'none',
                            cursor: 'pointer',
                            background: '#0f1e3d',
                            color: '#fff',
                            fontFamily: 'inherit',
                        }}
                    >
                        Recharger la page
                    </button>
                </main>
            )
        }
        return this.props.children
    }
}
