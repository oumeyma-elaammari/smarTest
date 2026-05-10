import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import './styles/fonts.css'
import './styles/index.css'
import './styles/global.css'
import App from './App.tsx'
import ErrorBoundary from './components/ErrorBoundary'

const rootElement = document.getElementById('root')
if (!rootElement) {
    throw new Error("L'élément racine 'root' est introuvable.")
}

createRoot(rootElement).render(
    <StrictMode>
        <ErrorBoundary>
            <App />
        </ErrorBoundary>
    </StrictMode>,
)