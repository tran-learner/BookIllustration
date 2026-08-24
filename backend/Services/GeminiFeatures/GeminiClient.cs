using System.Net.Http.Json;
using System.Net.Http.Headers;
using System.Text.Json;
using BookIllustration_Backend.Models.DTOs.GeminiAPI;

namespace BookIllustration_Backend.Services.GeminiFeatures;

public class GeminiClient(HttpClient httpClient, GeminiOptions options)
{
    public async Task<GeminiTextInteraction> CreateTextInteractionAsync(
        string input,
        string? previousInteractionId = null,
        CancellationToken cancellationToken = default,
        object? responseFormat = null)
    {
        var requestBody = new Dictionary<string, object?>
        {
            ["model"] = options.TextModel,
            ["input"] = input
        };

        if (!string.IsNullOrWhiteSpace(previousInteractionId))
        {
            requestBody["previous_interaction_id"] = previousInteractionId;
        }

        if (responseFormat is not null)
        {
            requestBody["response_format"] = responseFormat;
        }

        using var response = await httpClient.PostAsJsonAsync(
            "interactions",
            requestBody,
            cancellationToken);

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Gemini request failed with status {(int)response.StatusCode}: {responseJson}");
        }

        using var document = JsonDocument.Parse(responseJson);

        var root = document.RootElement;
        var interactionId = root.GetProperty("id").GetString()
            ?? throw new InvalidOperationException("Gemini returned an interaction without an ID.");

        return new GeminiTextInteraction
        {
            InteractionId = interactionId,
            Text = ExtractText(root)
        };
    }

    public async Task<GeminiImageInteraction> CreateImageInteractionAsync(
        string input,
        string? previousInteractionId = null,
        CancellationToken cancellationToken = default)
    {
        var requestBody = new Dictionary<string, object?>
        {
            ["model"] = options.ImageModel,
            ["input"] = input
        };

        if (!string.IsNullOrWhiteSpace(previousInteractionId))
        {
            requestBody["previous_interaction_id"] = previousInteractionId;
        }

        using var response = await httpClient.PostAsJsonAsync(
            "interactions",
            requestBody,
            cancellationToken);

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Gemini request failed with status {(int)response.StatusCode}: {responseJson}");
        }

        using var document = JsonDocument.Parse(responseJson);

        var interactionId = document.RootElement.GetProperty("id").GetString()
            ?? throw new InvalidOperationException("Gemini returned an interaction without an ID.");

        return ExtractImage(document.RootElement, interactionId);
    }

    public async Task<GeminiTextInteraction> CreateBookInteractionAsync(
        string uploadedFileUri,
        string systemInstruction,
        CancellationToken cancellationToken = default)
    {
        var requestBody = new Dictionary<string, object?>
        {
            ["model"] = options.TextModel,
            ["input"] = new object[]
            {
                new Dictionary<string, string>
                {
                    ["type"] = "text",
                    ["text"] = systemInstruction
                },
                new Dictionary<string, string>
                {
                    ["type"] = "document",
                    ["uri"] = uploadedFileUri
                }
            }
        };

        using var response = await httpClient.PostAsJsonAsync(
            "interactions",
            requestBody,
            cancellationToken);

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Gemini book interaction failed with status {(int)response.StatusCode}: {responseJson}");
        }

        using var document = JsonDocument.Parse(responseJson);

        var interactionId = document.RootElement.GetProperty("id").GetString()
            ?? throw new InvalidOperationException("Gemini returned an interaction without an ID.");

        return new GeminiTextInteraction
        {
            InteractionId = interactionId,
            Text = ExtractText(document.RootElement)
        };
    }

    public async Task<GeminiUploadedFile> UploadFileAsync(
        Stream fileContent,
        long fileSize,
        string mimeType,
        string displayName,
        CancellationToken cancellationToken = default)
    {
        using var startRequest = new HttpRequestMessage(
            HttpMethod.Post,
            "https://generativelanguage.googleapis.com/upload/v1beta/files");

        startRequest.Headers.Add("X-Goog-Upload-Protocol", "resumable");
        startRequest.Headers.Add("X-Goog-Upload-Command", "start");
        startRequest.Headers.Add("X-Goog-Upload-Header-Content-Length", fileSize.ToString());
        startRequest.Headers.Add("X-Goog-Upload-Header-Content-Type", mimeType);
        startRequest.Content = JsonContent.Create(new
        {
            file = new { display_name = displayName }
        });

        using var startResponse = await httpClient.SendAsync(startRequest, cancellationToken);
        var startResponseJson = await startResponse.Content.ReadAsStringAsync(cancellationToken);

        if (!startResponse.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Gemini upload start failed with status {(int)startResponse.StatusCode}: {startResponseJson}");
        }

        if (!startResponse.Headers.TryGetValues("X-Goog-Upload-URL", out var uploadUrls))
        {
            throw new InvalidOperationException("Gemini did not return an upload session URL.");
        }

        using var uploadRequest = new HttpRequestMessage(HttpMethod.Post, uploadUrls.Single())
        {
            Content = new StreamContent(fileContent)
        };

        uploadRequest.Headers.Add("X-Goog-Upload-Offset", "0");
        uploadRequest.Headers.Add("X-Goog-Upload-Command", "upload, finalize");
        uploadRequest.Content.Headers.ContentType = new MediaTypeHeaderValue(mimeType);
        uploadRequest.Content.Headers.ContentLength = fileSize;

        using var uploadResponse = await httpClient.SendAsync(uploadRequest, cancellationToken);
        var uploadResponseJson = await uploadResponse.Content.ReadAsStringAsync(cancellationToken);

        if (!uploadResponse.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Gemini upload failed with status {(int)uploadResponse.StatusCode}: {uploadResponseJson}");
        }

        using var document = JsonDocument.Parse(uploadResponseJson);

        var uri = document.RootElement
            .GetProperty("file")
            .GetProperty("uri")
            .GetString()
            ?? throw new InvalidOperationException("Gemini returned an uploaded file without a URI.");

        return new GeminiUploadedFile { Uri = uri };
    }

    private static string? ExtractText(JsonElement interaction)
    {
        string? text = null;

        if (!interaction.TryGetProperty("steps", out var steps))
        {
            return text;
        }

        foreach (var step in steps.EnumerateArray())
        {
            if (!step.TryGetProperty("content", out var contentItems))
            {
                continue;
            }

            foreach (var content in contentItems.EnumerateArray())
            {
                if (content.TryGetProperty("type", out var type)
                    && type.GetString() == "text"
                    && content.TryGetProperty("text", out var contentText))
                {
                    text = contentText.GetString();
                }
            }
        }

        return text;
    }

    private static GeminiImageInteraction ExtractImage(
        JsonElement interaction,
        string interactionId)
    {
        if (interaction.TryGetProperty("steps", out var steps))
        {
            foreach (var step in steps.EnumerateArray().Reverse())
            {
                if (!step.TryGetProperty("content", out var contentItems))
                {
                    continue;
                }

                foreach (var content in contentItems.EnumerateArray().Reverse())
                {
                    if (content.TryGetProperty("type", out var type)
                        && type.GetString() == "image")
                    {
                        var imageData = content.GetProperty("data").GetString();
                        var mimeType = content.GetProperty("mime_type").GetString();

                        if (imageData is not null && mimeType is not null)
                        {
                            return new GeminiImageInteraction
                            {
                                InteractionId = interactionId,
                                ImageData = imageData,
                                MimeType = mimeType
                            };
                        }
                    }
                }
            }
        }

        throw new InvalidOperationException(
            "Gemini returned an interaction without a generated image.");
    }
}
