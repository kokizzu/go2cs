﻿//---------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool. Changes to this
//     file may cause incorrect behavior and will be lost
//     if the code is regenerated.
// </auto-generated>
//---------------------------------------------------------

using System;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using go;

#nullable enable

namespace go.@internal;

public static partial class profilerecord_package
{
    [GeneratedCode("go2cs-gen", "0.1.4")]
    public partial struct MemProfileRecord
    {
        // Promoted Struct References
        // -- MemProfileRecord has no promoted structs

        // Field References
        internal static ref long ᏑAllocBytes(ref MemProfileRecord instance) => ref instance.AllocBytes;
        internal static ref long ᏑFreeBytes(ref MemProfileRecord instance) => ref instance.FreeBytes;
        internal static ref long ᏑAllocObjects(ref MemProfileRecord instance) => ref instance.AllocObjects;
        internal static ref long ᏑFreeObjects(ref MemProfileRecord instance) => ref instance.FreeObjects;
        internal static ref global::go.slice<nuint> ᏑStack(ref MemProfileRecord instance) => ref instance.Stack;
        
        // Constructors
        public MemProfileRecord(NilType _)
        {
            this.AllocBytes = default!;
            this.FreeBytes = default!;
            this.AllocObjects = default!;
            this.FreeObjects = default!;
            this.Stack = default!;
        }

        public MemProfileRecord(long AllocBytes = default!, long FreeBytes = default!, long AllocObjects = default!, long FreeObjects = default!, global::go.slice<nuint> Stack = default!)
        {
            this.AllocBytes = AllocBytes;
            this.FreeBytes = FreeBytes;
            this.AllocObjects = AllocObjects;
            this.FreeObjects = FreeObjects;
            this.Stack = Stack;
        }
        
        // Handle comparisons between struct 'MemProfileRecord' instances
        public bool Equals(MemProfileRecord other) =>
            AllocBytes == other.AllocBytes &&
            FreeBytes == other.FreeBytes &&
            AllocObjects == other.AllocObjects &&
            FreeObjects == other.FreeObjects &&
            Stack == other.Stack;
        
        public override bool Equals(object? obj) => obj is MemProfileRecord other && Equals(other);
        
        public override int GetHashCode() => runtime.HashCode.Combine(
            AllocBytes,
            FreeBytes,
            AllocObjects,
            FreeObjects,
            Stack);
        
        public static bool operator ==(MemProfileRecord left, MemProfileRecord right) => left.Equals(right);
        
        public static bool operator !=(MemProfileRecord left, MemProfileRecord right) => !(left == right);

        // Handle comparisons between 'nil' and struct 'MemProfileRecord'
        public static bool operator ==(MemProfileRecord value, NilType nil) => value.Equals(default(MemProfileRecord));

        public static bool operator !=(MemProfileRecord value, NilType nil) => !(value == nil);

        public static bool operator ==(NilType nil, MemProfileRecord value) => value == nil;

        public static bool operator !=(NilType nil, MemProfileRecord value) => value != nil;

        public static implicit operator MemProfileRecord(NilType nil) => default(MemProfileRecord);

        public override string ToString() => string.Concat("{", string.Join(" ",
        [
            AllocBytes.ToString(),
            FreeBytes.ToString(),
            AllocObjects.ToString(),
            FreeObjects.ToString(),
            Stack.ToString()
        ]), "}");
    }
}
