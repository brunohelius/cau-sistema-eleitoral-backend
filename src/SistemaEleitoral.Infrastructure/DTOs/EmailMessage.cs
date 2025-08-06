namespace SistemaEleitoral.Infrastructure.DTOs
{
    public class EmailMessage
    {
        public string To { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public bool IsHtml { get; set; } = true;
        public List<string> Attachments { get; set; } = new List<string>();
        public string? From { get; set; }
        public List<string>? Cc { get; set; }
        public List<string>? Bcc { get; set; }
    }
}