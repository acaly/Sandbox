using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ShaderCompiler
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.Out.WriteLine("------ Shader compiler: start ------");
            var targetAssembly = AssemblyDefinition.ReadAssembly("Sandbox.exe");
            foreach (var type in targetAssembly.Modules.First().Types)
            {
                if (type.Name.Contains("Shader"))
                {
                    Console.Out.WriteLine("Find a shader class: " + type.Name);
                }
            }
            Console.Out.WriteLine("------ Shader compiler: finished ------");
        }
    }
}
