using System.Globalization;
using System.Text.RegularExpressions;

namespace PlasticGitter;

public class Utilities
{
    public static DateTime? ExtractDateTime(string s)
    {
        string pattern = @"\d{4}-\d{2}-\d{2} \d{1,2}:\d{2}:\d{2} [AP]M";

        // Create a Regex object with the specified pattern
        Regex regex = new Regex(pattern);

        // Perform the search
        Match match = regex.Match(s);

        // Check if the pattern was found
        if (match.Success)
        {
            // Extracted datetime string
            string dateTimeStr = match.Value;
            // Console.WriteLine($"Extracted DateTime String: {dateTimeStr}");

            // Define the exact format
            string format = "yyyy-MM-dd h:mm:ss tt";

            // Parse the extracted string into a DateTime object
            if (DateTime.TryParseExact(dateTimeStr, format, CultureInfo.InvariantCulture, DateTimeStyles.None,
                    out DateTime dateTime))
            {
                // Console.WriteLine($"Parsed DateTime: {dateTime}");
            }
            else
            {
                // Console.WriteLine("Failed to parse the DateTime.");
            }

            return dateTime;
        }
        else
        {
            Console.WriteLine("No datetime found in the text.");
            return null;
        }
    }
}