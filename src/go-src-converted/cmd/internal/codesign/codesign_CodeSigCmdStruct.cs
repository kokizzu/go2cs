//---------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool. Changes to this
//     file may cause incorrect behavior and will be lost
//     if the code is regenerated.
//
//     Generated on 2022 March 13 05:57:56 UTC
// </auto-generated>
//---------------------------------------------------------
using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using sha256 = go.crypto.sha256_package;
using macho = go.debug.macho_package;
using binary = go.encoding.binary_package;
using io = go.io_package;
using go;

#nullable enable

namespace go {
namespace cmd {
namespace @internal
{
    public static partial class codesign_package
    {
        [GeneratedCode("go2cs", "0.1.2.0")]
        public partial struct CodeSigCmd
        {
            // Constructors
            public CodeSigCmd(NilType _)
            {
                this.Cmd = default;
                this.Cmdsize = default;
                this.Dataoff = default;
                this.Datasize = default;
            }

            public CodeSigCmd(uint Cmd = default, uint Cmdsize = default, uint Dataoff = default, uint Datasize = default)
            {
                this.Cmd = Cmd;
                this.Cmdsize = Cmdsize;
                this.Dataoff = Dataoff;
                this.Datasize = Datasize;
            }

            // Enable comparisons between nil and CodeSigCmd struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(CodeSigCmd value, NilType nil) => value.Equals(default(CodeSigCmd));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(CodeSigCmd value, NilType nil) => !(value == nil);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(NilType nil, CodeSigCmd value) => value == nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(NilType nil, CodeSigCmd value) => value != nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator CodeSigCmd(NilType nil) => default(CodeSigCmd);
        }

        [GeneratedCode("go2cs", "0.1.2.0")]
        public static CodeSigCmd CodeSigCmd_cast(dynamic value)
        {
            return new CodeSigCmd(value.Cmd, value.Cmdsize, value.Dataoff, value.Datasize);
        }
    }
}}}