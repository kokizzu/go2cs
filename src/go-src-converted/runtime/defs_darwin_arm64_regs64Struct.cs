//---------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool. Changes to this
//     file may cause incorrect behavior and will be lost
//     if the code is regenerated.
//
//     Generated on 2020 August 29 08:16:44 UTC
// </auto-generated>
//---------------------------------------------------------
using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using static go.builtin;
using @unsafe = go.@unsafe_package;

namespace go
{
    public static partial class runtime_package
    {
        [GeneratedCode("go2cs", "0.1.0.0")]
        private partial struct regs64
        {
            // Constructors
            public regs64(NilType _)
            {
                this.x = default;
                this.fp = default;
                this.lr = default;
                this.sp = default;
                this.pc = default;
                this.cpsr = default;
                this.__pad = default;
            }

            public regs64(array<ulong> x = default, ulong fp = default, ulong lr = default, ulong sp = default, ulong pc = default, uint cpsr = default, uint __pad = default)
            {
                this.x = x;
                this.fp = fp;
                this.lr = lr;
                this.sp = sp;
                this.pc = pc;
                this.cpsr = cpsr;
                this.__pad = __pad;
            }

            // Enable comparisons between nil and regs64 struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(regs64 value, NilType nil) => value.Equals(default(regs64));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(regs64 value, NilType nil) => !(value == nil);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(NilType nil, regs64 value) => value == nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(NilType nil, regs64 value) => value != nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator regs64(NilType nil) => default(regs64);
        }

        [GeneratedCode("go2cs", "0.1.0.0")]
        private static regs64 regs64_cast(dynamic value)
        {
            return new regs64(value.x, value.fp, value.lr, value.sp, value.pc, value.cpsr, value.__pad);
        }
    }
}