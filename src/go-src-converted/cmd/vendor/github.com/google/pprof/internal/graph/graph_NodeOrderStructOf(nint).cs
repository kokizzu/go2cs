//---------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool. Changes to this
//     file may cause incorrect behavior and will be lost
//     if the code is regenerated.
//
//     Generated on 2022 March 13 06:36:44 UTC
// </auto-generated>
//---------------------------------------------------------
using System;
using System.CodeDom.Compiler;
using System.Collections;
using System.Runtime.CompilerServices;
using go;

#nullable enable

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
        [GeneratedCode("go2cs", "0.1.2.0")]
        public partial struct NodeOrder
        {
            // Value of the NodeOrder struct
            private readonly nint m_value;
            
            public NodeOrder(nint value) => m_value = value;

            // Enable implicit conversions between nint and NodeOrder struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator NodeOrder(nint value) => new NodeOrder(value);
            
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator nint(NodeOrder value) => value.m_value;
            
            // Enable comparisons between nil and NodeOrder struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(NodeOrder value, NilType nil) => value.Equals(default(NodeOrder));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(NodeOrder value, NilType nil) => !(value == nil);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(NilType nil, NodeOrder value) => value == nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(NilType nil, NodeOrder value) => value != nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator NodeOrder(NilType nil) => default(NodeOrder);
        }
    }
}}}}}}}
