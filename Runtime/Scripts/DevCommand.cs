using System;
using System.Collections.Generic;

namespace GI.UnityToolkit.DevConsole
{
    /// <summary>
    /// Base class for dev console commands. Subclass it anywhere in the project and the registry picks it up
    /// at runtime; no registration needed. "--help" and "--aliases" are handled here for every command.
    /// </summary>
    public abstract class DevCommand
    {
        /// <summary>The command word without the slash, e.g. "exit".</summary>
        public abstract string Name { get; }

        /// <summary>Other words that run the same command, e.g. "quit" and "close".</summary>
        public virtual IReadOnlyList<string> Aliases => Array.Empty<string>();

        /// <summary>One line shown by /help.</summary>
        public abstract string Description { get; }

        /// <summary>Lists the options this command accepts. Shown for "--help"; don't include aliases.</summary>
        protected abstract string Help();
        
        protected virtual bool CanRun(IReadOnlyList<string> args, out string error)
        {
            error = null;
            return true;
        }

        protected abstract string Execute(IReadOnlyList<string> args);

        public string Run(IReadOnlyList<string> args)
        {
            // These flags win over every other argument.
            if (Contains(args, "--help")) return Help();
            if (Contains(args, "--aliases")) return Aliases.Count > 0 ? string.Join(", ", Aliases) : "No aliases.";

            if (!CanRun(args, out var error)) return error;

            return Execute(args);
        }

        protected static string Error(string message) => $"<color=#ff6b6b>{message}</color>";

        private static bool Contains(IReadOnlyList<string> args, string flag)
        {
            for (var i = 0; i < args.Count; i++)
            {
                if (string.Equals(args[i], flag, StringComparison.OrdinalIgnoreCase)) return true;
            }

            return false;
        }
    }
}