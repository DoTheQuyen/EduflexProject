using System.Text.Json.Serialization;

namespace ShareService.Enums.Chat
{
    // Which downstream provider actually generated the answer — recorded so a future
    // training-data export can weigh/exclude answers by source quality.
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ChatProvider
    {
        Gemini = 1,
        Groq = 2,
        OpenRouter = 3,
    }

    // Curation state for turning cached Q&A into a training-data source — nothing is
    // exported until a human flips it to Approved.
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ChatReviewStatus
    {
        Unreviewed = 1,
        Approved = 2,
        Rejected = 3,
    }
}
