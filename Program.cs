using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace NugetUpdate
{
    public class Program
    {
        //-dir 参数为dll文件夹名称
        // -outDir 参数为输出文件夹名称
        // -source 参数为nuget服务器地址
        // -name 参数为包名称
        //-help,-?,-h 参数为帮助
        //-NeedDependency 是否需要下载依赖性 默认值为false 
        public static void Main(string[] args)
        {//判断是否需要打印帮助信息
            if (args.Length == 1 && (args[0] == "-help" || args[0] == "-?" || args[0] == "-h"))
            {
                Console.WriteLine("参数说明:");
                Console.WriteLine("-dir 参数为dll文件夹名称");
                Console.WriteLine("-outDir 参数为输出文件夹名称");
                Console.WriteLine("-source 参数为nuget服务器地址");
                Console.WriteLine("-name 参数为包名称");
                Console.WriteLine("-help,-?,-h 参数为帮助");
                Console.WriteLine("-NeedDependency 是否需要下载依赖性 默认值为false ");
                Console.WriteLine("示例:");
                Console.WriteLine("NugetUpdate.exe -dir .\\bin\\Debug\\tt -outDir .\\bin\\Debug\\o");
                Console.WriteLine("NugetUpdate.exe -name Newtonsoft.Json");
                Console.WriteLine("NugetUpdate.exe -name Newtonsoft.Json -source http://192.168.21.45:8080/v3/index.json");
                Console.WriteLine("NugetUpdate.exe -dir .\\bin\\Debug\\tt -outDir .\\bin\\Debug\\o -source http://192.168.21.45:8080/v3/index.json -needDependency false");

                return;
            }

            //解析传入的参数
            Console.WriteLine("开始更新Nuget包");
            string dir = ".";
            string outDir = "./v";
            string source = "http://192.168.21.45:8080/v3/index.json";
            string name = "";
            bool isNeedDependency = false;
            List<string> dllNames = new List<string>();
            Guid guid = Guid.NewGuid();
            string tempOutDir = Path.Combine(Path.GetTempPath(), "updateNuget", guid.ToString());
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i].ToLower() == "-dir")
                {
                    dir = args[i + 1];
                }
                if (args[i].ToLower() == "-outdir")
                {
                    outDir = args[i + 1];
                }
                if (args[i].ToLower() == "-source")
                {
                    source = args[i + 1];
                }
                if (args[i].ToLower() == "-name")
                {
                    name = args[i + 1];
                }
                if (args[i].ToLower() == "-needdependency")
                {
                    isNeedDependency = bool.Parse(args[i + 1]);
                }

            }
            //没有输入包名称
            if (string.IsNullOrEmpty(name))
            {
                //要下载的文件夹为空
                if (string.IsNullOrEmpty(dir))
                {
                    Console.WriteLine("请输入要下载的包名称");
                    name = Console.ReadLine();
                    DownloadPackage(name, outDir, source);
                    return;
                }
                else
                {
                    //如果dir第一个字符".",将"."替换为当前目录
                    if (dir.StartsWith("."))
                    {
                        //将相对地址dir转换为绝对地址

                        dir = Path.GetFullPath(dir);
                        //替换第一个字符为当前运行目录
                        //dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, dir.Substring(1));
                    }

                    //获取文件夹下所有dll文件
                    if (!DownloadDirsFile(dir, source, tempOutDir, dllNames))
                    {
                        Console.WriteLine("下载失败,按Enter继续");
                        Console.ReadLine();
                    }
                }
            }
            else
            {
                //下载包
                DownloadPackage(name, tempOutDir, source);
                dllNames.Add(name);
            }
            outDir = CopyFile(outDir, isNeedDependency, dllNames, tempOutDir);
            Console.WriteLine("更新完成");
            //Console.ReadLine();
        }

        private static string CopyFile(string outDir, bool isNeedDependency, List<string> dllNames, string tempOutDir)
        {
            //将下载的包移动到指定文件夹
            string[] tempFiles1 = System.IO.Directory.GetFiles(tempOutDir, "*.dll", SearchOption.AllDirectories);
            string[] tempFiles2 = System.IO.Directory.GetFiles(tempOutDir, "*.xml", SearchOption.AllDirectories);
            string[] tempFiles = tempFiles1.Union(tempFiles2).ToArray();
            List<string> tempFileList = new List<string>();
            //如果needDependency为false,则不复制依赖项,否则复制所有依赖项
            if (!isNeedDependency)
            {
                foreach (var item in tempFiles)
                {
                    foreach (var dllName in dllNames)
                    {
                        if (item.Contains(dllName))
                        {
                            tempFileList.Add(item);
                        }
                    }
                }
            }
            else
            {
                tempFileList= tempFiles.ToList();
            }

            //如果输出文件夹包含".",将"."替换为当前目录
            if (outDir.StartsWith("."))
            {
                outDir = Path.GetFullPath(outDir);
            }
            //如果outDir不存在,就创建
            if (!System.IO.Directory.Exists(outDir))
            {
                System.IO.Directory.CreateDirectory(outDir);
            }
            //查找所有的dll以及xml
            foreach (var file in tempFileList)
            {
                FileInfo fileInfo = new FileInfo(file);
                //移动文件
                string fileName = fileInfo.Name;
                string destFile = System.IO.Path.Combine(outDir, fileName);
                System.IO.File.Copy(file, destFile, true);
                Console.WriteLine("移动文件:" + fileName + "到目标文件夹:" + destFile);

            }

            return outDir;
        }

        private static bool DownloadDirsFile(string dir, string source, string tempOutDir,List<string> dllNames)
        {
            List<Task<bool>> tasks = new List<Task<bool>>();
            string[] files = System.IO.Directory.GetFiles(dir, "*.dll");

            foreach (var file in files)
            {
              
                //获取文件名称
                string fileName = System.IO.Path.GetFileName(file);
                //获取包名称
                FileInfo fileInfo = new FileInfo(file);
                string packetName = fileInfo.Name.Replace(fileInfo.Extension, "");  dllNames.Add(packetName);
                //下载包

                Task<bool> t = Task.Run(() => DownloadPackage(packetName, tempOutDir, source));
                tasks.Add(t);
            }
            Task.WaitAll(tasks.ToArray());
            return tasks.ToArray().All(t => t.Result);
        }

        private static bool DownloadPackage(string packetName, string outDir = ".", string nugetServerUrl = "http://192.168.21.45:8080/v3/index.json")
        {

            string command = "nuget install " + packetName + " -Source " + nugetServerUrl + " -OutputDirectory " + outDir;
            //执行控制台指令
            string res = ExecuteCommand(command);
            //根据返回值确定是否下载成功
            if (res.Contains("Successfully installed"))
            {
                Console.WriteLine(packetName + "下载成功");
                return true;
            }
            else
            {
                Console.WriteLine(packetName + "下载失败");
                return false;
            }
        }
        private static string ExecuteCommand(string command)
        {
            Process process = new Process();
            process.StartInfo.FileName = "cmd.exe";
            process.StartInfo.Arguments = "/c" + command;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            process.Close();
            //如果有错误信息，输出错误信息
            if (output.Contains("Successfully installed"))
            {
                Console.WriteLine("下载成功");
            }
            else
            {//使用红色显示错误信息
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("下载失败");
                Console.WriteLine(output);
                Console.ResetColor();
            }
            return output;
        }
    }
}
