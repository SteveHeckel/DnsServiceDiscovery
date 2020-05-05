using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using System.Windows.Forms;
using Mittosoft.DnsServiceDiscovery;
using Mittosoft.DnsServiceDiscovery.Operations;
using ResolveEventArgs = Mittosoft.DnsServiceDiscovery.ResolveEventArgs;

namespace Mittosoft.DnsServiceBrowser
{
    public partial class DnsServiceBrowserForm : Form
    {
        private readonly DnsServiceDiscovery.DnsServiceDiscovery _service;

        public DnsServiceBrowserForm()
        {
            InitializeComponent();

            treeViewServices.TreeViewNodeSorter = new BrowserNodeComparer();
            treeViewServices.Sorted = true;

            // don't move this as the ctor captures the SynchronizationContext for callbacks
            _service = new DnsServiceDiscovery.DnsServiceDiscovery();
            _service.BrowseEvent += ServiceBrowseEventHandler;
            _service.ResolveEvent += ServiceResolveEventHandler;
            _service.LookupEvent += ServiceLookupEventHandler;
        }

        private IOperationToken _browseServicesToken;

        private async void ServiceBrowserForm_Load(object sender, EventArgs e)
        {
            try
            {
                if (!await _service.ProbeServiceAsync())
                {
                    WriteLine("The mDNSResponder Service is not running");
                    return;
                }

                WriteLine(@"Starting service type enumeration meta-query");
                _browseServicesToken = await _service.BrowseAsync(@"_services._dns-sd._udp", string.Empty, InterfaceIndex.Any);
                _browseServicesToken.StateChanged += OperationStatechanged;

                void OperationStatechanged(object s, OperationState state)
                {
                    if (state == OperationState.Canceled || state == OperationState.Faulted)
                    {
                        WriteLine($"Service type enumeration operation state changed to: {state}");
                        _browseServicesToken.StateChanged -= OperationStatechanged;
                    }
                };
            }
            catch (DnsServiceException dse)
            {
                treeViewServices.ResumeDrawing();
                WriteLine($"Service type enumeration browse operation failed: {dse.Message}");
            }
        }

        private async void ServiceBrowseEventHandler(object sender, BrowseEventArgs e)
        {
            treeViewServices.SuspendLayout();
            if (sender == _browseServicesToken)
            {
                var protocol = e.Descriptor.ServiceType.TrimEnd('.');
                var i = protocol.LastIndexOf('.');
                if (i != -1)
                    protocol = protocol.Substring(0, i);
                var serviceType = e.Descriptor.InstanceName + "." + protocol;

                if (e.EventType == BrowseEventType.Added)
                    await AddServiceTypeNode(serviceType, e.Descriptor.InterfaceIndex);
                else if (treeViewServices.Nodes.ContainsKey(serviceType))
                    await RemoveServiceTypeNode(serviceType, e.Descriptor.InterfaceIndex);
            }
            else // Service instance
            {
                if (sender is IOperationToken token && token.Context is TreeNode serviceTypeNode)
                {
                    if (e.EventType == BrowseEventType.Added)
                    {
                        WriteLine($"Adding node for service instance [{e.Descriptor.InstanceName}] of type [{e.Descriptor.ServiceType}] in domain [{e.Descriptor.Domain}] on interface [{e.Descriptor.InterfaceIndex}]");
                        await AddServiceInstanceNode(serviceTypeNode, e.Descriptor);
                    }
                    else
                    {
                        WriteLine($"Removing node for service instance [{e.Descriptor.InstanceName}] of type [{e.Descriptor.ServiceType}] in domain [{e.Descriptor.Domain}] on interface [{e.Descriptor.InterfaceIndex}]");
                        if (serviceTypeNode.Nodes.ContainsKey(e.Descriptor.ToString()))
                        {
                            var node = serviceTypeNode.Nodes[e.Descriptor.ToString()];
                            node.Remove();
                            await CancelOperations(node);
                        }
                    }
                }
            }
            if (!e.MoreComing)
                treeViewServices.ResumeLayout();
        }

