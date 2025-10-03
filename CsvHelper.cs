using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SimpleLogger
{
    public static class CsvHelper
    {
        public static List<QSO> Import(string filePath)
        {
            var qsos = new List<QSO>();
            var lines = File.ReadAllLines(filePath).Skip(1); // Skip header

            foreach (var line in lines)
            {
                // Basic CSV parsing, may not handle all edge cases like commas within quoted fields
                var values = line.Split(',');
                if (values.Length >= 13) // Increased to match the number of exported columns
                {
                    var qso = new QSO
                    {
                        // The order here now matches the Export method's header order
                        Date = values[0].Trim('"'),
                        Time = values[1].Trim('"'),
                        Callsign = values[2].Trim('"'),
                        FrequencyBand = values[3].Trim('"'),
                        Mode = values[4].Trim('"'),
                        RstSent = values[5].Trim('"'),
                        RstReceived = values[6].Trim('"'),
                        TheirName = values[7].Trim('"'),
                        TheirLocation = values[8].Trim('"'),
                        TheirGrid = values[9].Trim('"'),
                        MyCallsign = values[10].Trim('"'),
                        MyGrid = values[11].Trim('"'),
                        Notes = values[12].Trim('"')
                    };
                    qsos.Add(qso);
                }
            }
            return qsos;
        }

        public static void Export(List<QSO> qsos, string filePath)
        {
            var sb = new StringBuilder();

            // Define the desired column order for the export
            var headers = new List<string>
            {
                "Date", "Time", "Callsign", "FrequencyBand", "Mode",
                "RstSent", "RstReceived", "TheirName", "TheirLocation", "TheirGrid",
                "MyCallsign", "MyGrid", "Notes"
            };

            // Build and append the header row
            sb.AppendLine(string.Join(",", headers.Select(h => $"\"{h}\"")));

            // Build and append the data rows in the correct order
            foreach (var qso in qsos)
            {
                var values = new List<string>
                {
                    qso.Date,
                    qso.Time,
                    qso.Callsign,
                    qso.FrequencyBand,
                    qso.Mode,
                    qso.RstSent,
                    qso.RstReceived,
                    qso.TheirName,
                    qso.TheirLocation,
                    qso.TheirGrid,
                    qso.MyCallsign,
                    qso.MyGrid,
                    qso.Notes
                };

                // Sanitize and format each value for CSV
                var formattedValues = values.Select(val =>
                {
                    var sanitized = (val ?? "").Replace("\"", "\"\"");
                    return $"\"{sanitized}\"";
                });

                sb.AppendLine(string.Join(",", formattedValues));
            }

            File.WriteAllText(filePath, sb.ToString());
        }
    }
}

