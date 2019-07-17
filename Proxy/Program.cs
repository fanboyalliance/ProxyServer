using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace Proxy
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = new ConfigurationBuilder();
            builder.AddCommandLine(args);
            var config = builder.Build();
            StaticKeeper.Host = "localhost";
            StaticKeeper.Port = "8080";
            StaticKeeper.MainUri = "habr.com";
            
            if (config["host"] != null)
            {
                StaticKeeper.Host = config["host"];
            }
            if (config["port"] != null)
            {
                StaticKeeper.Port = config["port"];
            }
            if (config["site"] != null)
            {
                StaticKeeper.MainUri = config["site"];
            }
            
            var host = new WebHostBuilder()
                .UseConfiguration(config)
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseKestrel()
                .UseIISIntegration()
                .UseUrls($"http://{StaticKeeper.Host}:{StaticKeeper.Port}/")
                .UseStartup<Startup>()
                .Build();
            OpenBrowser($"http://{StaticKeeper.Host}:{StaticKeeper.Port}/");

            host.Run();
        }
        
        private static void OpenBrowser(string url)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Process.Start(new ProcessStartInfo("cmd", $"/c start {url}"));
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Process.Start("xdg-open", url);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Process.Start("open", url);
            }
        }
    }
}