//---------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool. Changes to this
//     file may cause incorrect behavior and will be lost
//     if the code is regenerated.
//
//     Generated on 2022 March 13 05:36:17 UTC
// </auto-generated>
//---------------------------------------------------------
using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using sync = go.sync_package;
using go;

#nullable enable

namespace go {
namespace net
{
    public static partial class textproto_package
    {
        [GeneratedCode("go2cs", "0.1.2.0")]
        public partial struct Pipeline
        {
            // Constructors
            public Pipeline(NilType _)
            {
                this.mu = default;
                this.id = default;
                this.request = default;
                this.response = default;
            }

            public Pipeline(sync.Mutex mu = default, nuint id = default, sequencer request = default, sequencer response = default)
            {
                this.mu = mu;
                this.id = id;
                this.request = request;
                this.response = response;
            }

            // Enable comparisons between nil and Pipeline struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(Pipeline value, NilType nil) => value.Equals(default(Pipeline));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(Pipeline value, NilType nil) => !(value == nil);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(NilType nil, Pipeline value) => value == nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(NilType nil, Pipeline value) => value != nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator Pipeline(NilType nil) => default(Pipeline);
        }

        [GeneratedCode("go2cs", "0.1.2.0")]
        public static Pipeline Pipeline_cast(dynamic value)
        {
            return new Pipeline(value.mu, value.id, value.request, value.response);
        }
    }
}}