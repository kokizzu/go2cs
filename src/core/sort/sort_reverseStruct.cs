//---------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool. Changes to this
//     file may cause incorrect behavior and will be lost
//     if the code is regenerated.
//
//     Generated on 2020 October 09 04:49:20 UTC
// </auto-generated>
//---------------------------------------------------------
using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using go.runtime;

#nullable enable

namespace go;

public static partial class sort_package
{
    [GeneratedCode("go2cs", "0.1.0.0")]
    private partial struct reverse
    {
        // Interface.Len function promotion
        private delegate nint LenByVal(reverse value);
        private delegate nint LenByRef(ref reverse value);

        private static readonly LenByVal? s_LenByVal;
        private static readonly LenByRef? s_LenByRef;

        [DebuggerNonUserCode, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public nint Len() => s_LenByRef?.Invoke(ref this) ?? s_LenByVal?.Invoke(this) ?? Interface?.Len() ?? throw RuntimeErrorPanic.NilPointerDereference();

        // Interface.Less function promotion
        private delegate bool LessByVal(reverse value, nint i, nint j);
        private delegate bool LessByRef(ref reverse value, nint i, nint j);

        private static readonly LessByVal? s_LessByVal;
        private static readonly LessByRef? s_LessByRef;

        [DebuggerNonUserCode, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Less(nint i, nint j) => s_LessByRef?.Invoke(ref this, i, j) ?? s_LessByVal?.Invoke(this, i, j) ?? Interface?.Less(i, j) ?? throw RuntimeErrorPanic.NilPointerDereference();

        // Interface.Swap function promotion
        private delegate void SwapByVal(reverse value, nint i, nint j);
        private delegate void SwapByRef(ref reverse value, nint i, nint j);

        private static readonly SwapByVal? s_SwapByVal;
        private static readonly SwapByRef? s_SwapByRef;

        [DebuggerNonUserCode, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Swap(nint i, nint j)
        {
            if (s_SwapByRef is not null)
            {
                s_SwapByRef.Invoke(ref this, i, j);
                return;
            }

            if (s_SwapByVal is not null)
            {
                s_SwapByVal.Invoke(this, i, j);
                return;
            }

            if (Interface is not null)
            {
                Interface.Swap(i, j);
                return;
            }

            throw RuntimeErrorPanic.NilPointerDereference();
        }

            
        [DebuggerStepperBoundary]
        static reverse()
        {
            Type targetType = typeof(reverse);
            MethodInfo? extensionMethod;
                
            extensionMethod = targetType.GetExtensionMethod("Len");

            if (!(extensionMethod is null))
            {
                s_LenByRef = extensionMethod.CreateStaticDelegate(typeof(LenByRef)) as LenByRef;

                if (s_LenByRef is null)
                    s_LenByVal = extensionMethod.CreateStaticDelegate(typeof(LenByVal)) as LenByVal;
            }
                
            extensionMethod = targetType.GetExtensionMethod("Less");

            if (!(extensionMethod is null))
            {
                s_LessByRef = extensionMethod.CreateStaticDelegate(typeof(LessByRef)) as LessByRef;

                if (s_LessByRef is null)
                    s_LessByVal = extensionMethod.CreateStaticDelegate(typeof(LessByVal)) as LessByVal;
            }
                
            extensionMethod = targetType.GetExtensionMethod("Swap");

            if (!(extensionMethod is null))
            {
                s_SwapByRef = extensionMethod.CreateStaticDelegate(typeof(SwapByRef)) as SwapByRef;

                if (s_SwapByRef is null)
                    s_SwapByVal = extensionMethod.CreateStaticDelegate(typeof(SwapByVal)) as SwapByVal;
            }
        }

        // Constructors
        public reverse(NilType _)
        {
            this.Interface = default!;
        }

        public reverse(Interface Interface = default!)
        {
            this.Interface = Interface;
        }

        // Enable comparisons between nil and reverse struct
        public static bool operator ==(reverse value, NilType nil) => value.Equals(default(reverse));

        public static bool operator !=(reverse value, NilType nil) => !(value == nil);

        public static bool operator ==(NilType nil, reverse value) => value == nil;

        public static bool operator !=(NilType nil, reverse value) => value != nil;

        public static implicit operator reverse(NilType nil) => default(reverse);
    }

    [GeneratedCode("go2cs", "0.1.0.0")]
    private static reverse reverse_cast(dynamic value)
    {
        return new reverse(value.Interface);
    }
}
