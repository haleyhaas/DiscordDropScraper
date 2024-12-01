using System.Text.RegularExpressions;
using Discord;
using Discord.WebSocket;

class Program
{
    private static readonly string Token = ""; // Replace with your token
    private static readonly ulong ChannelId = 1287941567809716305;
    private static readonly DateTime StartDate = new DateTime(2024, 11, 1); // Start date filter

    public static async Task Main(string[] args)
    {
        var config = new DiscordSocketConfig
        {
            GatewayIntents = GatewayIntents.GuildMessages | GatewayIntents.MessageContent
        };

        var client = new DiscordSocketClient(config);

        try
        {
            // Login and start the bot client
            await client.LoginAsync(TokenType.Bot, Token);
            await client.StartAsync();

            // Wait until the client is ready
            await Task.Delay(5000);

            var channel = await client.GetChannelAsync(ChannelId) as IMessageChannel;
            if (channel != null)
            {
                await FetchAndSumCoins(channel);
            }
            else
            {
                Console.WriteLine("Channel not found or bot lacks permissions.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
        finally
        {
            await client.LogoutAsync();
            await client.StopAsync();
            client.Dispose();
        }

        Console.WriteLine("Finished. Press any key to exit.");
        Console.ReadKey(); // Wait for user input before closing
    }

    private static async Task FetchAndSumCoins(IMessageChannel channel)
    {
        //.Where(msg => msg.Timestamp.UtcDateTime > StartDate) // Filter by date
        long totalCoins = 0;
        var total99s = 0;
        var totalPets = 0;
        var totalClogs = 0;

        // todo, make sure we're grabbing all messages in the last month
        // ulong fromMessageId, Direction dir, int limit = 100

        var messages = channel.GetMessagesAsync(
            fromMessageId: 1312689615311867936, // messageId is from midnight 12/1/2024
            dir: Direction.After,
            limit: 50000
            ).FlattenAsync().Result.Where(msg => msg.Timestamp.UtcDateTime > StartDate);

        // the rare drop message
        var pattern = @"\(([\d,]+) coins\)"; // Regex to capture the number inside (### coins)

        var messagesWithDrop = messages.Where(msg => msg.Content.Contains("received a drop", StringComparison.OrdinalIgnoreCase));
        var regex = messagesWithDrop.Select(msg =>
        {
            var result = msg.Content.Replace(@"\", "");
            var match = Regex.Match(result, pattern);
            return match.Success ? long.Parse(match.Groups[1].Value.Replace(",", "")) : 0;
        });
        totalCoins += regex.Sum();
        
        // the level 99s gained
        var messagesWith99s = messages.Select(msg =>
        {
            var result = msg.Content.Replace(@"\", "");
            var success = result.Contains(":Statsicon:") 
                          && result.Contains("has reached")
                          && result.Contains("level 99");
            return success ? result : string.Empty;
        });
        total99s += messagesWith99s.Count(x => !string.IsNullOrEmpty(x));

        // the number of pets gained
        var messagesWithPets = messages.Select(msg =>
        {
            var result = msg.Content.Replace(@"\", "");
            var success = result.Contains(":Petshopicon:") && result.Contains("has a funny feeling like");
            return success ? result : string.Empty;
        });
        totalPets += messagesWithPets.Count(x => !string.IsNullOrEmpty(x));

        // collection log slots completed
        var messagesWithClog = messages.Select(msg =>
        {
            var result = msg.Content.Replace(@"\", "");
            var success = result.Contains(":Collectionlog:") && result.Contains("received a new collection log item");
            return success ? result : string.Empty;
        });
        totalClogs += messagesWithClog.Count(x => !string.IsNullOrEmpty(x));

        Console.WriteLine($"Saved total coin sum: {totalCoins} coins.");
        Console.WriteLine($"Saved total 99s sum: {total99s} 99s.");
        Console.WriteLine($"Saved total pets sum: {totalPets} pets.");
        Console.WriteLine($"Saved total new clogs sum: {totalClogs} clogs.");

        await File.WriteAllTextAsync("drops.txt", $"Total Coins: {totalCoins}");
        await File.WriteAllTextAsync("skills.txt", $"Total 99s: {total99s}");
        await File.WriteAllTextAsync("pets.txt", $"Total pets: {totalPets}");
        await File.WriteAllTextAsync("clogs.txt", $"Total clogs: {totalClogs}");
    }
}
