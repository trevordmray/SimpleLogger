namespace SimpleLogger
{
    public class QSO
    {
        public string Date { get; set; } = string.Empty;
        public string Time { get; set; } = string.Empty;
        public string Callsign { get; set; } = string.Empty;
        public string TheirName { get; set; } = string.Empty;
        public string FrequencyBand { get; set; } = string.Empty;
        public string Mode { get; set; } = string.Empty;
        public string RstSent { get; set; } = string.Empty;
        public string RstReceived { get; set; } = string.Empty;
        public string TheirGrid { get; set; } = string.Empty;
        public string TheirLocation { get; set; } = string.Empty; // Consolidated location
        public string MyCallsign { get; set; } = string.Empty;
        public string MyGrid { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
    }
}

