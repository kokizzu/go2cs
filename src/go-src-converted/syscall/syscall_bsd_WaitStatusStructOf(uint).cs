//---------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool. Changes to this
//     file may cause incorrect behavior and will be lost
//     if the code is regenerated.
//
//     Generated on 2022 March 13 05:40:36 UTC
// </auto-generated>
//---------------------------------------------------------
using System;
using System.CodeDom.Compiler;
using System.Collections;
using System.Runtime.CompilerServices;

#nullable enable

namespace go
{
    public static partial class syscall_package
    {
        [GeneratedCode("go2cs", "0.1.2.0")]
        public partial struct WaitStatus
        {
            // Value of the WaitStatus struct
            private readonly uint m_value;
            
            public WaitStatus(uint value) => m_value = value;

            // Enable implicit conversions between uint and WaitStatus struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator WaitStatus(uint value) => new WaitStatus(value);
            
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator uint(WaitStatus value) => value.m_value;
            
            // Enable comparisons between nil and WaitStatus struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(WaitStatus value, NilType nil) => value.Equals(default(WaitStatus));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(WaitStatus value, NilType nil) => !(value == nil);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(NilType nil, WaitStatus value) => value == nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(NilType nil, WaitStatus value) => value != nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator WaitStatus(NilType nil) => default(WaitStatus);
        }
    }
}
