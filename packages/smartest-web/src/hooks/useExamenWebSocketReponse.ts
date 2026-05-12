import { useCallback, useRef, useEffect } from 'react'
import { Client } from '@stomp/stompjs'
import useAuth from './useAuth'
import { stompBrokerUrl } from '../config/runtimeBackend'

interface ReponseWebSocketPayload {
    examenId: number
    etudiantId: number
    questionId: number
    reponseId: number
}

export function useExamenWebSocketReponse() {
    const clientRef = useRef<Client | null>(null)
    const authToken = useAuth((s) => s.token)

    const connectWebSocket = useCallback(() => {
        if (clientRef.current?.connected) return

        const client = new Client({
            brokerURL: stompBrokerUrl(),
            reconnectDelay: 3000,
            connectHeaders: authToken ? { Authorization: `Bearer ${authToken}` } : {},
        })

        client.onConnect = () => {
            console.log('WebSocket connecté pour les réponses')
        }

        client.onWebSocketError = (error) => {
            console.error('Erreur WebSocket réponses:', error)
        }

        client.onStompError = (frame) => {
            console.error('Erreur STOMP réponses:', frame)
        }

        client.activate()
        clientRef.current = client
    }, [authToken])

    const envoyerReponseWebSocket = useCallback((payload: ReponseWebSocketPayload) => {
        if (!clientRef.current?.connected) {
            console.warn('WebSocket non connecté, tentative de reconnexion...')
            connectWebSocket()
            // Attendre un peu pour la connexion et réessayer
            setTimeout(() => {
                if (clientRef.current?.connected) {
                    clientRef.current.publish({
                        destination: '/app/examen/reponse',
                        body: JSON.stringify(payload),
                    })
                }
            }, 1000)
            return false
        }

        try {
            clientRef.current.publish({
                destination: '/app/examen/reponse',
                body: JSON.stringify(payload),
            })
            return true
        } catch (error) {
            console.error('Erreur envoi réponse WebSocket:', error)
            return false
        }
    }, [connectWebSocket])

    const disconnectWebSocket = useCallback(() => {
        if (clientRef.current?.connected) {
            clientRef.current.deactivate()
            clientRef.current = null
        }
    }, [])

    // Connexion automatique au premier usage
    useEffect(() => {
        connectWebSocket()
        return disconnectWebSocket
    }, [connectWebSocket, disconnectWebSocket])

    return {
        envoyerReponseWebSocket,
        isConnected: clientRef.current?.connected ?? false,
    }
}
