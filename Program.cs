using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;

namespace overflow_watcher
{
    class Program
    {
        static void Main(string[] args)
        {
            int processId = int.Parse(args[0]);

            System.Console.WriteLine($"monitoring process {processId}");

            long lastCount = 0;

            int i = 1;

            while (true)
            {
                var proc = Process.Start(new ProcessStartInfo
                {
                    RedirectStandardOutput = true,
                    FileName = "/usr/bin/nstat",
                    Arguments = "-az",
                });

		string poop = proc.StandardOutput.ReadToEnd();

                proc.WaitForExit();

                var overflowCount = long.Parse(Regex.Match(poop, "TcpExtListenOverflows[ ]*([0-9]+)").Groups[1].Value);

                if (lastCount > 0)
                {
                    var diff = overflowCount - lastCount;

                    if (diff > 0)
                        System.Console.WriteLine($"New overflows: {diff}");

                    if (diff >= 20)
                    {
                        System.Console.WriteLine("Dumping...");
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = "createdump",
                            WorkingDirectory = "/root/testing",
                            Arguments = $"-n {processId}"
                        }).WaitForExit();

                        File.Move($"/tmp/coredump.{processId}", $"./log-{i}-coreedump");

                        System.Console.WriteLine("Saving netstat...");

                        var netStat = Process.Start(new ProcessStartInfo
                        {
                            RedirectStandardOutput = true,
                            FileName = "/bin/netstat",
                            Arguments = "-n",
                        });

                        var netStatOutput = netStat.StandardOutput.ReadToEnd();

                        netStat.WaitForExit();

                        File.WriteAllText($"./log-{i}-netstat", netStatOutput);

                        System.Console.WriteLine("Done!");

                        i++;

                        System.Console.WriteLine("Sleeping a bit...");
                        Thread.Sleep(10000);
                    }
                }

                lastCount = overflowCount;
                Thread.Sleep(1000);
            }

        }
    }
}
