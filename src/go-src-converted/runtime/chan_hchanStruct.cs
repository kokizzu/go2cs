//---------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool. Changes to this
//     file may cause incorrect behavior and will be lost
//     if the code is regenerated.
//
//     Generated on 2020 August 29 08:16:38 UTC
// </auto-generated>
//---------------------------------------------------------
using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using static go.builtin;
using atomic = go.runtime.@internal.atomic_package;
using @unsafe = go.@unsafe_package;

namespace go
{
    public static partial class runtime_package
    {
        [GeneratedCode("go2cs", "0.1.0.0")]
        private partial struct hchan
        {
            // Constructors
            public hchan(NilType _)
            {
                this.qcount = default;
                this.dataqsiz = default;
                this.buf = default;
                this.elemsize = default;
                this.closed = default;
                this.elemtype = default;
                this.sendx = default;
                this.recvx = default;
                this.recvq = default;
                this.sendq = default;
                this.@lock = default;
            }

            public hchan(ulong qcount = default, ulong dataqsiz = default, unsafe.Pointer buf = default, ushort elemsize = default, uint closed = default, ref ptr<_type> elemtype = default, ulong sendx = default, ulong recvx = default, waitq recvq = default, waitq sendq = default, mutex @lock = default)
            {
                this.qcount = qcount;
                this.dataqsiz = dataqsiz;
                this.buf = buf;
                this.elemsize = elemsize;
                this.closed = closed;
                this.elemtype = elemtype;
                this.sendx = sendx;
                this.recvx = recvx;
                this.recvq = recvq;
                this.sendq = sendq;
                this.@lock = @lock;
            }

            // Enable comparisons between nil and hchan struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(hchan value, NilType nil) => value.Equals(default(hchan));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(hchan value, NilType nil) => !(value == nil);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(NilType nil, hchan value) => value == nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(NilType nil, hchan value) => value != nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator hchan(NilType nil) => default(hchan);
        }

        [GeneratedCode("go2cs", "0.1.0.0")]
        private static hchan hchan_cast(dynamic value)
        {
            return new hchan(value.qcount, value.dataqsiz, value.buf, value.elemsize, value.closed, ref value.elemtype, value.sendx, value.recvx, value.recvq, value.sendq, value.@lock);
        }
    }
}