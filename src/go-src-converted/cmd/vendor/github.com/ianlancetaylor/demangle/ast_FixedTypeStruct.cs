//---------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool. Changes to this
//     file may cause incorrect behavior and will be lost
//     if the code is regenerated.
//
//     Generated on 2022 March 13 06:37:26 UTC
// </auto-generated>
//---------------------------------------------------------
using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using fmt = go.fmt_package;
using strings = go.strings_package;
using go;

#nullable enable

namespace go {
namespace cmd {
namespace vendor {
namespace github.com {
namespace ianlancetaylor
{
    public static partial class demangle_package
    {
        [GeneratedCode("go2cs", "0.1.2.0")]
        public partial struct FixedType
        {
            // Constructors
            public FixedType(NilType _)
            {
                this.Base = default;
                this.Accum = default;
                this.Sat = default;
            }

            public FixedType(AST Base = default, bool Accum = default, bool Sat = default)
            {
                this.Base = Base;
                this.Accum = Accum;
                this.Sat = Sat;
            }

            // Enable comparisons between nil and FixedType struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(FixedType value, NilType nil) => value.Equals(default(FixedType));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(FixedType value, NilType nil) => !(value == nil);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(NilType nil, FixedType value) => value == nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(NilType nil, FixedType value) => value != nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator FixedType(NilType nil) => default(FixedType);
        }

        [GeneratedCode("go2cs", "0.1.2.0")]
        public static FixedType FixedType_cast(dynamic value)
        {
            return new FixedType(value.Base, value.Accum, value.Sat);
        }
    }
}}}}}