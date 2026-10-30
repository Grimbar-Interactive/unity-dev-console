using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace GI.UnityToolkit.DevConsole
{
    /// <summary>Finds every <see cref="DevCommand"/> subclass at runtime and runs command lines against them.</summary>
    public static class DevCommandRegistry
    {
        private static Dictionary<string, DevCommand> _byWord;
        private static List<DevCommand> _commands;

        /// <summary>Every command once, sorted by name.</summary>
        public static IReadOnlyList<DevCommand> Commands
        {
            get
            {
                Build();
                return _commands;
            }
        }

        public static string Execute(string line)
        {
            Build();

            var tokens = Tokenize(line);
            if (tokens.Count == 0) return null;

            var word = tokens[0].TrimStart('/').ToLowerInvariant();
            if (!_byWord.TryGetValue(word, out var command))
                return $"<color=#ff6b6b>Unknown command '{word}'. Type /help for a list.</color>";

            tokens.RemoveAt(0);
            try
            {
                return command.Run(tokens);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return $"<color=#ff6b6b>/{command.Name} failed: {e.Message}</color>";
            }
        }

        private static void Build()
        {
            if (_byWord != null) return;

            _byWord = new Dictionary<string, DevCommand>();
            _commands = new List<DevCommand>();

            var type = typeof(DevCommand);
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] types;
                try
                {
                    types = assembly.GetTypes();
                }
                catch (System.Reflection.ReflectionTypeLoadException e)
                {
                    types = e.Types.Where(t => t != null).ToArray();
                }

                foreach (var candidate in types)
                {
                    if (candidate.IsAbstract || !type.IsAssignableFrom(candidate)) continue;
                    if (candidate.GetConstructor(Type.EmptyTypes) == null) continue;

                    var command = (DevCommand)Activator.CreateInstance(candidate);
                    _commands.Add(command);
                    Register(command.Name, command);
                    foreach (var alias in command.Aliases) Register(alias, command);
                }
            }

            _commands.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));
        }

        private static void Register(string word, DevCommand command)
        {
            word = word.ToLowerInvariant();
            if (_byWord.TryGetValue(word, out var existing))
            {
                Debug.LogWarning(
                    $"[DevConsole] '{word}' is claimed by both {existing.GetType().Name} and {command.GetType().Name}; keeping the first.");
                return;
            }

            _byWord[word] = command;
        }

        /// <summary>Splits on whitespace; "double quoted" text stays together as one token.</summary>
        public static List<string> Tokenize(string line)
        {
            var tokens = new List<string>();
            var sb = new StringBuilder();
            var inQuotes = false;
            var hasToken = false;

            foreach (var c in line)
            {
                if (c == '"')
                {
                    inQuotes = !inQuotes;
                    hasToken = true;
                }
                else if (char.IsWhiteSpace(c) && !inQuotes)
                {
                    if (hasToken) tokens.Add(sb.ToString());
                    sb.Clear();
                    hasToken = false;
                }
                else
                {
                    sb.Append(c);
                    hasToken = true;
                }
            }

            if (hasToken) tokens.Add(sb.ToString());
            return tokens;
        }
    }
}