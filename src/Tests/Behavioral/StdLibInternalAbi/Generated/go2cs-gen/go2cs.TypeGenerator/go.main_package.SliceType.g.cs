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

#nullable enable

namespace go;

public static partial class main_package
{
    [GeneratedCode("go2cs-gen", "0.1.4")]
    public partial struct SliceType
    {
        // Promoted Struct References
        private readonly ж<global::go.main_package.Type> ᏑʗType;

        // Promoted Struct Accessors
        public partial ref global::go.main_package.Type Type => ref ᏑʗType.val;

        // Promoted Struct Field Accessors
        internal ref nuint Size_ => ref Type.Size_;
        internal ref nuint PtrBytes => ref Type.PtrBytes;
        internal ref uint Hash => ref Type.Hash;
        public ref global::go.main_package.TFlag TFlag => ref Type.TFlag;
        internal ref byte Align_ => ref Type.Align_;
        internal ref byte FieldAlign_ => ref Type.FieldAlign_;
        public ref global::go.main_package.ΔKind Kind_ => ref Type.Kind_;
        public ref global::System.Func<global::go.unsafe_package.Pointer, global::go.unsafe_package.Pointer, bool> Equal => ref Type.Equal;
        internal ref global::go.ж<byte> GCData => ref Type.GCData;
        public ref global::go.main_package.NameOff Str => ref Type.Str;
        public ref global::go.main_package.TypeOff PtrToThis => ref Type.PtrToThis;

        // Promoted Struct Field Accessor References
        internal static ref nuint ᏑSize_(ref SliceType instance) => ref instance.Type.Size_;
        internal static ref nuint ᏑPtrBytes(ref SliceType instance) => ref instance.Type.PtrBytes;
        internal static ref uint ᏑHash(ref SliceType instance) => ref instance.Type.Hash;
        public static ref global::go.main_package.TFlag ᏑTFlag(ref SliceType instance) => ref instance.Type.TFlag;
        internal static ref byte ᏑAlign_(ref SliceType instance) => ref instance.Type.Align_;
        internal static ref byte ᏑFieldAlign_(ref SliceType instance) => ref instance.Type.FieldAlign_;
        public static ref global::go.main_package.ΔKind ᏑKind_(ref SliceType instance) => ref instance.Type.Kind_;
        public static ref global::System.Func<global::go.unsafe_package.Pointer, global::go.unsafe_package.Pointer, bool> ᏑEqual(ref SliceType instance) => ref instance.Type.Equal;
        internal static ref global::go.ж<byte> ᏑGCData(ref SliceType instance) => ref instance.Type.GCData;
        public static ref global::go.main_package.NameOff ᏑStr(ref SliceType instance) => ref instance.Type.Str;
        public static ref global::go.main_package.TypeOff ᏑPtrToThis(ref SliceType instance) => ref instance.Type.PtrToThis;

        // Field References
        public static ref global::go.main_package.Type ᏑType(ref SliceType instance) => ref instance.Type;
        public static ref global::go.ж<global::go.main_package.Type> ᏑElem(ref SliceType instance) => ref instance.Elem;
        
        // Constructors
        public SliceType(NilType _)
        {
            ᏑʗType = new ж<global::go.main_package.Type>(new global::go.main_package.Type(nil));
            this.Elem = default!;
        }

        public SliceType(global::go.main_package.Type Type = default!, global::go.ж<global::go.main_package.Type> Elem = default!)
        {
            ᏑʗType = new ж<global::go.main_package.Type>(Type);
            this.Elem = Elem;
        }
        
        // Handle comparisons between struct 'SliceType' instances
        public bool Equals(SliceType other) =>
            Type == other.Type &&
            Elem == other.Elem;
        
        public override bool Equals(object? obj) => obj is SliceType other && Equals(other);
        
        public override int GetHashCode() => runtime.HashCode.Combine(
            Type,
            Elem);
        
        public static bool operator ==(SliceType left, SliceType right) => left.Equals(right);
        
        public static bool operator !=(SliceType left, SliceType right) => !(left == right);

        // Handle comparisons between 'nil' and struct 'SliceType'
        public static bool operator ==(SliceType value, NilType nil) => value.Equals(default(SliceType));

        public static bool operator !=(SliceType value, NilType nil) => !(value == nil);

        public static bool operator ==(NilType nil, SliceType value) => value == nil;

        public static bool operator !=(NilType nil, SliceType value) => value != nil;

        public static implicit operator SliceType(NilType nil) => default(SliceType);

        public override string ToString() => string.Concat("{", string.Join(" ",
        [
            Type.ToString(),
            Elem?.ToString() ?? "<nil>"
        ]), "}");
    }

