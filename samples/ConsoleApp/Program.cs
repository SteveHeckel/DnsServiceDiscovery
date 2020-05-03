using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq.Expressions;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Threading.Tasks;
using Mittosoft.DnsServiceDiscovery.Helpers;
using Mittosoft.DnsServiceDiscovery.Records;

namespace Mittosoft.DnsServiceDiscovery.Cli
{
    class Program
    {
        private static DnsServiceDiscovery _serviceDiscovery = new DnsServiceDiscovery();

        private static async Task Main(string[] args)
        {
            _serviceDiscovery.BrowseEvent += (_, bea) => Console.WriteLine($"Browse - Service {(bea.EventType == BrowseEventType.Added ? "Added" : "Removed")}: {bea.Descriptor}");
            _serviceDiscovery.ResolveEvent += (_, rea) => Console.WriteLine($"Service Resolved: {rea}");
            _serviceDiscovery.RegistrationEvent += (_, eargs) =>
            {
                if (eargs.EventType == RegistrationEventType.Error)
                    Console.WriteLine($"Register: Service Registration Error: {eargs.Error} while trying to register: {eargs.Descriptor}");
                else
                    Console.WriteLine($"Register: Service Registration {eargs.EventType}: {eargs.Descriptor}");
            };
            _serviceDiscovery.LookupEvent += (s, lea) =>
            {

                var address = lea.IPAddress?.ToString() ?? string.Empty;
                Console.WriteLine(DateTime.Now.ToString(CultureInfo.InvariantCulture).PadRight(TimestampColWidth) +
                                  lea.EventType.ToString().PadRight(EventTypeColWidth) +
                                  lea.InterfaceIndex.ToString().PadRight(InterfaceColWidth) +
                                  lea.HostName.PadRight(NameColWidth) +
                                  address.PadRight(AddressColWidth));
            };

            try
            {
                await ProcessCommandLineAsync(args);
            }
            catch (DnsServiceException dse)
            {
                Console.WriteLine($"DNS Service Exception: {dse.Message}");
            }
            catch (Exception e)
            {
                Console.WriteLine($"Exception caught running command: {e.Message}");
            }
        }

        private static readonly Dictionary<string, (string argsInfo, int minArgs, Func<string[], Task> execute)> MainUsageInfo = new Dictionary<string, (string argsInfo, int minArgs, Func<string[], Task> execute)>()
        {
            { "browse",   ("       <Type> <Domain>                     (Browse for service instances)", 1, BrowseAsync)},
            { "register", ("<Name> <Type> <Domain> <Port> [<TXT>...]             (Register a service)", 4, RegisterAsync)},
            { "resolve",  ("<Name> <Type> <Domain>                       (Resolve a service instance)", 2, ResolveAsync)},
            { "lookup",   ("    v4/v6/v4v6 <name>              (Get address information for hostname)", 2, LookupAsync)}
        };

        private static readonly Dictionary<string, (int argCount, string usageInfo, Action<string[]> optionAction)> OptionsUsageInfo =
            new Dictionary<string, (int argCount, string usageInfo, Action<string[]> optionAction)>()
        {
            {"interface",  (1, "                                       (Run command on specific interface index)", args => int.TryParse(args[0], out _opInterfaceIndex))},
            {"timeout",    (0, "                                       (Set kDNSServiceFlagsTimeout flag)", args => _withTimeout = true)}
        };

        private static bool _withTimeout;
        private static int _opInterfaceIndex;

        private const int TimestampColWidth = 20;
        private const int EventTypeColWidth = 14;
        private const int InterfaceColWidth = 10;
        private const int NameColWidth = 40;
        private const int AddressColWidth = 44;

        private static async Task LookupAsync(string[] args)
        {
            var protocolFlags = GetProtocolFlags(args[0]);
            var name = args[1];

            Console.WriteLine($"Lookup [{name}]");

            Console.BufferWidth = 160;
            Console.WriteLine("Timestamp".PadRight(TimestampColWidth) + "Event".PadRight(EventTypeColWidth) + "Interface".PadRight(InterfaceColWidth) +
                              "Hostname".PadRight(NameColWidth) + "Address".PadRight(AddressColWidth));

            var op = await _serviceDiscovery.LookupAsync(name, protocolFlags, _withTimeout, (uint)_opInterfaceIndex);
        }

