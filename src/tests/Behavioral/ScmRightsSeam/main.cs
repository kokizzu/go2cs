namespace go;

using fmt = fmt_package;
using syscall = syscall_package;

partial class main_package {

// Hoisted @string literals (single allocation; Go keeps these in RODATA)
private static readonly object failedˢ = (@string)"failed:"u8;

internal static void fatal(@string what, error err) {
    if (err != default!) {
        fmt.Println(what, failedˢ, err);
        throw panic(what);
    }
}

// Hoisted @string literals (single allocation; Go keeps these in RODATA)
private static readonly @string pipe2ˢ = "pipe2"u8;
private static readonly @string writePipeˢ = "write(pipe)"u8;
private static readonly @string closePipeWriteˢ = "close(pipeWrite)"u8;
private static readonly object bytesStagedInThePipeˢ = (@string)"bytes staged in the pipe:"u8;
private static readonly @string socketpairˢ = "socketpair"u8;
private static readonly object controlImageIsNonEmptyˢ = (@string)"control image is non-empty:"u8;
private static readonly @string sendmsgˢ = "sendmsg"u8;
private static readonly object payloadBytesSentˢ = (@string)"payload bytes sent:"u8;
private static readonly @string recvmsgˢ = "recvmsg"u8;
private static readonly object payloadByteReceivedˢ = (@string)"payload byte received:"u8;
private static readonly object controlBytesReceivedˢ = (@string)"control bytes received:"u8;
private static readonly @string parseControlˢ = "parse control"u8;
private static readonly object controlMessagesˢ = (@string)"control messages:"u8;
private static readonly @string parseRightsˢ = "parse rights"u8;
private static readonly object descriptorsReceivedˢ = (@string)"descriptors received:"u8;
private static readonly @string readReceivedFdˢ = "read(received fd)"u8;
private static readonly object receivedDescriptorReadsˢ = (@string)"received descriptor reads the staged bytes:"u8;

internal static void Main() {
    GoFrame ᒐ = default;
    try {
        @string secret = "scm-rights-payload"u8;
        array<nint> pipeFds = new(2);
        fatal(pipe2ˢ, syscall.Pipe2(pipeFds[..], 0));
        nint pipeRead = pipeFds[0];
        nint pipeWrite = pipeFds[1];
        defer(syscall.Close, pipeRead, ref ᒐ);
        var (n, err) = syscall.Write(pipeWrite, slice<byte>(secret));
        fatal(writePipeˢ, err);
        fatal(closePipeWriteˢ, syscall.Close(pipeWrite));
        fmt.Println(bytesStagedInThePipeˢ, n == len(secret));
        (var pair, err) = syscall.Socketpair(syscall.AF_UNIX, syscall.SOCK_STREAM, 0);
        fatal(socketpairˢ, err);
        nint sender = pair[0];
        nint receiver = pair[1];
        defer(syscall.Close, sender, ref ᒐ);
        defer(syscall.Close, receiver, ref ᒐ);
        var rights = syscall.UnixRights(pipeRead);
        fmt.Println(controlImageIsNonEmptyˢ, len(rights) > 0);
        (var sent, err) = syscall.SendmsgN(sender, new byte[]{(rune)'x'}.slice(), rights, default!, 0);
        fatal(sendmsgˢ, err);
        fmt.Println(payloadBytesSentˢ, sent == 1);
        var payload = new slice<byte>(8);
        var oob = new slice<byte>(syscall.CmsgSpace(4));
        (var rn, var oobn, _, _, err) = syscall.Recvmsg(receiver, payload, oob, 0);
        fatal(recvmsgˢ, err);
        fmt.Println(payloadByteReceivedˢ, rn == 1 && payload[0] == (rune)'x');
        fmt.Println(controlBytesReceivedˢ, oobn == len(oob));
        (var scms, err) = syscall.ParseSocketControlMessage(oob[..(int)(oobn)]);
        fatal(parseControlˢ, err);
        fmt.Println(controlMessagesˢ, len(scms));
        (var fds, err) = syscall.ParseUnixRights(Ꮡ(scms, 0));
        fatal(parseRightsˢ, err);
        fmt.Println(descriptorsReceivedˢ, len(fds));
        var got = new slice<byte>(len(secret));
        (rn, err) = syscall.Read(fds[0], got);
        fatal(readReceivedFdˢ, err);
        syscall.Close(fds[0]);
        fmt.Println(receivedDescriptorReadsˢ, rn == len(secret) && ((sstring)(got[..(int)(rn)])) == secret);
    }
    catch (Exception ᒐex) when (GoFrame.IsPanic(ᒐex, out PanicException? ᒐp)) { GoFrame.Capture(ᒐp); }
    finally { ᒐ.Run(); }
}

} // end main_package
