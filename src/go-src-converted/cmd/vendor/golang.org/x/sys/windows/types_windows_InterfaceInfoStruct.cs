//---------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool. Changes to this
//     file may cause incorrect behavior and will be lost
//     if the code is regenerated.
//
//     Generated on 2020 October 09 06:00:58 UTC
// </auto-generated>
//---------------------------------------------------------
using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using static go.builtin;
using net = go.net_package;
using syscall = go.syscall_package;
using @unsafe = go.@unsafe_package;
using go;

#nullable enable

namespace go {
namespace cmd {
namespace vendor {
namespace golang.org {
namespace x {
namespace sys
{
    public static partial class windows_package
    {
        [GeneratedCode("go2cs", "0.1.0.0")]
        public partial struct InterfaceInfo
        {
            // Constructors
            public InterfaceInfo(NilType _)
            {
                this.Flags = default;
                this.Address = default;
                this.BroadcastAddress = default;
                this.Netmask = default;
            }

            public InterfaceInfo(uint Flags = default, SockaddrGen Address = default, SockaddrGen BroadcastAddress = default, SockaddrGen Netmask = default)
            {
                this.Flags = Flags;
                this.Address = Address;
                this.BroadcastAddress = BroadcastAddress;
                this.Netmask = Netmask;
            }

            // Enable comparisons between nil and InterfaceInfo struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(InterfaceInfo value, NilType nil) => value.Equals(default(InterfaceInfo));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(InterfaceInfo value, NilType nil) => !(value == nil);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(NilType nil, InterfaceInfo value) => value == nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(NilType nil, InterfaceInfo value) => value != nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator InterfaceInfo(NilType nil) => default(InterfaceInfo);
        }

        [GeneratedCode("go2cs", "0.1.0.0")]
        public static InterfaceInfo InterfaceInfo_cast(dynamic value)
        {
            return new InterfaceInfo(value.Flags, value.Address, value.BroadcastAddress, value.Netmask);
        }
    }
}}}}}}