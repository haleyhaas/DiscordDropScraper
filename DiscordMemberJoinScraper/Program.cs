using System.Text.RegularExpressions;
using Discord;
using Discord.WebSocket;
using Microsoft.VisualBasic;

class Program
{
    private static readonly string Token = ""; // Replace with your token
    private static readonly ulong ChannelId = 1181608074885210142;
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
        var totalNewMembers = 0;

        // todo, make sure we're grabbing all messages in the last month
        var messages = channel.GetMessagesAsync(limit: 500).FlattenAsync().Result.Where(msg => msg.Timestamp.UtcDateTime > StartDate);

        var titles = messages.SelectMany(x => x.Embeds)
            .Select(x => x.Title)
            .Where(x => x.Contains("New group members joined", StringComparison.CurrentCultureIgnoreCase)
                     || x.Contains("New group member joined", StringComparison.CurrentCultureIgnoreCase))
            .ToList();

        foreach(var title in titles)
        {
            var result = title.Replace("🎉", "").Replace("New group member joined", "").Replace("New group members joined", "");
            totalNewMembers += int.Parse(result);
        }

        await File.WriteAllTextAsync("members.txt", $"Total new members: {totalNewMembers}");
    }
}