        private static ProtocolFlags GetProtocolFlags(string protocolString)
        {
            protocolString = protocolString.ToLower();
            var flags = protocolString switch
            {
                "v4"     => ProtocolFlags.IPv4,
                "v6"     => ProtocolFlags.IPv6,
                "v4v6"   => ProtocolFlags.IPv4 | ProtocolFlags.IPv6,
                "v6v4"   => ProtocolFlags.IPv4 | ProtocolFlags.IPv6,
                "udp"    => ProtocolFlags.UDP,
                "tcp"    => ProtocolFlags.TCP,
                "udptcp" => ProtocolFlags.TCP | ProtocolFlags.UDP,
                "tcpudp" => ProtocolFlags.TCP | ProtocolFlags.UDP,
                _        => throw new UsageException("Unrecognized protocol value")
            };

            return flags;
        }

        private static async Task ResolveAsync(string[] args)
        {
            var name = args[0];
            var type = GetType(args[1]);
            var domain = args.Length < 3 || args[2] == "." ? "local" : args[2];

            Console.WriteLine($"Resolve [{name}.{type}.{domain}]");

            await _serviceDiscovery.ResolveAsync(name, type, domain, (uint)_opInterfaceIndex);
        }

        private static string GetType(string type)
        {
            if (string.IsNullOrEmpty(type) || type == ".")
                type = "_http._tcp";
            else if (type.IndexOf('.') == 0)
                type += "._tcp";

            return type;
        }

        private static async Task RegisterAsync(string[] args)
        {
            // Allow '.' to mean empty string
            var name = args[0] == "." ? string.Empty : args[0];
            var type = args[1];
            var domain = args[2] == "." ? string.Empty : args[2];
            if (!ushort.TryParse(args[3], out var port))
                throw new UsageException($"The Port argument contains invalid data [{args[3]}]");

            byte[] txtRecord = null;
            if (args.Length > 4)
            {
                var trb = new TxtRecordBuilder(args.SubArray(4, args.Length - 4));
                txtRecord = trb.GetBytes();
            }

            Console.WriteLine($"Registering service instance [{name}] with service type [{type}]");

            await _serviceDiscovery.RegisterAsync(name, type, domain, null, port, txtRecord, (uint)_opInterfaceIndex);
        }

        private static async Task BrowseAsync(string[] args)
        {
            var type = args[0];
            // Allow '.' to mean empty string
            var domain = args.Length < 2 || args[1] == "." ? string.Empty : args[1];

            await _serviceDiscovery.BrowseAsync(type, domain, (uint)_opInterfaceIndex);
        }

        private static async Task ProcessCommandLineAsync(string[] args)
        {
            // Go through args and handle and pluck out the options
            args = ProcessCommandLineOptions(args);

            if (args.Length != 0 && MainUsageInfo.ContainsKey(args[0]))
            {
                var cmdInfo = MainUsageInfo[args[0]];
                var cargs = args.SubArray(1, args.Length - 1);
                if (cargs.Length < cmdInfo.minArgs)
                {
                    PrintUsage();
                    return;
                }

                try
                {
                    await cmdInfo.execute(cargs);
                    Console.ReadKey();
                }
                catch (UsageException ue)
                {
                    Console.WriteLine(ue.Message);
                    PrintUsage();
                }
            }
            else
            {
                PrintUsage();
            }
        }

        private static string[] ProcessCommandLineOptions(string[] args)
        {
            var newCommandArgsList = new List<string>();
            for (var i = 0; i < args.Length; i++)
            {
                var arg = args[i];
                if (arg.Length > 2 && arg.Substring(0, 2) == "--")
                {
                    var option = arg[2..];
                    if (OptionsUsageInfo.ContainsKey(option))
                    {
                        var oui = OptionsUsageInfo[option];
                        var argsCount = oui.argCount;
                        string[] oargs = null;
                        if (argsCount != 0)
                        {
                            var firstArgIndex = i + 1;
                            if (firstArgIndex + argsCount > args.Length)
                                throw new UsageException($"Not enough parameters for option {arg}");
                            oargs = args[firstArgIndex..(firstArgIndex + argsCount)];
                            i += argsCount;
                        }

                        OptionsUsageInfo[option].optionAction(oargs);
                    }
                    else
                        throw new UsageException($"Unrecognized option: {arg}");
                }
                else
                {
                    newCommandArgsList.Add(arg);
                }
            }

            return newCommandArgsList.ToArray();
        }

