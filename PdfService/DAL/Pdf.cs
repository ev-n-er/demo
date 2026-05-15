namespace PdfService.DAL
{
    public enum StatusEnum
    {
        Queued,
        Ready,
        Failed
    }

    public class Pdf
    {
        public Guid Id { get; set; }
        public string? Text { get; set; }
        public StatusEnum Status { get; set; }
    }
}
