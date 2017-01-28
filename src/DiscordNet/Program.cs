namespace DiscordNet
{
    public class Program
    {
        public static void Main(string[] args) => new DiscordNet().RunAsync().GetAwaiter().GetResult();
    }
}
