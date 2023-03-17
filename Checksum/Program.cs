using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Collections;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Web.Script.Serialization;

namespace Checksum
{
    internal class Program
    {
        static void Main()
        {
            List<Archive> info = new List<Archive>();

            string dir;

            Console.WriteLine(" _____ _               _                        \r\n/  __ \\ |             | |                       \r\n| /  \\/ |__   ___  ___| | _____ _   _ _ __ ___  \r\n| |   | '_ \\ / _ \\/ __| |/ / __| | | | '_ ` _ \\ \r\n| \\__/\\ | | |  __/ (__|   <\\__ \\ |_| | | | | | |\r\n \\____/_| |_|\\___|\\___|_|\\_\\___/\\__,_|_| |_| |_|  v" + Assembly.GetExecutingAssembly().GetName().Version);

            while (true)
            {
                Console.Write("\nВведите путь до директории с архивами: ");
                dir = Console.ReadLine();

                foreach (var archive in Directory.GetFiles(dir))
                {
                    var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, archive);
                    Console.WriteLine(path);
                    try
                    {
                        var arch = ScanArchive7z(path);
                        info.Add(arch);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Ошибка в файле " + Path.GetFileName(path) + ":" + e.Message);
                    }
                }

                var json = new JavaScriptSerializer().Serialize(info);
                File.WriteAllText(new DirectoryInfo(dir).Name + ".txt", json);

                Console.WriteLine("Завершено, просканировано архивов: " + Directory.GetFiles(dir).Length);
            }
        }

        [Serializable]
        public class Archive
        {
            public Archive(string name)
            {
                this.archive = name;
                this.isZip = false;
                this.bins = new List<BinFile>();
                this.txts = new List<string>();
                this.other = new List<string>();
            }

            public string archive { get; set; }
            public bool isZip { get; set; }
            public List<BinFile> bins { get; set; }
            public List<string> txts { get; set; }
            public List<string> other { get; set; }
        }

        [Serializable]
        public class BinFile
        {
            public BinFile(string name, string checksum, string size, string packed, string date)
            {
                this.name = name;
                this.checksum = checksum;
                this.size = size;
                this.packed = packed;
                this.date = date;
            }

            public BinFile(string name)
            {
                this.name = name;
            }

            public string name { get; set; }
            public string checksum { get; set; }
            public string size { get; set; }
            public string packed { get; set; }
            public string date { get; set; }
        }

        static public Archive ScanArchive7z(string archive)
        {
            Process p = new Process();
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.FileName = "C:\\Program Files\\7-Zip\\7z.exe";
            p.StartInfo.Arguments = $"l -slt -ba {archive}";
            p.Start();
            string output = p.StandardOutput.ReadToEnd();
            output = output.Substring(0, output.Length - 2);
            p.WaitForExit();

            //Console.WriteLine(output + "#END");
            Archive a = new Archive(Path.GetFileName(archive));

            foreach (var file in output.Split(new string[] { "\n\r" }, StringSplitOptions.None))
            {
                //Console.WriteLine(file);
                var fileInfo = file.StartsWith("\n") ? file.Substring(1).Split('\n') : file.Split('\n');

                var path = fileInfo[0].Replace("Path = ", "").Replace("\r", "");
                var size = fileInfo[2].Replace("Size = ", "").Replace("\r", "");
                var packed = fileInfo[3].Replace("Packed Size = ", "").Replace("\r", "");
                var date = fileInfo[4].Replace("Modified = ", "").Replace("\r", "").Split('.')[0];
                a.isZip = fileInfo[10].StartsWith("CRC");
                var checksum = a.isZip ? fileInfo[10].Replace("CRC = ", "").Replace("\r", "") : fileInfo[13].Replace("CRC = ", "").Replace("\r", "");

                if (size == "0" || checksum == "00000000")
                    continue;

                if (new string[] { ".bin", ".c1", ".hex", "" }.Contains(Path.GetExtension(path).ToLower()))
                {
                    a.bins.Add(new BinFile(path, checksum, size, packed, date));
                }
                else if (new string[] { ".pdf", ".docx", ".txt", ".doc", ".doc" }.Contains(Path.GetExtension(path).ToLower()))
                {
                    a.txts.Add(path);
                }
                else
                {
                    a.other.Add(path);
                }

            }

            return a;
        }
    }
}
