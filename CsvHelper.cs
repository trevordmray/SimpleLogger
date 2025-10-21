using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions; // Added for Regex support

namespace SimpleLogger
{
    public static class CsvHelper
    {
        public static List<QSO> Import(string filePath)
        {
            var qsos = new List<QSO>();
            // Skip the header row
            var lines = File.ReadAllLines(filePath).Skip(1);

            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                // FIX 1: Use Regex to split the line, which correctly handles commas inside quoted fields.
                var values = Regex.Split(line, ",(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)");

                // FIX 2: The export function writes 13 columns, so we should check for 13.
                if (values.Length >= 13)
                {
                    // Helper function to un-escape the CSV field. It removes the surrounding quotes
                    // and replaces escaped double-quotes ("") with a single quote (").
                    string Unescape(string value)
                    {
                        if (value.StartsWith("\"") && value.EndsWith("\""))
                        {
                            value = value.Substring(1, value.Length - 2);
                        }
                        return value.Replace("\"\"", "\"");
                    }

                    var qso = new QSO
                    {
                        // FIX 3: Apply the Unescape helper to each value.
                        Date = Unescape(values[0]),
                        Time = Unescape(values[1]),
                        Callsign = Unescape(values[2]),
                        FrequencyBand = Unescape(values[3]),
                        Mode = Unescape(values[4]),
                        RstSent = Unescape(values[5]),
                        RstReceived = Unescape(values[6]),
                        TheirName = Unescape(values[7]),
                        TheirLocation = Unescape(values[8]),
                        TheirGrid = Unescape(values[9]),
                        MyCallsign = Unescape(values[10]),
                        MyGrid = Unescape(values[11]),
                        Notes = Unescape(values[12])
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