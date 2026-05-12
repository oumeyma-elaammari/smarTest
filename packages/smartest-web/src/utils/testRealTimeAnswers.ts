/**
 * Utilitaire de test pour vérifier le fonctionnement des réponses en temps réel
 * à utiliser dans la console du navigateur pendant une session d'examen
 */

export interface TestAnswerPayload {
    examenId: number
    etudiantId: number
    questionId: number
    reponseId: number
}

/**
 * Simule l'envoi d'une réponse via WebSocket pour tester le compteur temps réel
 * Usage: Dans la console du navigateur sur la page d'examen:
 * importTestRealTimeAnswers().simulateAnswer(1, 1, 1, 1)
 */
export function importTestRealTimeAnswers() {
    // Import dynamique pour éviter les erreurs de build en production
    const testFunctions = {
        simulateAnswer: (examenId: number, etudiantId: number, questionId: number, reponseId: number) => {
            const payload: TestAnswerPayload = {
                examenId,
                etudiantId,
                questionId,
                reponseId
            }

            // Trouver le client WebSocket STOMP dans la page
            const stompClients = (window as any).__SMARTTEST_STOMP_CLIENTS || []
            
            if (stompClients.length === 0) {
                console.warn('Aucun client STOMP trouvé. Assurez-vous d\'être sur une page d\'examen active.')
                return false
            }

            const client = stompClients[0]
            if (!client?.connected) {
                console.warn('Client STOMP non connecté')
                return false
            }

            try {
                client.publish({
                    destination: '/app/examen/reponse',
                    body: JSON.stringify(payload),
                })
                console.log('✅ Réponse test envoyée via WebSocket:', payload)
                return true
            } catch (error) {
                console.error('❌ Erreur lors de l\'envoi de la réponse test:', error)
                return false
            }
        },

        // Fonction pour vérifier l'état du WebSocket
        checkWebSocketStatus: () => {
            const stompClients = (window as any).__SMARTTEST_STOMP_CLIENTS || []
            console.log('📡 Clients STOMP disponibles:', stompClients.length)
            
            stompClients.forEach((client: any, index: number) => {
                console.log(`Client ${index}:`, {
                    connected: client?.connected,
                    url: client?.brokerURL
                })
            })
        },

        // Test multiple réponses consécutives
        simulateMultipleAnswers: (examenId: number, etudiantId: number, questionId: number, reponseIds: number[], delay = 1000) => {
            console.log(`🚀 Simulation de ${reponseIds.length} réponses avec ${delay}ms d'intervalle`)
            
            reponseIds.forEach((reponseId, index) => {
                setTimeout(() => {
                    testFunctions.simulateAnswer(examenId, etudiantId, questionId, reponseId)
                }, index * delay)
            })
        }
    }

    // Exposer globalement pour accès facile depuis la console
    ;(window as any).testRealTimeAnswers = testFunctions
    ;(window as any).__SMARTTEST_STOMP_CLIENTS = (window as any).__SMARTTEST_STOMP_CLIENTS || []

    console.log('🧪 Utilitaires de test temps réel chargés!')
    console.log('Utilisez: testRealTimeAnswers.simulateAnswer(examenId, etudiantId, questionId, reponseId)')
    console.log('Utilisez: testRealTimeAnswers.checkWebSocketStatus()')
    
    return testFunctions
}

/**
 * Hook pour capturer les clients STOMP créés par l'application
 * À injecter dans le code de l'application si nécessaire pour les tests
 */
export function captureStompClient(client: any) {
    if (!(window as any).__SMARTTEST_STOMP_CLIENTS) {
        ;(window as any).__SMARTTEST_STOMP_CLIENTS = []
    }
    ;(window as any).__SMARTTEST_STOMP_CLIENTS.push(client)
    console.log('📡 Client STOMP capturé pour les tests')
}