    // Promoted Struct Receivers
    public static go.main_package.ΔKind Kind(this ref SliceType target) => target.Type.Kind();
    public static go.main_package.ΔKind Kind(this ж<SliceType> Ꮡtarget)
    {
        ref var target = ref Ꮡtarget.val;
        return target.Kind();
    }
    internal static bool HasName(this ref SliceType target) => target.Type.HasName();
    internal static bool HasName(this ж<SliceType> Ꮡtarget)
    {
        ref var target = ref Ꮡtarget.val;
        return target.HasName();
    }
    internal static bool Pointers(this ref SliceType target) => target.Type.Pointers();
    internal static bool Pointers(this ж<SliceType> Ꮡtarget)
    {
        ref var target = ref Ꮡtarget.val;
        return target.Pointers();
    }
    internal static bool IfaceIndir(this ref SliceType target) => target.Type.IfaceIndir();
    internal static bool IfaceIndir(this ж<SliceType> Ꮡtarget)
    {
        ref var target = ref Ꮡtarget.val;
        return target.IfaceIndir();
    }
    internal static bool IsDirectIface(this ref SliceType target) => target.Type.IsDirectIface();
    internal static bool IsDirectIface(this ж<SliceType> Ꮡtarget)
    {
        ref var target = ref Ꮡtarget.val;
        return target.IsDirectIface();
    }
    internal static go.slice<byte> GcSlice(this ref SliceType target, nuint begin, nuint end) => target.Type.GcSlice(begin, end);
    internal static go.slice<byte> GcSlice(this ж<SliceType> Ꮡtarget, nuint begin, nuint end)
    {
        ref var target = ref Ꮡtarget.val;
        return target.GcSlice(begin, end);
    }
    internal static nint Len(this ref SliceType target) => target.Type.Len();
    internal static nint Len(this ж<SliceType> Ꮡtarget)
    {
        ref var target = ref Ꮡtarget.val;
        return target.Len();
    }
    public static go.ж<go.main_package.Type> Common(this ref SliceType target) => target.Type.Common();
    public static go.ж<go.main_package.Type> Common(this ж<SliceType> Ꮡtarget)
    {
        ref var target = ref Ꮡtarget.val;
        return target.Common();
    }
    public static go.main_package.ΔChanDir ChanDir(this ref SliceType target) => target.Type.ChanDir();
    public static go.main_package.ΔChanDir ChanDir(this ж<SliceType> Ꮡtarget)
    {
        ref var target = ref Ꮡtarget.val;
        return target.ChanDir();
    }
    public static go.ж<go.main_package.UncommonType> Uncommon(this ref SliceType target) => target.Type.Uncommon();
    public static go.ж<go.main_package.UncommonType> Uncommon(this ж<SliceType> Ꮡtarget)
    {
        ref var target = ref Ꮡtarget.val;
        return target.Uncommon();
    }
    public static go.ж<go.main_package.Type> Elem(this ref SliceType target) => target.Type.Elem();
    public static go.ж<go.main_package.Type> Elem(this ж<SliceType> Ꮡtarget)
    {
        ref var target = ref Ꮡtarget.val;
        return target.Elem();
    }
    public static go.ж<go.main_package.ΔStructType> StructType(this ref SliceType target) => target.Type.StructType();
    public static go.ж<go.main_package.ΔStructType> StructType(this ж<SliceType> Ꮡtarget)
    {
        ref var target = ref Ꮡtarget.val;
        return target.StructType();
    }
    public static go.ж<go.main_package.ΔMapType> MapType(this ref SliceType target) => target.Type.MapType();
    public static go.ж<go.main_package.ΔMapType> MapType(this ж<SliceType> Ꮡtarget)
    {
        ref var target = ref Ꮡtarget.val;
        return target.MapType();
    }
    public static go.ж<go.main_package.ΔArrayType> ArrayType(this ref SliceType target) => target.Type.ArrayType();
    public static go.ж<go.main_package.ΔArrayType> ArrayType(this ж<SliceType> Ꮡtarget)
    {
        ref var target = ref Ꮡtarget.val;
        return target.ArrayType();
    }
    public static go.ж<go.main_package.ΔFuncType> FuncType(this ref SliceType target) => target.Type.FuncType();
    public static go.ж<go.main_package.ΔFuncType> FuncType(this ж<SliceType> Ꮡtarget)
    {
        ref var target = ref Ꮡtarget.val;
        return target.FuncType();
    }
    public static go.ж<go.main_package.ΔInterfaceType> InterfaceType(this ref SliceType target) => target.Type.InterfaceType();
    public static go.ж<go.main_package.ΔInterfaceType> InterfaceType(this ж<SliceType> Ꮡtarget)
    {
        ref var target = ref Ꮡtarget.val;
        return target.InterfaceType();
    }
    internal static nuint Size(this ref SliceType target) => target.Type.Size();
    internal static nuint Size(this ж<SliceType> Ꮡtarget)
    {
        ref var target = ref Ꮡtarget.val;
        return target.Size();
    }
    internal static nint Align(this ref SliceType target) => target.Type.Align();
    internal static nint Align(this ж<SliceType> Ꮡtarget)
    {
        ref var target = ref Ꮡtarget.val;
        return target.Align();
    }
    internal static nint FieldAlign(this ref SliceType target) => target.Type.FieldAlign();
    internal static nint FieldAlign(this ж<SliceType> Ꮡtarget)
    {
        ref var target = ref Ꮡtarget.val;
        return target.FieldAlign();
    }
    public static go.slice<go.main_package.Method> ExportedMethods(this ref SliceType target) => target.Type.ExportedMethods();
    public static go.slice<go.main_package.Method> ExportedMethods(this ж<SliceType> Ꮡtarget)
    {
        ref var target = ref Ꮡtarget.val;
        return target.ExportedMethods();
    }
    internal static nint NumMethod(this ref SliceType target) => target.Type.NumMethod();
    internal static nint NumMethod(this ж<SliceType> Ꮡtarget)
    {
        ref var target = ref Ꮡtarget.val;
        return target.NumMethod();
    }
    public static go.ж<go.main_package.Type> Key(this ref SliceType target) => target.Type.Key();
    public static go.ж<go.main_package.Type> Key(this ж<SliceType> Ꮡtarget)
    {
        ref var target = ref Ꮡtarget.val;
        return target.Key();
    }
}
