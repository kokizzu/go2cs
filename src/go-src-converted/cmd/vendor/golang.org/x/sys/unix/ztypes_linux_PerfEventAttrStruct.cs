//---------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool. Changes to this
//     file may cause incorrect behavior and will be lost
//     if the code is regenerated.
//
//     Generated on 2020 October 09 06:00:34 UTC
// </auto-generated>
//---------------------------------------------------------
using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;

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
        public partial struct PerfEventAttr
        {
            // Constructors
            public PerfEventAttr(NilType _)
            {
                this.Type = default;
                this.Size = default;
                this.Config = default;
                this.Sample = default;
                this.Sample_type = default;
                this.Read_format = default;
                this.Bits = default;
                this.Wakeup = default;
                this.Bp_type = default;
                this.Ext1 = default;
                this.Ext2 = default;
                this.Branch_sample_type = default;
                this.Sample_regs_user = default;
                this.Sample_stack_user = default;
                this.Clockid = default;
                this.Sample_regs_intr = default;
                this.Aux_watermark = default;
                this.Sample_max_stack = default;
                this._ = default;
            }

            public PerfEventAttr(uint Type = default, uint Size = default, ulong Config = default, ulong Sample = default, ulong Sample_type = default, ulong Read_format = default, ulong Bits = default, uint Wakeup = default, uint Bp_type = default, ulong Ext1 = default, ulong Ext2 = default, ulong Branch_sample_type = default, ulong Sample_regs_user = default, uint Sample_stack_user = default, int Clockid = default, ulong Sample_regs_intr = default, uint Aux_watermark = default, ushort Sample_max_stack = default, ushort _ = default)
            {
                this.Type = Type;
                this.Size = Size;
                this.Config = Config;
                this.Sample = Sample;
                this.Sample_type = Sample_type;
                this.Read_format = Read_format;
                this.Bits = Bits;
                this.Wakeup = Wakeup;
                this.Bp_type = Bp_type;
                this.Ext1 = Ext1;
                this.Ext2 = Ext2;
                this.Branch_sample_type = Branch_sample_type;
                this.Sample_regs_user = Sample_regs_user;
                this.Sample_stack_user = Sample_stack_user;
                this.Clockid = Clockid;
                this.Sample_regs_intr = Sample_regs_intr;
                this.Aux_watermark = Aux_watermark;
                this.Sample_max_stack = Sample_max_stack;
                this._ = _;
            }

            // Enable comparisons between nil and PerfEventAttr struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(PerfEventAttr value, NilType nil) => value.Equals(default(PerfEventAttr));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(PerfEventAttr value, NilType nil) => !(value == nil);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(NilType nil, PerfEventAttr value) => value == nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(NilType nil, PerfEventAttr value) => value != nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator PerfEventAttr(NilType nil) => default(PerfEventAttr);
        }

        [GeneratedCode("go2cs", "0.1.0.0")]
        public static PerfEventAttr PerfEventAttr_cast(dynamic value)
        {
            return new PerfEventAttr(value.Type, value.Size, value.Config, value.Sample, value.Sample_type, value.Read_format, value.Bits, value.Wakeup, value.Bp_type, value.Ext1, value.Ext2, value.Branch_sample_type, value.Sample_regs_user, value.Sample_stack_user, value.Clockid, value.Sample_regs_intr, value.Aux_watermark, value.Sample_max_stack, value._);
        }
    }
}}}}}}