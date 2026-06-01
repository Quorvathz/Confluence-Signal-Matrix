using System.Net.WebSockets;
using System.Text;

namespace WinFormsApp5;

internal sealed class Mt5WebSocketBridge : IAsyncDisposable
{
    private ClientWebSocket? _socket;
    private CancellationTokenSource? _receiveLoopTokenSource;

    public event EventHandler<string>? MessageReceived;
    public event EventHandler<string>? StateChanged;

    public bool IsConnected => _socket?.State == WebSocketState.Open;

    public async Task ConnectAsync(Uri endpoint)
    {
        if (IsConnected)
        {
            return;
        }

        await DisconnectAsync();
        _socket = new ClientWebSocket();
        _receiveLoopTokenSource = new CancellationTokenSource();

        PublishState("connecting");
        await _socket.ConnectAsync(endpoint, _receiveLoopTokenSource.Token);
        PublishState("connected");
        _ = Task.Run(() => ReceiveLoopAsync(_socket, _receiveLoopTokenSource.Token));
    }

    public async Task DisconnectAsync()
    {
        var socket = _socket;
        _socket = null;

        var tokenSource = _receiveLoopTokenSource;
        _receiveLoopTokenSource = null;
        tokenSource?.Cancel();

        if (socket is { State: WebSocketState.Open })
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));
            await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Operator disconnected", timeout.Token);
        }

        socket?.Dispose();
        tokenSource?.Dispose();
        PublishState("standby");
    }

    public async ValueTask DisposeAsync()
    {
        await DisconnectAsync();
    }

    private async Task ReceiveLoopAsync(ClientWebSocket socket, CancellationToken cancellationToken)
    {
        var buffer = new byte[8192];
        var message = new StringBuilder();

        try
        {
            while (socket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
            {
                var result = await socket.ReceiveAsync(buffer, cancellationToken);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    break;
                }

                message.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
                if (!result.EndOfMessage)
                {
                    continue;
                }

                MessageReceived?.Invoke(this, message.ToString());
                message.Clear();
            }
        }
        catch (OperationCanceledException)
        {
            // Normal disconnect path.
        }
        catch (Exception ex)
        {
            MessageReceived?.Invoke(this, $"{{\"error\":\"{ex.Message.Replace("\"", "'", StringComparison.Ordinal)}\"}}");
        }
        finally
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                PublishState("standby");
            }
        }
    }

    private void PublishState(string state)
    {
        StateChanged?.Invoke(this, state);
    }
}
