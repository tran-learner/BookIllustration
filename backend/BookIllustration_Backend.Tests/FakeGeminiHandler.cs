using System.Net;
using System.Text;

namespace BookIllustration_Backend.Tests;

public class FakeGeminiHandler : HttpMessageHandler
{
    private int _interactionRequestCount;
    private readonly object _pauseLock = new();
    private TaskCompletionSource? _pausedInteractionStarted;
    private TaskCompletionSource? _releasePausedInteraction;
    private bool _pauseNextInteraction;

    public void PauseNextInteraction()
    {
        lock (_pauseLock)
        {
            _pausedInteractionStarted = new TaskCompletionSource(
                TaskCreationOptions.RunContinuationsAsynchronously);
            _releasePausedInteraction = new TaskCompletionSource(
                TaskCreationOptions.RunContinuationsAsynchronously);
            _pauseNextInteraction = true;
        }
    }

    public Task WaitUntilPausedInteractionStartsAsync()
    {
        lock (_pauseLock)
        {
            return _pausedInteractionStarted?.Task
                ?? throw new InvalidOperationException(
                    "No Gemini interaction has been configured to pause.");
        }
    }

    public void ReleasePausedInteraction()
    {
        lock (_pauseLock)
        {
            _releasePausedInteraction?.TrySetResult();
        }
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var path = request.RequestUri?.AbsolutePath;

        if (path == "/upload/v1beta/files")
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Headers.Add(
                "X-Goog-Upload-URL",
                "https://test-upload-session.local/session");

            return response;
        }

        if (request.RequestUri?.Host == "test-upload-session.local")
        {
            return CreateJsonResponse("""
                {
                  "file": {
                    "uri": "https://generativelanguage.googleapis.com/v1beta/files/test-book"
                  }
                }
                """);
        }

        if (path == "/v1beta/interactions")
        {
            var interactionRequestNumber = Interlocked.Increment(
                ref _interactionRequestCount);

            TaskCompletionSource? releasePausedInteraction = null;

            lock (_pauseLock)
            {
                if (_pauseNextInteraction)
                {
                    _pauseNextInteraction = false;
                    _pausedInteractionStarted!.TrySetResult();
                    releasePausedInteraction = _releasePausedInteraction;
                }
            }

            if (releasePausedInteraction is not null)
            {
                await releasePausedInteraction.Task.WaitAsync(cancellationToken);
            }

            return interactionRequestNumber switch
            {
                1 => CreateJsonResponse("""
                    {
                      "id": "book-interaction-id",
                      "steps": []
                    }
                    """),
                2 => CreateJsonResponse("""
                    {
                      "id": "style-interaction-id",
                      "steps": [
                        {
                          "type": "model_output",
                          "content": [
                            {
                              "type": "text",
                              "text": "Whimsical watercolor storybook illustration."
                            }
                          ]
                        }
                      ]
                    }
                    """),
                _ => new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new StringContent(
                        "Unexpected Gemini interaction request.",
                        Encoding.UTF8,
                        "text/plain")
                }
            };
        }

        return new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            Content = new StringContent(
                "Unexpected Gemini request.",
                Encoding.UTF8,
                "text/plain")
        };
    }

    private static HttpResponseMessage CreateJsonResponse(string json) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(json, Encoding.UTF8, "application/json")
    };
}
