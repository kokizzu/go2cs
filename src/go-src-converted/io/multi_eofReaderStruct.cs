//---------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool. Changes to this
//     file may cause incorrect behavior and will be lost
//     if the code is regenerated.
//
//     Generated on 2022 March 13 05:27:45 UTC
// </auto-generated>
//---------------------------------------------------------
using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;


#nullable enable

namespace go
{
    public static partial class io_package
    {
        [GeneratedCode("go2cs", "0.1.2.0")]
        private partial struct eofReader
        {
            // Constructors
            public eofReader(NilType _)
            {
            }
            // Enable comparisons between nil and eofReader struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(eofReader value, NilType nil) => value.Equals(default(eofReader));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(eofReader value, NilType nil) => !(value == nil);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(NilType nil, eofReader value) => value == nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(NilType nil, eofReader value) => value != nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator eofReader(NilType nil) => default(eofReader);
        }

        [GeneratedCode("go2cs", "0.1.2.0")]
        private static eofReader eofReader_cast(dynamic value)
        {
            return new eofReader();
        }
    }
}