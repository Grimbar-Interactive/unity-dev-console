#if FISHNET
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FishNet;
using FishNet.Managing;
using FishNet.Transporting;

namespace GI.UnityToolkit.DevConsole
{
    public class ServerCommand : FishNetDevCommand
    {
        public override string Name => "server";

        public override string Description =>
            "Manages the server connection: start [port] | stop | join <address> [port] | leave | info | clients.";

        protected override string Help() =>
            "/server start [port] - start the server; the transport's default port if none is given\n" +
            "/server stop - stop the server (only if we are the server)\n" +
            "/server join <address> [port] - connect to a server (also: connect); the transport's default port if none is given\n" +
            "/server leave - disconnect this client (also: disconnect); a host's server keeps running\n" +
            "/server info - show whether we're a client, server or host, and the address and port in use\n" +
            "/server clients - list the clients connected to this server (server only)";

        protected override string Execute(IReadOnlyList<string> args)
        {
            var manager = InstanceFinder.NetworkManager;
            if (manager == null) return Error("No NetworkManager in the scene.");
            if (args.Count == 0) return Help();

            switch (args[0].ToLowerInvariant())
            {
                case "start":
                    if (manager.IsServerStarted) return Error("The server is already running.");
                    ushort startPort = 0;
                    if (args.Count > 1 && !TryParsePort(args[1], out startPort)) return Error(InvalidPort(args[1]));
                    var serverStarted = args.Count > 1
                        ? manager.ServerManager.StartConnection(startPort)
                        : manager.ServerManager.StartConnection();
                    return serverStarted ? "Server starting." : Error("Could not start the server.");

                case "stop":
                    if (!manager.IsServerStarted) return Error("We are not the server.");
                    return manager.ServerManager.StopConnection(true)
                        ? "Server stopped."
                        : Error("Could not stop the server.");

                case "join":
                case "connect":
                    if (manager.IsClientStarted) return Error("Already connected or connecting.");
                    if (args.Count < 2) return Error("/server join needs an address. Try /server --help.");
                    ushort joinPort = 0;
                    if (args.Count > 2 && !TryParsePort(args[2], out joinPort)) return Error(InvalidPort(args[2]));
                    var clientStarted = args.Count > 2
                        ? manager.ClientManager.StartConnection(args[1], joinPort)
                        : manager.ClientManager.StartConnection(args[1]);
                    return clientStarted ? "Joining server." : Error("Could not start the client.");

                case "leave":
                case "disconnect":
                    if (!manager.IsClientStarted) return Error("Not connected to a server.");
                    return manager.ClientManager.StopConnection() ? "Disconnected." : Error("Could not disconnect.");

                case "info":
                    return Info(manager);

                case "clients":
                    return Clients(manager);

                default:
                    return Error($"Unknown option '{args[0]}'.\n{Help()}");
            }
        }

        private static string Info(NetworkManager manager)
        {
            if (!manager.IsServerStarted && !manager.IsClientStarted)
                return "Role: offline (not hosting or connected).";

            var transport = manager.TransportManager.Transport;
            var port = transport.GetPort();
            var builder = new StringBuilder();

            builder.Append("Role: ").Append(manager.IsHostStarted ? "Host (client + server)" :
                manager.IsServerStarted ? "Server" : "Client");

            if (manager.IsServerStarted)
            {
                var bind = transport.GetServerBindAddress(IPAddressType.IPv4);
                if (string.IsNullOrEmpty(bind)) bind = "all interfaces";
                builder.Append($"\nHosting on {bind}:{port} ({manager.ServerManager.Clients.Count} client(s))");
            }

            if (manager.IsClientStarted)
            {
                var connection = manager.ClientManager.Connection;
                var connected = connection != null && connection.IsActive;
                if (!manager.IsServerStarted)
                    builder.Append(
                        $"\n{(connected ? "Connected to" : "Connecting to")} {transport.GetClientAddress()}:{port}");
                if (connected) builder.Append($"\nLocal client id: {connection.ClientId}");
            }

            return builder.ToString();
        }

        private static string Clients(NetworkManager manager)
        {
            if (!manager.IsServerStarted) return Error("Only the server can list clients.");

            var clients = manager.ServerManager.Clients;
            if (clients.Count == 0) return "No clients connected.";

            var builder = new StringBuilder($"{clients.Count} client(s) connected:");
            foreach (var connection in clients.Values.OrderBy(c => c.ClientId))
            {
                builder.Append($"\n#{connection.ClientId}  {connection.GetAddress()}");
                if (connection.IsLocalClient) builder.Append("  [host]");
                builder.Append($"  authenticated: {(connection.IsAuthenticated ? "yes" : "no")}");
                builder.Append($"  objects: {connection.Objects.Count}");
                builder.Append($"  player: {(connection.FirstObject != null ? connection.FirstObject.name : "none")}");
            }

            return builder.ToString();
        }

        private static bool TryParsePort(string text, out ushort port) => ushort.TryParse(text, out port) && port > 0;

        private static string InvalidPort(string text) => $"'{text}' is not a valid port (1-65535).";
    }
}
#endif