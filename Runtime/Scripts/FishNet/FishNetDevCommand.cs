#if FISHNET
using System.Collections.Generic;
using FishNet;
using JetBrains.Annotations;

namespace GI.UnityToolkit.DevConsole
{
    public abstract class FishNetDevCommand : DevCommand
    {
        /// <summary>When true, the command is refused unless this peer is the server.</summary>
        public virtual bool ServerOnly => true;
        
        [CanBeNull]
        protected override bool CanRun(IReadOnlyList<string> args, out string error)
        {
            if (ServerOnly && !InstanceFinder.IsServerStarted)
            {
                error = Error($"Only the server host can run /{Name}.");
            }

            return base.CanRun(args, out error);
        }
    }
}
#endif