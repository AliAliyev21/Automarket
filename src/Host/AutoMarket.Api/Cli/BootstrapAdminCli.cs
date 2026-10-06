using System.Text;
using AutoMarket.Identity;

namespace AutoMarket.Api.Cli;

// ARCHITECTURE §11: ilk Admin birdəfəlik əmrlə yaradılır, HTTP endpoint-i yoxdur və kodda parol yoxdur:
//   dotnet run --project src/Host/AutoMarket.Api -- bootstrap-admin --email admin@example.az [--name "Ad Soyad"]
// Şifrə arqumentdən oxunmur (shell tarixçəsinə düşməsin): BootstrapAdmin:Password konfiqurasiyası (env var
// AutoMarket__BootstrapAdmin__Password, user-secrets) və ya stdin. Sistemdə aktiv Admin varsa əmr rədd olunur
internal static class BootstrapAdminCli
{
    public const string CommandName = "bootstrap-admin";
    public const string PasswordConfigurationKey = "BootstrapAdmin:Password";

    private const string DefaultName = "Administrator";

    public static bool IsRequested(string[] args) => args.Length > 0 && args[0] == CommandName;

    public static async Task<int> RunAsync(IServiceProvider services, IConfiguration configuration, string[] args, CancellationToken cancellationToken)
    {
        var email = ReadOption(args, "--email");
        if (string.IsNullOrWhiteSpace(email))
        {
            await Console.Error.WriteLineAsync($"Usage: {CommandName} --email <email> [--name <name>]");
            return 2;
        }

        var name = ReadOption(args, "--name") ?? DefaultName;
        var password = configuration[PasswordConfigurationKey];
        if (string.IsNullOrEmpty(password))
        {
            await Console.Out.WriteAsync("Password: ");
            password = ReadPassword();
        }

        if (string.IsNullOrEmpty(password))
        {
            await Console.Error.WriteLineAsync("Password is required.");
            return 2;
        }

        var (succeeded, message) = await IdentityModule.BootstrapAdminAsync(services, email, password, name, cancellationToken);
        await (succeeded ? Console.Out : Console.Error).WriteLineAsync(message);
        return succeeded ? 0 : 1;
    }

    private static string? ReadOption(string[] args, string option)
    {
        var index = Array.IndexOf(args, option);
        return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
    }

    // İnteraktiv terminalda simvollar ekranda göstərilmir; yönləndirilmiş stdin-dən bir sətir oxunur
    private static string? ReadPassword()
    {
        if (Console.IsInputRedirected)
        {
            return Console.In.ReadLine();
        }

        var password = new StringBuilder();
        while (true)
        {
            var key = Console.ReadKey(intercept: true);
            if (key.Key == ConsoleKey.Enter)
            {
                Console.WriteLine();
                return password.ToString();
            }

            if (key.Key == ConsoleKey.Backspace)
            {
                if (password.Length > 0)
                {
                    password.Length--;
                }
            }
            else if (!char.IsControl(key.KeyChar))
            {
                password.Append(key.KeyChar);
            }
        }
    }
}
