using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace smartest_desktop.Helpers
{
    /// <summary>Sessions STOMP minimales pour /ws Spring (CONNECT, SUBSCRIBE, SEND).</summary>
    public sealed class StompWebSocketSession : IAsyncDisposable
    {
        private static readonly TimeSpan DelaiMaxConnexionStomp = TimeSpan.FromSeconds(30);

        private readonly ClientWebSocket _ws = new();
        private readonly TaskCompletionSource<bool> _connecteTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private Task? _boucleRecevoir;
        private CancellationTokenSource? _boucleCt;
        private int _aboId;

        public bool EstConnecte => _ws.State == WebSocketState.Open;

        public async Task ConnecterAsync(Uri wsEndpoint, string? jwtBearer, CancellationToken cancellationToken)
        {
            using var delai = new CancellationTokenSource(DelaiMaxConnexionStomp);
            using var lie = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, delai.Token);

            try
            {
                await _ws.ConnectAsync(wsEndpoint, lie.Token).ConfigureAwait(false);

                MessageRecuInterne -= OnStompIncoming;
                MessageRecuInterne += OnStompIncoming;

                var sbConn = new StringBuilder();
                sbConn.AppendLine("CONNECT");
                sbConn.AppendLine("accept-version:1.1,1.2");
                sbConn.AppendLine("heart-beat:0,0");
                if (!string.IsNullOrWhiteSpace(jwtBearer))
                    sbConn.AppendLine($"Authorization:Bearer {jwtBearer.Trim()}");
                sbConn.Append('\n');
                await EnvoyerRawAsync(sbConn.ToString(), lie.Token).ConfigureAwait(false);

                _boucleCt = CancellationTokenSource.CreateLinkedTokenSource(lie.Token);
                _boucleRecevoir = RecevoirBoucle(_boucleCt.Token);

                await _connecteTcs.Task.WaitAsync(lie.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                throw new InvalidOperationException(
                    "Connexion temps réel (STOMP) : délai dépassé ou annulation. " +
                    "Vérifiez que le backend est démarré, le port (ex. 8081) et l’URL WebSocket ws://…/ws.",
                    null);
            }
        }

        private void OnStompIncoming(string commandeOuMessage, string? corpsSiMessage)
        {
            if ("CONNECTED".Equals(commandeOuMessage, StringComparison.Ordinal))
                _connecteTcs.TrySetResult(true);

            if ("ERROR".Equals(commandeOuMessage, StringComparison.Ordinal))
            {
                string detail = string.IsNullOrWhiteSpace(corpsSiMessage)
                    ? "Le serveur a renvoyé une trame STOMP ERROR (souvent droits ou sécurité messaging)."
                    : corpsSiMessage.Trim();
                _connecteTcs.TrySetException(new InvalidOperationException(detail));
            }

            if ("MESSAGE".Equals(commandeOuMessage, StringComparison.Ordinal) && corpsSiMessage != null)
                CorpsMessageExterne?.Invoke(corpsSiMessage);
        }

        /// <summary>Corps JSON de chaque trame MESSAGE (destination topic).</summary>
        public event Action<string>? CorpsMessageExterne;

        private event Action<string, string?>? MessageRecuInterne;

        public Task SAbonnerAsync(string stompDestination, CancellationToken ct)
        {
            var id = "sub-" + System.Threading.Interlocked.Increment(ref _aboId).ToString();
            var sb = new StringBuilder();
            sb.AppendLine("SUBSCRIBE");
            sb.Append("id:").AppendLine(id);
            sb.Append("destination:").AppendLine(stompDestination);
            sb.Append('\n');
            return EnvoyerRawAsync(sb.ToString(), ct);
        }

        public Task EnvoyerJsonAsync(string stompDestination, object corpsObj, string? jwtBearer, CancellationToken ct)
        {
            var json = JsonConvert.SerializeObject(corpsObj, new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                NullValueHandling = NullValueHandling.Ignore,
            });
            var sb = new StringBuilder();
            sb.AppendLine("SEND");
            sb.Append("destination:").AppendLine(stompDestination);
            sb.AppendLine("content-type:application/json;charset=UTF-8");
            if (!string.IsNullOrWhiteSpace(jwtBearer))
                sb.AppendLine($"Authorization:Bearer {jwtBearer.Trim()}");
            sb.Append('\n');
            sb.Append(json);
            return EnvoyerRawAsync(sb.ToString(), ct);
        }

        private async Task RecevoirBoucle(CancellationToken ct)
        {
            var buffer = new byte[64 * 1024];
            var accum = new List<byte>();
            try
            {
                while (_ws.State == WebSocketState.Open && !ct.IsCancellationRequested)
                {
                    WebSocketReceiveResult result;
                    try
                    {
                        result = await _ws.ReceiveAsync(new ArraySegment<byte>(buffer), ct).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }

                    if (result.MessageType == WebSocketMessageType.Close)
                        break;

                    accum.AddRange(buffer.AsSpan(0, result.Count).ToArray());

                    if (!result.EndOfMessage)
                        continue;

                    string text = Encoding.UTF8.GetString(accum.ToArray());
                    accum.Clear();
                    TraiterUneTrameUtf8(text);
                }
            }
            catch { /* fermeture */ }
        }

        private void TraiterUneTrameUtf8(string utf8SansNullTerminate)
        {
            string trimmed = utf8SansNullTerminate.TrimEnd('\0', '\r', '\n').TrimStart();
            if (trimmed.StartsWith("CONNECTED", StringComparison.Ordinal))
                MessageRecuInterne?.Invoke("CONNECTED", null);
            else if (trimmed.StartsWith("MESSAGE", StringComparison.Ordinal))
                MessageRecuInterne?.Invoke("MESSAGE", ExtraireCorps(trimmed));
            else if (trimmed.StartsWith("ERROR", StringComparison.Ordinal))
                MessageRecuInterne?.Invoke("ERROR", trimmed);
        }

        private static string ExtraireCorps(string frame)
        {
            int nl = frame.IndexOf("\n\n", StringComparison.Ordinal);
            if (nl < 0 || nl + 2 >= frame.Length)
                return string.Empty;
            return frame[(nl + 2)..].TrimEnd('\0');
        }

        private async Task EnvoyerRawAsync(string frame, CancellationToken cancellationToken)
        {
            // STOMP 1.2 : fins de ligne LF ; CRLF Windows peut perturber le décodage (corps vu comme vide).
            string normalise = frame.Replace("\r\n", "\n", StringComparison.Ordinal).Replace("\r", "\n", StringComparison.Ordinal);
            byte[] bytes = Encoding.UTF8.GetBytes(normalise + "\0");
            await _ws.SendAsync(
                new ArraySegment<byte>(bytes),
                WebSocketMessageType.Text,
                endOfMessage: true,
                cancellationToken).ConfigureAwait(false);
        }

        public async ValueTask DisposeAsync()
        {
            try
            {
                _boucleCt?.Cancel();
                if (_boucleRecevoir != null)
                    await _boucleRecevoir.ConfigureAwait(false);
            }
            catch { /* ignored */ }

            if (_ws.State == WebSocketState.Open)
                try
                {
                    await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None)
                        .ConfigureAwait(false);
                }
                catch { /* ignored */ }
            _ws.Dispose();
            _boucleCt?.Dispose();
            MessageRecuInterne -= OnStompIncoming;
        }
    }
}
