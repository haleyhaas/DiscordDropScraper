using System;
using System.Net.Http;
using HtmlAgilityPack;
using System.Threading.Tasks;
using System.Globalization;

class Program
{
    static async Task Main(string[] args)
    {
        long totalGained = 0;

        // 10 is only 200 members, but people below this seem fairly inactive so we don't care much
        for (var i = 1; i <= 10; i++)
        {
            string url = $"https://wiseoldman.net/groups/6204/gained?period=month&page={i}";

            try
            {
                // Fetch the HTML from the page
                var httpClient = new HttpClient();
                var html = await httpClient.GetStringAsync(url);

                // Load HTML into HtmlDocument
                var htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(html);

                // Select the table rows containing the gained values
                var tableRows = htmlDoc.DocumentNode.SelectNodes("//table/tbody/tr");

                if (tableRows != null)
                {
                    foreach (var row in tableRows)
                    {
                        // Extract the 'Gained' column value (second-to-last column)
                        var gainedColumn = row.SelectSingleNode("td[position()=last()-1]");

                        if (gainedColumn != null)
                        {
                            string gainedText = gainedColumn.InnerText.Trim();
                            long gainedValue = ConvertGainedValue(gainedText);
                            totalGained += gainedValue;
                            Console.WriteLine(gainedValue);
                        }
                    }
                }
                else
                {
                    Console.WriteLine("No table rows found.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }            
        }

        Console.WriteLine($"Total Exp Gained: {totalGained}");
        await File.WriteAllTextAsync("exp.txt", $"Total Exp: {totalGained}");
    }

    private static long ConvertGainedValue(string gainedText)
    {
        try
        {
            // Remove the '+' sign and trim any spaces
            gainedText = gainedText.Replace("+", "").Trim();

            // Check if the value ends with 'm' (millions) or 'k' (thousands)
            if (gainedText.EndsWith("m", StringComparison.OrdinalIgnoreCase))
            {
                double value = double.Parse(gainedText.Replace("m", ""), CultureInfo.InvariantCulture);
                return (long)(value * 1_000_000);
            }
            else if (gainedText.EndsWith("k", StringComparison.OrdinalIgnoreCase))
            {
                double value = double.Parse(gainedText.Replace("k", ""), CultureInfo.InvariantCulture);
                return (long)(value * 1_000);
            }
            else
            {
                // If no suffix, just parse it as a plain number
                return long.Parse(gainedText, CultureInfo.InvariantCulture);
            }
        }
        catch (Exception)
        {
            // probably happens if someone's total gained for the month is less than 100k
            Console.WriteLine($"Failed to parse value: {gainedText}");
            return 0; // Return 0 if parsing fails
        }
    }
}
