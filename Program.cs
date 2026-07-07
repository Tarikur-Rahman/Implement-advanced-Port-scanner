using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;


class Program
{
    private const int MaxdegreeOfParallelism = 100;
    static string GetServiceName(int port)
    {
        return port switch
        {
            21 => "FTP",
            22 => "SSH",
            23 => "Telnet",
            25 => "SMTP",
            53 => "DNS",
            80 => "HTTP",
            110 => "POP3",
            143 => "IMAP",
            443 => "HTTPS",
            3306 => "MySQL",
            5432 => "PostgreSQL",
            6379 => "Redis",
            27017 => "MongoDB",
            _ => "Unknown"
        };
    }
    static async Task Main()
    {
        Console.WriteLine("=== Advanced Port Scanner ===");
        Console.Write("Enter Target Ip Address: ");
        string TargetIp =Console.ReadLine()?.Trim();

        if(string.IsNullOrEmpty(TargetIp))
        {
            Console.WriteLine("Invalid IP Address. Exiting...");
            return;
        }
        if(!TryGetPort("Enter Start Port: ", out int startPort) || 
        ! TryGetPort("Enter end Port: ", out int endPort) || 
        startPort > endPort)
        {
            Console.WriteLine("Invalid Port Range. Exiting...");
            return;
        }

        Console.WriteLine($"\n Scanning {TargetIp} from part {startPort} to {endPort}");

        var openPorts = new ConcurrentBag<string>();

        var ports = new List<int>();
        for(int P=startPort; P<=endPort; P++)
        {
            ports.Add(P);
        }

        var options = new ParallelOptions {MaxDegreeOfParallelism = MaxdegreeOfParallelism};
        DateTime startTime =DateTime.Now;

        await Parallel.ForEachAsync(ports, options, async (port, cancellationToken ) =>
        {
            bool isOpen = await IsPortOpen(TargetIp, port, 500);
            if(isOpen)
            {
                string result = $"Port {port} is Open - {GetServiceName(port)}";
                Console.WriteLine(result);
                openPorts.Add(result);
            }
                       
        });
        if(!openPorts.IsEmpty)
        {
            await File.WriteAllLinesAsync("Scan-results.txt", openPorts);
            Console.WriteLine($"\nResults saved to 'scan-result.txt'");

        }
        Console.WriteLine($"\nScan Complete in {(DateTime.Now - startTime).TotalSeconds:F2} Second.");
    }
    static async Task<bool> IsPortOpen(string host, int port, int timeout)
    {
        try
        {
            using var client =new TcpClient();

            using var cts = new CancellationTokenSource(timeout);

            await client.ConnectAsync(host, port, cts.Token);
            return true;

        }
        catch
        {
            return false;
        }
    }
    static bool TryGetPort(string prompt, out int port)
    {
        Console.Write(prompt);
        return int.TryParse(Console.ReadLine(), out port) && port >= 1 && port <=65535;
    }
}
