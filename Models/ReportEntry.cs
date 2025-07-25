﻿namespace NewsSite1.Models
{
    public class ReportEntry
    {
        public int Id { get; set; }
        public int ReporterId { get; set; }
        public string ReporterName { get; set; }
        public string TargetName { get; set; }
        public string ReportType { get; set; }
        public int ReferenceId { get; set; }
        public string Reason { get; set; }
        public DateTime ReportedAt { get; set; }
        public string Content { get; set; }
    }
}