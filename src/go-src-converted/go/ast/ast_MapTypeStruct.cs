//---------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool. Changes to this
//     file may cause incorrect behavior and will be lost
//     if the code is regenerated.
//
//     Generated on 2020 August 29 08:46:42 UTC
// </auto-generated>
//---------------------------------------------------------
using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using static go.builtin;
using token = go.go.token_package;
using strings = go.strings_package;
using unicode = go.unicode_package;
using utf8 = go.unicode.utf8_package;
using go;

namespace go {
namespace go
{
    public static partial class ast_package
    {
        [GeneratedCode("go2cs", "0.1.0.0")]
        public partial struct MapType
        {
            // Constructors
            public MapType(NilType _)
            {
                this.Map = default;
                this.Key = default;
                this.Value = default;
            }

            public MapType(token.Pos Map = default, Expr Key = default, Expr Value = default)
            {
                this.Map = Map;
                this.Key = Key;
                this.Value = Value;
            }

            // Enable comparisons between nil and MapType struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(MapType value, NilType nil) => value.Equals(default(MapType));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(MapType value, NilType nil) => !(value == nil);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(NilType nil, MapType value) => value == nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(NilType nil, MapType value) => value != nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator MapType(NilType nil) => default(MapType);
        }

        [GeneratedCode("go2cs", "0.1.0.0")]
        public static MapType MapType_cast(dynamic value)
        {
            return new MapType(value.Map, value.Key, value.Value);
        }
    }
}}