﻿using Mono.Cecil;
using Mono.Cecil.Cil;
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
        private static string CompileShader(TypeDefinition type)
        {
            return "Hello World!";
        }

        static void Main(string[] args)
        {
            Console.Out.WriteLine("------ Shader compiler: start ------");
            var target = ModuleDefinition.ReadModule("Sandbox.exe");
            var attrClass = target.Types.Single(t => t.Name == "ShaderClassAttribute");
            var voidtype = target.Import(typeof(void));
            foreach (var type in target.Types)
            {
                var attr = type.CustomAttributes.SingleOrDefault(ca =>
                    ca.AttributeType.FullName == attrClass.FullName);
                if (attr == null) continue;
                
                Console.Out.WriteLine("Find a shader class: " + type.Name);

                var output = CompileShader(type);

                var cctor = type.Methods.SingleOrDefault(m => m.IsConstructor && m.IsStatic);
                if (cctor == null)
                {
                    Console.Out.WriteLine("Create a new static constructor for field init.");
                    cctor = new MethodDefinition(".cctor",
                        Mono.Cecil.MethodAttributes.Private |
                        Mono.Cecil.MethodAttributes.HideBySig |
                        Mono.Cecil.MethodAttributes.SpecialName |
                        Mono.Cecil.MethodAttributes.RTSpecialName |
                        Mono.Cecil.MethodAttributes.Static,
                        voidtype);
                    type.Methods.Add(cctor);
                    //add a ret at the end
                    cctor.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
                }
                var appendPos = cctor.Body.Instructions.TakeWhile(inst => inst.OpCode != OpCodes.Ret).Count();
                var retVariable = type.Fields.SingleOrDefault(f => f.Name == "ShaderCode");
                //TODO check variable existance

                cctor.Body.Instructions.Insert(appendPos++, Instruction.Create(OpCodes.Ldstr, output));
                cctor.Body.Instructions.Insert(appendPos++, Instruction.Create(OpCodes.Stsfld, retVariable));
            }
            target.Write("Sandbox.exe");
            Console.Out.WriteLine("------ Shader compiler: finished ------");
        }
    }
}
