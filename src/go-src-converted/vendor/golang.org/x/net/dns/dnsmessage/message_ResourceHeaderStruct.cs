//---------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool. Changes to this
//     file may cause incorrect behavior and will be lost
//     if the code is regenerated.
//
//     Generated on 2022 March 13 06:45:25 UTC
// </auto-generated>
//---------------------------------------------------------
using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using errors = go.errors_package;
using go;

#nullable enable

namespace go {
namespace vendor {
namespace golang.org {
namespace x {
namespace net {
namespace dns
{
    public static partial class dnsmessage_package
    {
        [GeneratedCode("go2cs", "0.1.2.0")]
        public partial struct ResourceHeader
        {
            // Constructors
            public ResourceHeader(NilType _)
            {
                this.Name = default;
                this.Type = default;
                this.Class = default;
                this.TTL = default;
                this.Length = default;
            }

            public ResourceHeader(Name Name = default, Type Type = default, Class Class = default, uint TTL = default, ushort Length = default)
            {
                this.Name = Name;
                this.Type = Type;
                this.Class = Class;
                this.TTL = TTL;
                this.Length = Length;
            }

            // Enable comparisons between nil and ResourceHeader struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(ResourceHeader value, NilType nil) => value.Equals(default(ResourceHeader));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(ResourceHeader value, NilType nil) => !(value == nil);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(NilType nil, ResourceHeader value) => value == nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(NilType nil, ResourceHeader value) => value != nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator ResourceHeader(NilType nil) => default(ResourceHeader);
        }

        [GeneratedCode("go2cs", "0.1.2.0")]
        public static ResourceHeader ResourceHeader_cast(dynamic value)
        {
            return new ResourceHeader(value.Name, value.Type, value.Class, value.TTL, value.Length);
        }
    }
}}}}}}