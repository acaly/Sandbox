using Mono.Cecil;
using Mono.Cecil.Cil;
using ShaderCompiler.AST;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShaderCompiler.Decompiler
{
    class DecompilerEnvironment
    {
        public MethodDefinition Input { get; set; }
        public ASTList Output { get; set; }

        private Stack<object> cilStack = new Stack<object>();

        public void Run()
        {
            foreach (var inst in Input.Body.Instructions)
            {
                ParseInst(inst);
            }
        }

        private void ParseInst(Instruction inst)
        {
            if (inst.OpCode == OpCodes.Nop)
            {
            }
            else
            {
                throw new Exception("Unknown OpCode.");
            }
        }
    }
}
