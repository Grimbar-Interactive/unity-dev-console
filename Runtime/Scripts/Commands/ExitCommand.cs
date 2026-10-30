using System.Collections.Generic;

#if !UNITY_EDITOR
using UnityEngine;
#endif

namespace GI.UnityToolkit.DevConsole
{
    public class ExitCommand : DevCommand
    {
        public override string Name => "exit";
        public override IReadOnlyList<string> Aliases { get; } = new[] { "quit", "close" };
        public override string Description => "Closes the game (or stops Play mode in the editor).";

        protected override string Help() => "/exit takes no options.";

        protected override string Execute(IReadOnlyList<string> args)
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
            return "Exiting...";
        }
    }
}