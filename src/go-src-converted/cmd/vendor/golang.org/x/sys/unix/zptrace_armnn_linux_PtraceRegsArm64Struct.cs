//---------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool. Changes to this
//     file may cause incorrect behavior and will be lost
//     if the code is regenerated.
//
//     Generated on 2020 October 09 05:57:35 UTC
// </auto-generated>
//---------------------------------------------------------
using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using static go.builtin;
using @unsafe = go.@unsafe_package;
using go;

#nullable enable

namespace go {
namespace cmd {
namespace vendor {
namespace golang.org {
namespace x {
namespace sys
{
    public static partial class unix_package
    {
        [GeneratedCode("go2cs", "0.1.0.0")]
        public partial struct PtraceRegsArm64
        {
            // Constructors
            public PtraceRegsArm64(NilType _)
            {
                this.Regs = default;
                this.Sp = default;
                this.Pc = default;
                this.Pstate = default;
            }

            public PtraceRegsArm64(array<ulong> Regs = default, ulong Sp = default, ulong Pc = default, ulong Pstate = default)
            {
                this.Regs = Regs;
                this.Sp = Sp;
                this.Pc = Pc;
                this.Pstate = Pstate;
            }

            // Enable comparisons between nil and PtraceRegsArm64 struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(PtraceRegsArm64 value, NilType nil) => value.Equals(default(PtraceRegsArm64));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(PtraceRegsArm64 value, NilType nil) => !(value == nil);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(NilType nil, PtraceRegsArm64 value) => value == nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(NilType nil, PtraceRegsArm64 value) => value != nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator PtraceRegsArm64(NilType nil) => default(PtraceRegsArm64);
        }

        [GeneratedCode("go2cs", "0.1.0.0")]
        public static PtraceRegsArm64 PtraceRegsArm64_cast(dynamic value)
        {
            return new PtraceRegsArm64(value.Regs, value.Sp, value.Pc, value.Pstate);
        }
    }
}}}}}}