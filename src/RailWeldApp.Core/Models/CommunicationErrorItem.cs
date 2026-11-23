namespace RailWeldApp.Models
{
    public sealed class CommunicationErrorItem
    {
        public string Time { get; init; } = string.Empty;
        public string CommunicationType { get; init; } = string.Empty;
        public string Message { get; init; } = string.Empty;
    }
}
