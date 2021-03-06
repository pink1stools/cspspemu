﻿using CSPspEmu.Core.Cpu.Assembler;
using SafeILGenerator.Ast.Nodes;
using System.Collections.Generic;

namespace CSPspEmu.Core.Cpu.Dynarec.Ast
{
    public class AstNodeStmPspInstruction : AstNodeStm
    {
        public MipsDisassembler.Result DisassembledResult;
        public AstNodeStm Statement;

        public AstNodeStmPspInstruction(MipsDisassembler.Result disassembledResult, AstNodeStm statement)
        {
            DisassembledResult = disassembledResult;
            Statement = statement;
        }

        public override void TransformNodes(TransformNodesDelegate transformer) => transformer.Ref(ref Statement);

        public override Dictionary<string, string> Info => new Dictionary<string, string>
        {
            {"Address", $"0x{DisassembledResult.InstructionPc:X8}"},
            {"Instruction", DisassembledResult.ToString()},
        };
    }
}