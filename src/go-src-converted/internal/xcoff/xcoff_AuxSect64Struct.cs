//---------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool. Changes to this
//     file may cause incorrect behavior and will be lost
//     if the code is regenerated.
//
//     Generated on 2022 March 13 05:52:09 UTC
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
namespace @internal
{
    public static partial class xcoff_package
    {
        [GeneratedCode("go2cs", "0.1.2.0")]
        public partial struct AuxSect64
        {
            // Constructors
            public AuxSect64(NilType _)
            {
                this.Xscnlen = default;
                this.Xnreloc = default;
                this.pad = default;
                this.Xauxtype = default;
            }

            public AuxSect64(ulong Xscnlen = default, ulong Xnreloc = default, byte pad = default, byte Xauxtype = default)
            {
                this.Xscnlen = Xscnlen;
                this.Xnreloc = Xnreloc;
                this.pad = pad;
                this.Xauxtype = Xauxtype;
            }

            // Enable comparisons between nil and AuxSect64 struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(AuxSect64 value, NilType nil) => value.Equals(default(AuxSect64));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(AuxSect64 value, NilType nil) => !(value == nil);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(NilType nil, AuxSect64 value) => value == nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(NilType nil, AuxSect64 value) => value != nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator AuxSect64(NilType nil) => default(AuxSect64);
        }

        [GeneratedCode("go2cs", "0.1.2.0")]
        public static AuxSect64 AuxSect64_cast(dynamic value)
        {
            return new AuxSect64(value.Xscnlen, value.Xnreloc, value.pad, value.Xauxtype);
        }
    }
}}