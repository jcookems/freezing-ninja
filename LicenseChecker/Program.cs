using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace LicenseChecker
{
    internal class Program
    {
        //        static string path = @"C:\Users\jcooke\Desktop\WindowsAzure-azure-sdk-for-java";
        static string path = @"C:\Users\jcooke\Desktop\azure-mobile-services-master";
        //        static string path = @"C:\dd\git\jcooke\socket.io-servicebus";
        //        static string path = @"C:\dd\git\jcooke\azure-sdk-for-java-pr";
        //        static string path = @"C:\users\jcooke\desktop\azure-sdk-for-java-pr-dev";


        private static string[] skipExtensions = new string[] {
            ".png",
            ".jpg",
            ".gitignore",
            ".storyboard",
            ".jar",
            ".nupkg",
            ".lnk",
            ".nuspec",
            ".dll",
            ".sln",
            ".csproj",
            ".jsproj",
            ".Designer.cs",
            ".resx",
            ".json",
            ".pfx",
            ".ps1",
            ".rtf",
            ".targets",
            ".config",
            ".pbxproj",
            ".xib",
            ".plist",
            ".pch", 
            ".strings",
            ".css",
            ".properties",
            ".bat",
            ".sh",
            ".txt",
            ".md",
            ".command",
            ".xcworkspacedata",
            ".library",
            ".resjson",
            ".user",
            ".appxmanifest",
            ".xml",
            ".pri",
            ".min.js",
        };
        private static string[] skipFiles = new string[] {
            "package.html",
            "AssemblyInfo.cs",
            "NuGet.targets",
            "_._",
            ".gitattributes",
            ".gitmodules",
            ".classpath",
            ".pmd",
            ".project",
            "WindowsAzureMobileServices",
            "Settings.settings",
        };
        private static string[] skipFolders = new string[] {
           @"\.git\",
           @"\build\",
           @"\microsoft-azure-api\target\",
           @"\nbproject\", 
           @"\.settings\", 
           @"\.yardoc\", 
           @"\doc\",
           @"\test\fixtures\",
           @"node_modules\",
           @"\quickstart\",
        };

        private static string[] firstLicenseLines =
        {
            @"Copyright Microsoft Corporation",
            @"Copyright (c) Microsoft. All rights reserved.",
            @"Copyright (c) Microsoft.  All rights reserved.",
            @"Copyright (c) Microsoft Corporation. All rights reserved.",
            @"Copyright (c) Microsoft Open Technologies, Inc.",
        };

        private static string[] licenseText = new string[] { };
        //@"
        //Licensed under the Apache License, Version 2.0 (the ""License"");
        //you may not use this file except in compliance with the License.
        //You may obtain a copy of the License at
        //http://www.apache.org/licenses/LICENSE-2.0
        //
        //Unless required by applicable law or agreed to in writing, software
        //distributed under the License is distributed on an ""AS IS"" BASIS,
        //WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
        //See the License for the specific language governing permissions and
        //limitations under the License.".Split(new string[] { "\r\n" }, StringSplitOptions.None);

        private static void Main(string[] args)
        {
            RecursiveValidator.doit(path, CheckLicense, "license");
           //      RecursiveValidator.doit(path, checkASCII, "ASCII");
            //      RecursiveValidator.doit(path, removeEOLWhitespace, "EOL Whitespace");
            ReadLine();
        }

        class RecursiveValidator
        {
            private int goodCounter = 0;
            private int totalCounter = 0;
            private List<string> badFiles = new List<string>();

            public static void doit(string targetPath, Func<string[], string, bool> validator, string description)
            {
                RecursiveValidator x = new RecursiveValidator();
                x.doitWorker(targetPath, validator, description);

                WriteLine();
                foreach (string s in x.badFiles)
                {
                    WriteLine(s);
                }

                WriteLine();
                WriteLine(x.goodCounter + " out of " + x.totalCounter + " pass the " + description + " test");

                foreach (var xx in x.badFiles.Select(p => new { ext = p.Split('.').Reverse().First(), name = p }).GroupBy(p => p.ext))
                {
                    WriteLine(xx.Key);
                    foreach (var yy in xx)
                    {
                        WriteLine("    " + yy.name);
                    }
                }
            }

            private void doitWorker(string targetPath, Func<string[], string, bool> validator, string description)
            {
                foreach (var fileName in Directory.GetFiles(targetPath))
                {
                    doitWorkerWorker(fileName, validator);
                }

                foreach (var d in Directory.GetDirectories(targetPath))
                {
                    doitWorker(Path.Combine(targetPath, d), validator, description);
                }
            }

            private void doitWorkerWorker(string fileName, Func<string[], string, bool> validator)
            {
                if (skipExtensions.Any(p => fileName.ToLowerInvariant().EndsWith(p.ToLowerInvariant())) ||
                    skipFiles.Any(p => fileName.ToLowerInvariant().EndsWith(p.ToLowerInvariant())) ||
                    skipFolders.Any(p => fileName.ToLowerInvariant().Contains(p.ToLowerInvariant())))
                {
                    return;
                }

                string[] lines = File.ReadAllLines(fileName);

                bool good = validator(lines, fileName);
                Write(good ? "." : "X");
                if (good)
                {
                    goodCounter++;
                }
                else
                {
                    badFiles.Add(fileName);
                }

                totalCounter++;
                return;
            }
        }

        private static bool checkASCII(string[] lines, string fileName)
        {
            var nonAsciiLines = lines.Where(p => p.Any(q => ((int)q) > 127)).ToArray();
            if (nonAsciiLines.Length > 0)
            {
                WriteLine(fileName);
                foreach (String s in nonAsciiLines)
                {
                    WriteLine(s);
                    WriteLine(string.Join("", s.Select(q => (((int)q) > 127) ? "*" : " ")));
                    WriteLine("");
                }
            }
            return nonAsciiLines.Length == 0;
        }

        private static bool removeEOLWhitespace(string[] lines, string fileName)
        {
            var newLines = lines.Select(p => p.TrimEnd()).ToList();
            if (newLines.Last() != "") newLines.Add("");
            File.WriteAllText(fileName, string.Join("\n", newLines));
            return lines.Any(p => p.TrimEnd() != p);
        }

        private static bool CheckLicense(string[] lines, string fileName)
        {
            List<string> ret = new List<string>();
            var potentialStarts = lines.Select((p, i) => new { Index = i, Line = p }).
                Where(p => firstLicenseLines.Any(q => p.Line.Contains(q))).
                Select(p => p.Index);

            bool gotMatch = false;
            bool wroteOnce = false;

            foreach (var firstLine in potentialStarts)
            {
                if (lines.Length >= firstLine + licenseText.Length && firstLine < 10)
                {
                    bool thisTryOK = true;
                    int startIndex = -1;
                    foreach (var startLine in firstLicenseLines)
                    {
                        startIndex = lines[firstLine].IndexOf(startLine);
                        if (startIndex != -1)
                        {
                            break;
                        }
                    }
                    string prefix = lines[firstLine].Substring(0, startIndex).TrimEnd();
                    if (prefix.EndsWith("LICENSE:"))
                    {
                        prefix = prefix.Replace("LICENSE:", "").TrimEnd();
                    }

                    for (int k = 0; k < licenseText.Length; k++)
                    {
                        int i = firstLine + 1;
                        if (lines[i + k].Length < prefix.Length)
                        {
                            thisTryOK = false;
                            if (!wroteOnce)
                            {
                                WriteLine();
                                WriteLine(fileName);
                                wroteOnce = true;
                            }
                            WriteLine("Line: " + (i + k));
                            WriteLine("'" + lines[i + k] + "'");
                            WriteLine("'" + licenseText[k] + "'");
                        }
                        else
                        {
                            bool singleMatch = lines[i + k].Substring(prefix.Length).Trim() == licenseText[k].Trim();
                            if (!singleMatch && k == 0)
                            {
                                if (lines[i].Length < prefix.Length + "LICENSE: ".Length)
                                {
                                    singleMatch = false;
                                }
                                else
                                {
                                    singleMatch = lines[i].Substring(prefix.Length + "LICENSE: ".Length).Trim() == licenseText[k].Trim();
                                }
                            }
                            if (!singleMatch && k == 0)
                            {
                                if (lines[i].Length < prefix.Length + " LICENSE: ".Length)
                                {
                                    singleMatch = false;
                                }
                                else
                                {
                                    singleMatch = lines[i].Substring(prefix.Length + " LICENSE: ".Length).Trim() == licenseText[k].Trim();
                                }
                            }
                            if (!singleMatch)
                            {
                                thisTryOK = false;
                                if (!wroteOnce)
                                {
                                    WriteLine();
                                    WriteLine(fileName);
                                    wroteOnce = true;
                                }
                                WriteLine("Line: " + (i + k));
                                WriteLine("'" + lines[i + k] + "'");
                                WriteLine("'" + prefix + " " + licenseText[k] + "'");
                            }
                        }
                    }

                    if (thisTryOK)
                    {
                        gotMatch = true;
                        break;
                    }
                }
            }

            return gotMatch;
        }

        private static void Write(string p)
        {
            Console.Write(p);
            Debug.Write(p);
        }
        private static void ReadLine()
        {
            Console.ReadLine();
        }
        private static void WriteLine(string p = "")
        {
            Console.WriteLine(p);
            Debug.WriteLine(p);
        }

    }
}