        private async Task AddServiceTypeNode(string type, uint interfaceIndex)
        {
            var node = new TreeNode(type) {Name = type};
            // We'll get multiple events for the same type if it exists on multiple interfaces, ignore any after the first
            if (!treeViewServices.Nodes.ContainsKey(type))
            {
                WriteLine($"Adding node for service type [{type}]");
                treeViewServices.Nodes.Add(node);
                try
                {
                    WriteLine($"Browsing for service instances of [{type}]");
                    node.Tag = await _service.BrowseAsync(type, string.Empty, InterfaceIndex.Any, node);
                }
                catch (DnsServiceException dse)
                {
                    WriteLine($"Browsing operation for service instances of type [{type}] failed: {dse.Message}");
                    node.Remove();
                }
            }
        }

        private async Task RemoveServiceTypeNode(string type, uint interfaceIndex)
        {
            var key = $"{type} ({interfaceIndex})";
            if (treeViewServices.Nodes.ContainsKey(key))
            {
                var node = treeViewServices.Nodes[key];
                WriteLine($"Removing node for service type [{type}] on interface [{interfaceIndex}]");
                node.Remove();
                await CancelOperations(node);
            }
        }

        private async Task AddServiceInstanceNode(TreeNode serviceTypeNode, ServiceDescriptor descriptor)
        {
            var serviceInstanceNode = new TreeNode(descriptor.InstanceName) { Name = descriptor.ToString() };
            serviceTypeNode.Nodes.Add(serviceInstanceNode);
            AddNetworkInterfaceNode(serviceInstanceNode, descriptor.InterfaceIndex);
            try
            {
                serviceInstanceNode.Tag = await _service.ResolveAsync(descriptor.InstanceName, descriptor.ServiceType, descriptor.Domain,
                    descriptor.InterfaceIndex, serviceInstanceNode);
            }
            catch (DnsServiceException e)
            {
                WriteLine(
                    $"Resolve operation for service instance [{descriptor.InstanceName}] of type [{descriptor.ServiceType}] failed: {e.Message}");
            }
        }

        private void AddNetworkInterfaceNode(TreeNode node, uint interfaceIndex)
        {
            var ni = GetNetworkInterface(interfaceIndex);
            if (ni != null)
            {
                var key = $"Interface: {ni.Name} - {ni.Description}";
                var newNode = new TreeNode(key) {Name = key, Tag = ni};
                node.Nodes.Add(newNode);
                newNode.Nodes.Add($"Index: {interfaceIndex}");
                newNode.Nodes.Add($"Physical: {ni.GetPhysicalAddress()}");
                foreach (var address in ni.GetIPProperties().UnicastAddresses)
                {
                    newNode.Nodes.Add($"{address.Address.AddressFamily}: {address.Address.ToString()}");
                }
            }
        }

        private NetworkInterface GetNetworkInterface(uint interfaceIndex)
        {
            var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
            var networkInterface = networkInterfaces.FirstOrDefault(ni =>
            {
                var index = ni.GetIPProperties().GetIPv4Properties()?.Index ??
                            ni.GetIPProperties().GetIPv6Properties()?.Index ?? 0;
                if (index != 0)
                    return index == interfaceIndex;

                return false;
            });

            return networkInterface;
        }

        private async void ServiceResolveEventHandler(object sender, ResolveEventArgs e)
        {
            if (sender is IOperationToken token && token.Context is TreeNode node)
            {
                treeViewServices.SuspendLayout();
                WriteLine($"Adding node for host [{e.HostName}], Full Service Name [{e.FullName}]");
                await AddHostInfoNodes(node, e);
                // Todo: we could cancel this operation here
            }
            if (!e.MoreComing)
                treeViewServices.ResumeLayout();
        }

