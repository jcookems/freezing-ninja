using System;
using System.IO;
using System.Linq;

namespace ConvertUnitToExtTest
{
    class Program
    {
        static string target = @"C:\dd\git\jcooke\azure-sdk-for-java-pr\microsoft-azure-api\src\test\java\com";
        static string dest = @"C:\Users\jcooke\workspace2\tests\src\ext";
        static bool recalibrate = false;

        static void Main(string[] args)
        {
            doitWorker(target, dest);
            Console.ReadLine();
        }

        private static string doitWorker(string targetPath, string destPath)
        {
            if (!Directory.Exists(destPath)) Directory.CreateDirectory(destPath);
            Directory.GetFiles(targetPath).Select(p => doit(p, destPath)).ToList();
            Directory.GetDirectories(targetPath)
                .Select(d => new DirectoryInfo(d).Name)
                .Select(dn => doitWorker(Path.Combine(targetPath, dn), Path.Combine(destPath, dn))).ToArray();
            return targetPath;
        }

        private static string doit(string fileName, string destPath)
        {
            if (destPath == dest + @"\microsoft\windowsazure\serviceruntime")
            {
                return fileName;
            }

            var destFileName = Path.Combine(destPath, new FileInfo(fileName).Name);
            var lines = File.ReadAllLines(fileName);
            string oldPackage = lines.FirstOrDefault(p => p.StartsWith("package"));
            string newPackage =
                oldPackage.Replace("package com.", "package ext.");
            if (!(oldPackage.Contains("com.microsoft.windowsazure.configuration.builder") ||
                oldPackage.Contains("com.microsoft.windowsazure.services.scenarios") ||
                oldPackage.Contains("com.microsoft.windowsazure.utils")))
            {
                newPackage += "\r\n" + oldPackage.Replace("package ", "import ").Replace(";", ".*;");
            }

            var newLines = String.Join("\r\n", lines.Select(p => (p == oldPackage ? newPackage : p))).Split(new string[] { "\r\n" }, StringSplitOptions.None);

            newLines = newLines.Select(p =>
                p == "import com.microsoft.windowsazure.services.media.IntegrationTestBase;" ?
                "import ext.microsoft.windowsazure.services.media.IntegrationTestBase;" : p).ToArray();
            newLines = newLines.Select(p =>
                p == "import com.microsoft.windowsazure.services.scenarios.MediaServiceWrapper.EncoderType;" ?
                "import ext.microsoft.windowsazure.services.scenarios.MediaServiceWrapper.EncoderType;" : p).ToArray();
            newLines = newLines.Select(p =>
                p == "import com.microsoft.windowsazure.services.table.IntegrationTestBase;" ?
                "import ext.microsoft.windowsazure.services.table.IntegrationTestBase;" : p).ToArray();
            newLines = newLines.Select(p =>
                (p.Contains("System.setProperty(\"http") ? "//" : "") + p).ToArray();


            if (!recalibrate)
            {
                File.WriteAllLines(destFileName, newLines);
            }
            else
            {
                bool diff = !File.Exists(destFileName);
                if (diff)
                {
                    Console.WriteLine("not exist: " + destFileName);
                }
                else
                {
                    var tmp = File.ReadAllLines(destFileName);
                    diff = (newLines.Length != tmp.Length);
                    string d = Diff.ShowDiff(newLines, tmp);
                    if (!String.IsNullOrEmpty(d))
                    {
                        Console.WriteLine(fileName);
                        Console.WriteLine(d);
                    }
                }
            }

            return fileName;
        }
    }
}
