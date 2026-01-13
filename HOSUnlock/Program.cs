using HOSUnlock;

internal class Program
{
    private static async Task Main(string[] args)
    {
        var isHeadless = args.Length > 0 && args.Any(a => a.Equals("--headless", StringComparison.OrdinalIgnoreCase));

        if (isHeadless)
        {
            await HeadlessApp.Run(args);
        }
        else
        {
            await new App().Run(args);
        }
    }
}