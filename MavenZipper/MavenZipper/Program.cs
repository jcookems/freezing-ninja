using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace MavenZipper
{
    class Program
    {
        private static string mavenFiles = @"C:\Users\jcooke\.m2\repository\";
        private static string rootPom = @"C:\dd\git\jcooke\azure-sdk-for-java-pr\microsoft-azure-api\pom.xml";
        private static string outputFolder = @"C:\users\jcooke\desktop\tmp\";
        private static string latestToken = "*LATEST*";
        static void Main(string[] args)
        {
            Directory.CreateDirectory(outputFolder);
            GetFromPom(rootPom, "");
        }

        static void GetFromPom(String pom, String offset)
        {
            Debug.WriteLine(offset + pom);
            var xml = XDocument.Load(pom);
            var root = xml.Root;
            var parent = root.Descendants(root.GetDefaultNamespace() + "parent").FirstOrDefault();
            string version = safeGetValue(parent, "version");

            var dependencies = root.Descendants(root.GetDefaultNamespace() + "dependencies");
            var deps = dependencies.Elements().Select(p => new Dep(p, version));
            //            foreach (var dep in deps.Where(dep => !(dep.scope == "test" || dep.scope == "provided" || dep.scope == "compile")))
            foreach (var dep in deps)
                // foreach (var dep in deps.Where(dep => !(dep.scope == "test")))
                {
                string dir = dep.ToString();
                if (dir.Contains(latestToken))
                {
                    String upOneDir = dep.ToString().Replace(@"\" + latestToken, "");
                    if (!Directory.Exists(upOneDir))
                    {
                        Debug.WriteLine(offset + "NO DIRECTORY: " + dir);
                        continue;
                    }
                    dir = Directory.GetDirectories(upOneDir).OrderBy(p => p).LastOrDefault();
                    if (dir == null)
                    {
                        Debug.WriteLine(offset + "DIR HAS NO SUBDIR: " + upOneDir);
                        continue;
                    }
                }

                if (!Directory.Exists(dir))
                {
                    Debug.WriteLine(offset + "NO DIRECTORY: " + dir);
                    continue;
                }

                foreach (var file in Directory.GetFiles(dir))
                {
                    if (file.EndsWith(".jar"))
                    {
                        FileInfo f = new FileInfo(file);
                        Debug.WriteLine(offset + f.Name);
                        f.CopyTo(outputFolder + f.Name, true);
                    }
                    if (file.EndsWith(".pom"))
                    {
                        GetFromPom(file, offset + "   ");
                    }
                }
            }
        }


        class Dep
        {
            public Dep(XElement dep, string defaultVersion)
            {
                string groupId = safeGetValue(dep, "groupId");
                string artifactId = safeGetValue(dep, "artifactId");
                string version = safeGetValue(dep, "version");
                version = (version == "${project.version}" ? defaultVersion : version);
                version = (version == null ? latestToken : version);
                this.scope = safeGetValue(dep, "scope");
                this.strval = Path.Combine(mavenFiles, groupId.Replace('.', '\\'), artifactId, version);
            }

            public string scope;
            private string strval;

            public override string ToString()
            {
                return strval;
            }
        }

        private static string safeGetValue(XElement dep, string name)
        {
            if (dep == null) return null;
            if (dep.Element(dep.GetDefaultNamespace() + name) == null) return null;
            return dep.Element(dep.GetDefaultNamespace() + name).Value;
        }
    }
}