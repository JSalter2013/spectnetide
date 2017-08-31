﻿using AntlrZ80Asm.Assembler;
using Shouldly;

namespace AntlrZ80Asm.Test
{
    public class AssemblerTestBed : ParserTestBed
    {
        protected void CodeEmitWorks(string source, params byte[] opCodes)
        {
            // --- Arrange
            var compiler = new Z80Assembler();

            // --- Act
            var output = compiler.Compile(source);

            // --- Assert
            output.ErrorCount.ShouldBe(0);
            output.Segments.Count.ShouldBe(1);
            var bytes = output.Segments[0].EmittedCode;
            bytes.Count.ShouldBe(opCodes.Length);
            for (var i = 0; i < opCodes.Length; i++)
            {
                bytes[i].ShouldBe(opCodes[i]);
            }
        }

        protected void CodeEmitWorks(string source, FixupType type, int offset, params byte[] opCodes)
        {
            // --- Arrange
            var compiler = new Z80Assembler();

            // --- Act
            var output = compiler.Compile(source);

            // --- Assert
            output.ErrorCount.ShouldBe(0);
            output.Segments.Count.ShouldBe(1);
            var bytes = output.Segments[0].EmittedCode;
            bytes.Count.ShouldBe(opCodes.Length);
            for (var i = 0; i < opCodes.Length; i++)
            {
                bytes[i].ShouldBe(opCodes[i]);
            }
            compiler.Fixups.Count.ShouldBe(1);
            var fixup = compiler.Fixups[0];
            fixup.Type.ShouldBe(type);
            fixup.SegmentIndex.ShouldBe(0);
            fixup.Offset.ShouldBe(offset);
            fixup.Expression.ShouldNotBeNull();
        }

    }
}