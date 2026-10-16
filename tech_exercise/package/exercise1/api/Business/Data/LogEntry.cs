using System.ComponentModel.DataAnnotations;

namespace StargateAPI.Business.Data
{
    public class LogEntry
    {
        [Key]
        public int Id { get; set; }

        public DateTime Timestamp { get; set; }

        public string Level { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;

        public string? Exception { get; set; }

        public string Path { get; set; } = string.Empty;

        public string Method { get; set; } = string.Empty;

        public int StatusCode { get; set; }

        public long ElapsedMilliseconds { get; set; }
    }
}
