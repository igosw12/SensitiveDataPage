using Microsoft.Playwright;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.ComponentModel;
using System.Net.Http;

namespace SensitiveDataPageTests
{
    public class BrowserFixture : IAsyncLifetime
    {
        private IPlaywright _playwright = null!;
        private IBrowser _browser = null!;
        private Process? _serverProcess;

        public string BaseUrl { get; } = "https://localhost:7157/";
        private const string BaseUrlConstant = "https://localhost:7157/";

        public async Task InitializeAsync()
        {
            _serverProcess = StartServer();
            await Task.Delay(500);

            _playwright = await Playwright.CreateAsync();
            _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = false
            });
        }

        public async Task<IBrowserContext> CreateBrowserContextAsync()
        {
            return await _browser.NewContextAsync(new BrowserNewContextOptions
            {
                BaseURL = BaseUrl
            });
        }

        private static Process StartServer()
        {
            KillProcessOnPort(7157);

            var currentPath = Directory.GetCurrentDirectory();
            var solutionPath = Directory.GetParent(currentPath)!.Parent!.Parent!.Parent!.FullName;
            var projectPath = Path.Combine(solutionPath, "SensitiveDataPage");

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    WorkingDirectory = projectPath,
                    FileName = "dotnet",
                    Arguments = $"run --urls \"{BaseUrlConstant}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = false
                }
            };

            process.Start();
            return process;
        }

        private static void KillProcessOnPort(int port)
        {
            var activePorts = IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners();
            if (!activePorts.Any(ep => ep.Port == port))
                return;

            var findProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c netstat -ano | findstr :{port}",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            findProcess.Start();
            var output = findProcess.StandardOutput.ReadToEnd();
            findProcess.WaitForExit();

            foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                var parts = line.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length > 0 && int.TryParse(parts[^1], out var pid))
                {
                    try
                    {
                        var existing = Process.GetProcessById(pid);
                        existing.Kill(entireProcessTree: true);
                        existing.WaitForExit();
                    } 
                    catch (ArgumentException) { }
                    catch (Win32Exception) { }
                    catch (InvalidOperationException) { }
                }
            }
        }

        public async Task DisposeAsync()
        {
            _serverProcess?.Kill(entireProcessTree: true);
            _serverProcess?.Dispose();

            await _browser.DisposeAsync();
            _playwright.Dispose();

            KillProcessOnPort(7157);
        }
    }
}