        private static void PrintUsage()
        {
            Console.WriteLine("Usage:");
            foreach (var (key, value) in MainUsageInfo)
                Console.WriteLine($"{key?.PadRight(12)}{value.argsInfo}");
        }

       
        //private static string[] MainUsageStrings = new[]
        //{
        //    "-E                              (Enumerate recommended registration domains)",
        //    "-F                                  (Enumerate recommended browsing domains)",
        //    "-R <Name> <Type> <Domain> <Port> [<TXT>...]             (Register a service)",
        //    "-B        <Type> <Domain>                     (Browse for service instances)",
        //    "-L <Name> <Type> <Domain>                       (Resolve a service instance)",
        //    "-Q <name> <rrtype> <rrclass>             (Generic query for any record type)",
        //    "-Z        <Type> <Domain>               (Output results in Zone File format)",
        //    "-G     v4/v6/v4v6 <name>              (Get address information for hostname)",
        //    "-H                                   (Print usage for complete command list)",
        //    "-V                (Get version of currently running daemon / system service)",
        //};

        //private static string[] OptionsStrings = new[]
        //{
        //    "-A                                  (Test Adding/Updating/Deleting a record)",
        //    "-C <FQDN> <rrtype> <rrclass>               (Query; reconfirming each result)",
        //    "-D <name> <rrtype> <rrclass>(Validate query for any record type with DNSSEC)",
        //    "-I               (Test registering and then immediately updating TXT record)",
        //    "-N                                         (Test adding a large NULL record)",
        //    "-M                  (Test creating a registration with multiple TXT records)",
        //    "-P <Name> <Type> <Domain> <Port> <Host> <IP> [<TXT>...]              (Proxy)",
        //    "-S                             (Test multiple operations on a shared socket)",
        //    "-T                                        (Test creating a large TXT record)",
        //    "-U                                              (Test updating a TXT record)",
        //    "-X udp/tcp/udptcp <IntPort> <ExtPort> <TTL>               (NAT Port Mapping)",
        //    "-ble                                      (Use kDNSServiceInterfaceIndexBLE)",
        //    "-g v4/v6/v4v6 <name>        (Validate address info for hostname with DNSSEC)",
        //    "-i <Interface>             (Run dns-sd cmd on a specific interface (en0/en1)",
        //    "-includep2p                            (Set kDNSServiceFlagsIncludeP2P flag)",
        //    "-includeAWDL                          (Set kDNSServiceFlagsIncludeAWDL flag)",
        //    "-intermediates                (Set kDNSServiceFlagsReturnIntermediates flag)",
        //    "-ku                                   (Set kDNSServiceFlagsKnownUnique flag)",
        //    "-lo                              (Run dns-sd cmd using local only interface)",
        //    "-optional                        (Set kDNSServiceFlagsValidateOptional flag)",
        //    "-p2p                                      (Use kDNSServiceInterfaceIndexP2P)",
        //    "-q <name> <rrtype> <rrclass> (Equivalent to -Q with kDNSServiceFlagsSuppressUnusable set)",
        //    "-tc                        (Set kDNSServiceFlagsBackgroundTrafficClass flag)",
        //    "-test                                      (Run basic API input range tests)",
        //    "-t1                                  (Set kDNSServiceFlagsThresholdOne flag)",
        //    "-tFinder                          (Set kDNSServiceFlagsThresholdFinder flag)",
        //    "-timeout                                  (Set kDNSServiceFlagsTimeout flag)",
        //    "-unicastResponse                  (Set kDNSServiceFlagsUnicastResponse flag)",
        //    "-autoTrigger                          (Set kDNSServiceFlagsAutoTrigger flag)",
        //};
    }

    public class UsageException : Exception
    {
        public UsageException(string message) : base(message)
        {
        }
    }
}
