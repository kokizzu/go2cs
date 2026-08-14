// Created by cgo -godefs - DO NOT EDIT
// cgo -godefs types_darwin.go
//go:build amd64 && darwin
namespace go;

partial class syscall_package {

internal static UntypedInt sizeofPtr => 0x8;
internal static UntypedInt sizeofShort => 0x2;
internal static UntypedInt sizeofInt => 0x4;
internal static UntypedInt sizeofLong => 0x8;
internal static UntypedInt sizeofLongLong => 0x8;

[GoType("num:int16")] partial struct _C_short;

[GoType("num:int32")] partial struct _C_int;

[GoType("num:int64")] partial struct _C_long;

[GoType("num:int64")] partial struct _C_long_long;

[GoType] partial struct Timespec {
    public int64 Sec;
    public int64 Nsec;
}

[GoType] partial struct Timeval {
    public int64 Sec;
    public int32 Usec;
    public array<byte> Pad_cgo_0 = new(4);
}

[GoType] partial struct Timeval32 {
    public int32 Sec;
    public int32 Usec;
}

[GoType] partial struct Rusage {
    public Timeval Utime;
    public Timeval Stime;
    public int64 Maxrss;
    public int64 Ixrss;
    public int64 Idrss;
    public int64 Isrss;
    public int64 Minflt;
    public int64 Majflt;
    public int64 Nswap;
    public int64 Inblock;
    public int64 Oublock;
    public int64 Msgsnd;
    public int64 Msgrcv;
    public int64 Nsignals;
    public int64 Nvcsw;
    public int64 Nivcsw;
}

[GoType] partial struct Rlimit {
    public uint64 Cur;
    public uint64 Max;
}

[GoType("num:uint32")] partial struct _Gid_t;

[GoType] partial struct Stat_t {
    public int32 Dev;
    public uint16 Mode;
    public uint16 Nlink;
    public uint64 Ino;
    public uint32 Uid;
    public uint32 Gid;
    public int32 Rdev;
    public array<byte> Pad_cgo_0 = new(4);
    public Timespec Atimespec;
    public Timespec Mtimespec;
    public Timespec Ctimespec;
    public Timespec Birthtimespec;
    public int64 Size;
    public int64 Blocks;
    public int32 Blksize;
    public uint32 Flags;
    public uint32 Gen;
    public int32 Lspare;
    public array<int64> Qspare = new(2);
}

[GoType] partial struct Statfs_t {
    public uint32 Bsize;
    public int32 Iosize;
    public uint64 Blocks;
    public uint64 Bfree;
    public uint64 Bavail;
    public uint64 Files;
    public uint64 Ffree;
    public Fsid Fsid;
    public uint32 Owner;
    public uint32 Type;
    public uint32 Flags;
    public uint32 Fssubtype;
    public array<int8> Fstypename = new(16);
    public array<int8> Mntonname = new(1024);
    public array<int8> Mntfromname = new(1024);
    public array<uint32> Reserved = new(8);
}

[GoType] partial struct Flock_t {
    public int64 Start;
    public int64 Len;
    public int32 Pid;
    public int16 Type;
    public int16 Whence;
}

[GoType] partial struct Fstore_t {
    public uint32 Flags;
    public int32 Posmode;
    public int64 Offset;
    public int64 Length;
    public int64 Bytesalloc;
}

[GoType] partial struct Radvisory_t {
    public int64 Offset;
    public int32 Count;
    public array<byte> Pad_cgo_0 = new(4);
}

[GoType] partial struct Fbootstraptransfer_t {
    public int64 Offset;
    public uint64 Length;
    public ж<byte> Buffer;
}

[GoType] partial struct Log2phys_t {
    public uint32 Flags;
    public int64 Contigbytes;
    public int64 Devoffset;
}

[GoType] partial struct Fsid {
    public array<int32> Val = new(2);
}

[GoType] partial struct Dirent {
    public uint64 Ino;
    public uint64 Seekoff;
    public uint16 Reclen;
    public uint16 Namlen;
    public uint8 Type;
    public array<int8> Name = new(1024);
    public array<byte> Pad_cgo_0 = new(3);
}

internal static UntypedInt pathMax => 0x400;

[GoType] partial struct RawSockaddrInet4 {
    public uint8 Len;
    public uint8 Family;
    public uint16 Port;
    public array<byte> Addr = new(4); /* in_addr */
    public array<int8> Zero = new(8);
}

[GoType] partial struct RawSockaddrInet6 {
    public uint8 Len;
    public uint8 Family;
    public uint16 Port;
    public uint32 Flowinfo;
    public array<byte> Addr = new(16); /* in6_addr */
    public uint32 Scope_id;
}

[GoType] partial struct RawSockaddrUnix {
    public uint8 Len;
    public uint8 Family;
    public array<int8> Path = new(104);
}

[GoType] partial struct RawSockaddrDatalink {
    public uint8 Len;
    public uint8 Family;
    public uint16 Index;
    public uint8 Type;
    public uint8 Nlen;
    public uint8 Alen;
    public uint8 Slen;
    public array<int8> Data = new(12);
}

[GoType] partial struct RawSockaddr {
    public uint8 Len;
    public uint8 Family;
    public array<int8> Data = new(14);
}

[GoType] partial struct RawSockaddrAny {
    public RawSockaddr Addr;
    public array<int8> Pad = new(92);
}

[GoType("num:uint32")] public partial struct _Socklen;

[GoType] partial struct Linger {
    public int32 Onoff;
    public int32 ΔLinger;
}

[GoType] partial struct Iovec {
    public ж<byte> Base;
    public uint64 Len;
}

[GoType] partial struct IPMreq {
    public array<byte> Multiaddr = new(4); /* in_addr */
    public array<byte> Interface = new(4); /* in_addr */
}

[GoType] partial struct IPv6Mreq {
    public array<byte> Multiaddr = new(16); /* in6_addr */
    public uint32 Interface;
}

[GoType] partial struct Msghdr {
    public ж<byte> Name;
    public uint32 Namelen;
    public array<byte> Pad_cgo_0 = new(4);
    public ж<Iovec> Iov;
    public int32 Iovlen;
    public array<byte> Pad_cgo_1 = new(4);
    public ж<byte> Control;
    public uint32 Controllen;
    public int32 Flags;
}

[GoType] partial struct Cmsghdr {
    public uint32 Len;
    public int32 Level;
    public int32 Type;
}

[GoType] partial struct Inet4Pktinfo {
    public uint32 Ifindex;
    public array<byte> Spec_dst = new(4); /* in_addr */
    public array<byte> Addr = new(4); /* in_addr */
}

[GoType] partial struct Inet6Pktinfo {
    public array<byte> Addr = new(16); /* in6_addr */
    public uint32 Ifindex;
}

[GoType] partial struct IPv6MTUInfo {
    public RawSockaddrInet6 Addr;
    public uint32 Mtu;
}

[GoType] partial struct ICMPv6Filter {
    public array<uint32> Filt = new(8);
}

public static UntypedInt SizeofSockaddrInet4 => 0x10;
public static UntypedInt SizeofSockaddrInet6 => 0x1c;
public static UntypedInt SizeofSockaddrAny => 0x6c;
public static UntypedInt SizeofSockaddrUnix => 0x6a;
public static UntypedInt SizeofSockaddrDatalink => 0x14;
public static UntypedInt SizeofLinger => 0x8;
public static UntypedInt SizeofIPMreq => 0x8;
public static UntypedInt SizeofIPv6Mreq => 0x14;
public static UntypedInt SizeofMsghdr => 0x30;
public static UntypedInt SizeofCmsghdr => 0xc;
public static UntypedInt SizeofInet4Pktinfo => 0xc;
public static UntypedInt SizeofInet6Pktinfo => 0x14;
public static UntypedInt SizeofIPv6MTUInfo => 0x20;
public static UntypedInt SizeofICMPv6Filter => 0x20;

public static UntypedInt PTRACE_TRACEME => 0x0;
public static UntypedInt PTRACE_CONT => 0x7;
public static UntypedInt PTRACE_KILL => 0x8;

[GoType] partial struct Kevent_t {
    public uint64 Ident;
    public int16 Filter;
    public uint16 Flags;
    public uint32 Fflags;
    public int64 Data;
    public ж<byte> Udata;
}

[GoType] partial struct FdSet {
    public array<int32> Bits = new(32);
}

public static UntypedInt SizeofIfMsghdr => 0x70;
public static UntypedInt SizeofIfData => 0x60;
public static UntypedInt SizeofIfaMsghdr => 0x14;
public static UntypedInt SizeofIfmaMsghdr => 0x10;
public static UntypedInt SizeofIfmaMsghdr2 => 0x14;
public static UntypedInt SizeofRtMsghdr => 0x5c;
public static UntypedInt SizeofRtMetrics => 0x38;

[GoType] partial struct IfMsghdr {
    public uint16 Msglen;
    public uint8 Version;
    public uint8 Type;
    public int32 Addrs;
    public int32 Flags;
    public uint16 Index;
    public array<byte> Pad_cgo_0 = new(2);
    public IfData Data;
}

[GoType] partial struct IfData {
    public uint8 Type;
    public uint8 Typelen;
    public uint8 Physical;
    public uint8 Addrlen;
    public uint8 Hdrlen;
    public uint8 Recvquota;
    public uint8 Xmitquota;
    public uint8 Unused1;
    public uint32 Mtu;
    public uint32 Metric;
    public uint32 Baudrate;
    public uint32 Ipackets;
    public uint32 Ierrors;
    public uint32 Opackets;
    public uint32 Oerrors;
    public uint32 Collisions;
    public uint32 Ibytes;
    public uint32 Obytes;
    public uint32 Imcasts;
    public uint32 Omcasts;
    public uint32 Iqdrops;
    public uint32 Noproto;
    public uint32 Recvtiming;
    public uint32 Xmittiming;
    public Timeval32 Lastchange;
    public uint32 Unused2;
    public uint32 Hwassist;
    public uint32 Reserved1;
    public uint32 Reserved2;
}

[GoType] partial struct IfaMsghdr {
    public uint16 Msglen;
    public uint8 Version;
    public uint8 Type;
    public int32 Addrs;
    public int32 Flags;
    public uint16 Index;
    public array<byte> Pad_cgo_0 = new(2);
    public int32 Metric;
}

[GoType] partial struct IfmaMsghdr {
    public uint16 Msglen;
    public uint8 Version;
    public uint8 Type;
    public int32 Addrs;
    public int32 Flags;
    public uint16 Index;
    public array<byte> Pad_cgo_0 = new(2);
}

[GoType] partial struct IfmaMsghdr2 {
    public uint16 Msglen;
    public uint8 Version;
    public uint8 Type;
    public int32 Addrs;
    public int32 Flags;
    public uint16 Index;
    public array<byte> Pad_cgo_0 = new(2);
    public int32 Refcount;
}

[GoType] partial struct RtMsghdr {
    public uint16 Msglen;
    public uint8 Version;
    public uint8 Type;
    public uint16 Index;
    public array<byte> Pad_cgo_0 = new(2);
    public int32 Flags;
    public int32 Addrs;
    public int32 Pid;
    public int32 Seq;
    public int32 Errno;
    public int32 Use;
    public uint32 Inits;
    public RtMetrics Rmx;
}

[GoType] partial struct RtMetrics {
    public uint32 Locks;
    public uint32 Mtu;
    public uint32 Hopcount;
    public int32 Expire;
    public uint32 Recvpipe;
    public uint32 Sendpipe;
    public uint32 Ssthresh;
    public uint32 Rtt;
    public uint32 Rttvar;
    public uint32 Pksent;
    public array<uint32> Filler = new(4);
}

public static UntypedInt SizeofBpfVersion => 0x4;
public static UntypedInt SizeofBpfStat => 0x8;
public static UntypedInt SizeofBpfProgram => 0x10;
public static UntypedInt SizeofBpfInsn => 0x8;
public static UntypedInt SizeofBpfHdr => 0x14;

[GoType] partial struct BpfVersion {
    public uint16 Major;
    public uint16 Minor;
}

[GoType] partial struct BpfStat {
    public uint32 Recv;
    public uint32 Drop;
}

[GoType] partial struct BpfProgram {
    public uint32 Len;
    public array<byte> Pad_cgo_0 = new(4);
    public ж<BpfInsn> Insns;
}

[GoType] partial struct BpfInsn {
    public uint16 Code;
    public uint8 Jt;
    public uint8 Jf;
    public uint32 K;
}

[GoType] partial struct BpfHdr {
    public Timeval32 Tstamp;
    public uint32 Caplen;
    public uint32 Datalen;
    public uint16 Hdrlen;
    public array<byte> Pad_cgo_0 = new(2);
}

internal static UntypedInt _AT_FDCWD => /* -0x2 */ -2;

[GoType] partial struct Termios {
    public uint64 Iflag;
    public uint64 Oflag;
    public uint64 Cflag;
    public uint64 Lflag;
    public array<uint8> Cc = new(20);
    public array<byte> Pad_cgo_0 = new(4);
    public uint64 Ispeed;
    public uint64 Ospeed;
}

} // end syscall_package
