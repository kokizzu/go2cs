//---------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool. Changes to this
//     file may cause incorrect behavior and will be lost
//     if the code is regenerated.
//
//     Generated on 2020 August 29 10:05:40 UTC
// </auto-generated>
//---------------------------------------------------------
using System.CodeDom.Compiler;
using System.Runtime.CompilerServices;
using go;

namespace go {
namespace cmd {
namespace vendor {
namespace github.com {
namespace google {
namespace pprof {
namespace @internal
{
    public static partial class graph_package
    {
        [GeneratedCode("go2cs", "0.1.0.0")]
        public partial struct EdgeMap
        {
            // Value of the EdgeMap struct
            private readonly map<ref Node, ref Edge> m_value;

            public EdgeMap(map<ref Node, ref Edge> value) => m_value = value;

            // Enable implicit conversions between map<ref Node, ref Edge> and EdgeMap struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator EdgeMap(map<ref Node, ref Edge> value) => new EdgeMap(value);
            
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator map<ref Node, ref Edge>(EdgeMap value) => value.m_value;
            
            // Enable comparisons between nil and EdgeMap struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(EdgeMap value, NilType nil) => value.Equals(default(EdgeMap));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(EdgeMap value, NilType nil) => !(value == nil);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(NilType nil, EdgeMap value) => value == nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(NilType nil, EdgeMap value) => value != nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator EdgeMap(NilType nil) => default(EdgeMap);
        }
    }
}}}}}}}