using System.Net.Http.Json;
using System.Text.Json;
using BookIllustration_Backend.Models.DTOs.GeminiAPI;

namespace BookIllustration_Backend.Services.GeminiFeatures;

public class GeminiClient(HttpClient httpClient, GeminiOptions options)
{
    public async Task<GeminiTextInteraction> CreateTextInteractionAsync(
        string input,
        string? previousInteractionId = null,
        CancellationToken cancellationToken = default)
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
}
