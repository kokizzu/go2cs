//---------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool. Changes to this
//     file may cause incorrect behavior and will be lost
//     if the code is regenerated.
//
//     Generated on 2022 March 13 05:26:46 UTC
// </auto-generated>
//---------------------------------------------------------
using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using atomic = go.runtime.@internal.atomic_package;
using sys = go.runtime.@internal.sys_package;
using @unsafe = go.@unsafe_package;

#nullable enable

namespace go
{
    public static partial class runtime_package
    {
        [GeneratedCode("go2cs", "0.1.2.0")]
        private partial struct _panic
        {
            // Constructors
            public _panic(NilType _)
            {
                this.argp = default;
                this.link = default;
                this.pc = default;
                this.sp = default;
                this.recovered = default;
                this.aborted = default;
                this.goexit = default;
            }

            public _panic(unsafe.Pointer argp = default, ref ptr<_panic> link = default, System.UIntPtr pc = default, unsafe.Pointer sp = default, bool recovered = default, bool aborted = default, bool goexit = default)
            {
                this.argp = argp;
                this.link = link;
                this.pc = pc;
                this.sp = sp;
                this.recovered = recovered;
                this.aborted = aborted;
                this.goexit = goexit;
            }

            // Enable comparisons between nil and _panic struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(_panic value, NilType nil) => value.Equals(default(_panic));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(_panic value, NilType nil) => !(value == nil);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(NilType nil, _panic value) => value == nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(NilType nil, _panic value) => value != nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator _panic(NilType nil) => default(_panic);
        }

        [GeneratedCode("go2cs", "0.1.2.0")]
        private static _panic _panic_cast(dynamic value)
        {
            return new _panic(value.argp, ref value.link, value.pc, value.sp, value.recovered, value.aborted, value.goexit);
        }
    }
}