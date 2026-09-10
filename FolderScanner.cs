using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PHelper
{
    public static class FolderScanner
    {
        private static readonly string[] IgnoredKeywords = {
            "unins", "crash", "setup", "update", "launcher", "redist", 
            "install", "vcredist", "dxwebsetup", "unitycrashhandler", "createredump"
        };

        public static List<string> GetDefaultGameFolders()
        {
            List<string> folders = new List<string>();

            string steamPath = @"C:\Program Files (x86)\Steam\steamapps\common";
            if (Directory.Exists(steamPath)) folders.Add(steamPath);

            string epicPath = @"C:\Program Files\Epic Games";
            if (Directory.Exists(epicPath)) folders.Add(epicPath);

            string gogPath = @"C:\Program Files (x86)\GOG Galaxy\Games";
            if (Directory.Exists(gogPath)) folders.Add(gogPath);

            string eaPath = @"C:\Program Files\EA Games";
            if (Directory.Exists(eaPath)) folders.Add(eaPath);

            return folders;
        }

        public static List<string> ScanForGameExecutables(List<string> directories)
        {
            List<string> foundGames = new List<string>();

            var searchOptions = new EnumerationOptions
            {
                IgnoreInaccessible = true,
                RecurseSubdirectories = true
            };

            foreach (var dir in directories)
            {
                if (!Directory.Exists(dir)) continue;

                try
                {
                    var exeFiles = Directory.EnumerateFiles(dir, "*.exe", searchOptions);

                    foreach (var file in exeFiles)
                    {
                        string fileName = Path.GetFileName(file).ToLower();
                        bool isJunk = IgnoredKeywords.Any(keyword => fileName.Contains(keyword));

                        if (!isJunk)
                        {
                            foundGames.Add(file);
                        }
                    }
                }
                catch (Exception)
                {
                    continue;
                }
            }

            return foundGames;
        }
    }
}