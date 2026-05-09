using System.Threading;
using System.Threading.Tasks;

namespace smartest_desktop.Services;

/// <summary>Abstraction utilisée pour les tests UI (QuizGeneration sans appel réseau réel).</summary>
public interface IGroqGenerationClient
{
    Task<string> GenererAvecRetryAsync(string prompt, CancellationToken ct, double temperature = 0.65);
}