        private async Task AddHostInfoNodes(TreeNode node, ResolveEventArgs args)
        {
            var hostNode = new TreeNode("Host: " + args.HostName) {Name = args.HostName};
            node.Nodes.Add(hostNode);
            try
            {
                hostNode.Tag = await _service.LookupAsync(args.HostName, ProtocolFlags.IPv4v6, true, args.InterfaceIndex, hostNode);
            }
            catch (DnsServiceException dse)
            {
                WriteLine($"Lookup operation for host [{args.HostName}] failed: {dse.Message}");
            }

            node.Nodes.Add(new TreeNode("Full Name: " + args.FullName));
            node.Nodes.Add(new TreeNode("Port: " + args.Port));
            if (args.TxtRecords != null)
            {
                var txtRecNode = new TreeNode("TXT Records");
                node.Nodes.Add(txtRecNode);
                foreach (var txtRecord in args.TxtRecords)
                {
                    var recNode = new TreeNode(txtRecord.ToString());
                    txtRecNode.Nodes.Add(recNode);
                }
            }
        }

        private void ServiceLookupEventHandler(object sender, LookupEventArgs e)
        {
            if ((e.EventType == LookupEventType.Added || e.EventType == LookupEventType.Removed) &&
                sender is IOperationToken token && token.Context is TreeNode node &&
                node.Name == e.HostName)
            {
                treeViewServices.SuspendLayout();
                var key = $"{e.IPAddress.AddressFamily}: {e.IPAddress}";
                switch (e.EventType)
                {
                    case LookupEventType.Added:
                    {
                        WriteLine(
                            $"Adding address node for host [{e.HostName}], address family [{e.IPAddress.AddressFamily}], address is [{e.IPAddress}]");
                        var addrNode = new TreeNode(key) {Name = key};
                        node.Nodes.Add(addrNode);
                        break;
                    }
                    case LookupEventType.Removed:
                    {
                        WriteLine(
                            $"Removing address node for host [{e.HostName}], address family [{e.IPAddress.AddressFamily}], address is [{e.IPAddress}]");
                        node.Nodes.RemoveByKey(key);
                        break;
                    }
                }
            }
            if (!e.MoreComing)
                treeViewServices.ResumeLayout();
        }

        private static async Task CancelOperations(TreeNode node)
        {
            await EnumerateNodes(node, async (n) =>
            {
                if (n.Tag is IOperationToken token)
                {
                    await token.CancelAsync();
                    n.Tag = null;
                }
            });
        }

        private static async Task EnumerateNodes(TreeNode node, Func<TreeNode, Task> nodeFunc)
        {
            await nodeFunc(node);
            foreach (TreeNode cNode in node.Nodes)
                await EnumerateNodes(cNode, nodeFunc);
        }

        #region Logging Methods

        private const int MaxLines = 500;

        public void WriteLine(string line)
        {
            AddText(line + Environment.NewLine);
        }

        public void AddText(string text)
        {
            if (textBoxMessages.InvokeRequired)
            {
                textBoxMessages.Invoke(new Action<string>(AddText), new object[] { text });
                return;
            }

            if (textBoxMessages.Lines.Length >= MaxLines)
            {
                var rem = (textBoxMessages.Lines.Length - MaxLines) + 1;
                const int ns = MaxLines - 1;
                var lines = new string[ns];
                Array.Copy(textBoxMessages.Lines, rem, lines, 0, MaxLines - 1);
                textBoxMessages.Lines = lines;
            }

            textBoxMessages.AppendText(text);
            textBoxMessages.ScrollToCaret();
        }

        #endregion

        private void treeViewServices_KeyPress(object sender, KeyPressEventArgs e)
        {
        }

        private void treeViewServices_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.C)
            {
                Clipboard.SetText(treeViewServices.SelectedNode.Text);
                e.SuppressKeyPress = true;
            }
        }
    }

    public class BrowserNodeComparer : IComparer<TreeNode>, IComparer
    {
        public int Compare(TreeNode x, TreeNode y)
        {
            if (x == null || y == null)
                return 0;

            if (x.Tag is NetworkInterface)
                return -1;
            
            return y.Tag is NetworkInterface ? 1 : string.CompareOrdinal(x.Text, y.Text);
        }

        public int Compare(object x, object y)
        {
            return Compare(x as TreeNode, y as TreeNode);
        }
    }
}
