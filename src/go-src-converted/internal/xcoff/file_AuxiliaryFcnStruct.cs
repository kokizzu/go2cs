//---------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool. Changes to this
//     file may cause incorrect behavior and will be lost
//     if the code is regenerated.
//
//     Generated on 2022 March 13 05:52:08 UTC
// </auto-generated>
//---------------------------------------------------------
using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using dwarf = go.debug.dwarf_package;
using binary = go.encoding.binary_package;
using fmt = go.fmt_package;
using io = go.io_package;
using os = go.os_package;
using strings = go.strings_package;
using go;

#nullable enable

namespace go {
namespace @internal
{
    public static partial class xcoff_package
    {
        [GeneratedCode("go2cs", "0.1.2.0")]
        public partial struct AuxiliaryFcn
        {
            // Constructors
            public AuxiliaryFcn(NilType _)
            {
                this.Size = default;
            }

            public AuxiliaryFcn(long Size = default)
            {
                this.Size = Size;
            }

            // Enable comparisons between nil and AuxiliaryFcn struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(AuxiliaryFcn value, NilType nil) => value.Equals(default(AuxiliaryFcn));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(AuxiliaryFcn value, NilType nil) => !(value == nil);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(NilType nil, AuxiliaryFcn value) => value == nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(NilType nil, AuxiliaryFcn value) => value != nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator AuxiliaryFcn(NilType nil) => default(AuxiliaryFcn);
        }

        [GeneratedCode("go2cs", "0.1.2.0")]
        public static AuxiliaryFcn AuxiliaryFcn_cast(dynamic value)
        {
            return new AuxiliaryFcn(value.Size);
        }
    }
}}