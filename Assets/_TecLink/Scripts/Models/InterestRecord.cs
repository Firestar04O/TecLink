namespace TecLink.Models
{
    public sealed class InterestRecord
    {
        public string ProjectId { get; set; } = string.Empty;
        public ProjectInterest Data { get; set; }
    }
}
