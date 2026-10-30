using System.Collections.Generic;
using System.Text;

namespace GI.UnityToolkit.DevConsole
{
    public class HelpCommand : DevCommand
    {
        public override string Name => "help";
        public override string Description => "Lists every command.";

        protected override string Help() => "/help takes no options. Add --help to any command to see its options.";

        protected override string Execute(IReadOnlyList<string> args)
        {
            var sb = new StringBuilder("Commands (add --help or --aliases to any of them):");
            foreach (var command in DevCommandRegistry.Commands)
            {
                sb.Append($"\n  <b>/{command.Name}</b> - {command.Description}");
            }
            return sb.ToString();
        }
    }
}