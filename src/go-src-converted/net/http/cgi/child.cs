// Copyright 2011 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.

// This file implements CGI from the perspective of a child
// process.

// package cgi -- go2cs converted at 2022 March 13 05:37:50 UTC
// import "net/http/cgi" ==> using cgi = go.net.http.cgi_package
// Original source: C:\Program Files\Go\src\net\http\cgi\child.go
namespace go.net.http;

using bufio = bufio_package;
using tls = crypto.tls_package;
using errors = errors_package;
using fmt = fmt_package;
using io = io_package;
using net = net_package;
using http = net.http_package;
using url = net.url_package;
using os = os_package;
using strconv = strconv_package;
using strings = strings_package;


// Request returns the HTTP request as represented in the current
// environment. This assumes the current program is being run
// by a web server in a CGI environment.
// The returned Request's Body is populated, if applicable.

public static partial class cgi_package {

public static (ptr<http.Request>, error) Request() {
    ptr<http.Request> _p0 = default!;
    error _p0 = default!;

    var (r, err) = RequestFromMap(envMap(os.Environ()));
    if (err != null) {
        return (_addr_null!, error.As(err)!);
    }
    if (r.ContentLength > 0) {
        r.Body = io.NopCloser(io.LimitReader(os.Stdin, r.ContentLength));
    }
    return (_addr_r!, error.As(null!)!);
}

private static map<@string, @string> envMap(slice<@string> env) {
    var m = make_map<@string, @string>();
    foreach (var (_, kv) in env) {
        {
            var idx = strings.Index(kv, "=");

            if (idx != -1) {
                m[kv[..(int)idx]] = kv[(int)idx + 1..];
            }

        }
    }    return m;
}

// RequestFromMap creates an http.Request from CGI variables.
// The returned Request's Body field is not populated.
public static (ptr<http.Request>, error) RequestFromMap(map<@string, @string> @params) {
    ptr<http.Request> _p0 = default!;
    error _p0 = default!;

    ptr<http.Request> r = @new<http.Request>();
    r.Method = params["REQUEST_METHOD"];
    if (r.Method == "") {
        return (_addr_null!, error.As(errors.New("cgi: no REQUEST_METHOD in environment"))!);
    }
    r.Proto = params["SERVER_PROTOCOL"];
    bool ok = default;
    r.ProtoMajor, r.ProtoMinor, ok = http.ParseHTTPVersion(r.Proto);
    if (!ok) {
        return (_addr_null!, error.As(errors.New("cgi: invalid SERVER_PROTOCOL version"))!);
    }
    r.Close = true;
    r.Trailer = new http.Header();
    r.Header = new http.Header();

    r.Host = params["HTTP_HOST"];

    {
        var lenstr = params["CONTENT_LENGTH"];

        if (lenstr != "") {
            var (clen, err) = strconv.ParseInt(lenstr, 10, 64);
            if (err != null) {
                return (_addr_null!, error.As(errors.New("cgi: bad CONTENT_LENGTH in environment: " + lenstr))!);
            }
            r.ContentLength = clen;
        }
    }

    {
        var ct = params["CONTENT_TYPE"];

        if (ct != "") {
            r.Header.Set("Content-Type", ct);
        }
    } 

    // Copy "HTTP_FOO_BAR" variables to "Foo-Bar" Headers
    foreach (var (k, v) in params) {
        if (!strings.HasPrefix(k, "HTTP_") || k == "HTTP_HOST") {
            continue;
        }
        r.Header.Add(strings.ReplaceAll(k[(int)5..], "_", "-"), v);
    }    var uriStr = params["REQUEST_URI"];
    if (uriStr == "") { 
        // Fallback to SCRIPT_NAME, PATH_INFO and QUERY_STRING.
        uriStr = params["SCRIPT_NAME"] + params["PATH_INFO"];
        var s = params["QUERY_STRING"];
        if (s != "") {
            uriStr += "?" + s;
        }
    }
    {
        var s__prev1 = s;

        s = params["HTTPS"];

        if (s == "on" || s == "ON" || s == "1") {
            r.TLS = addr(new tls.ConnectionState(HandshakeComplete:true));
        }
        s = s__prev1;

    }

    if (r.Host != "") { 
        // Hostname is provided, so we can reasonably construct a URL.
        var rawurl = r.Host + uriStr;
        if (r.TLS == null) {
            rawurl = "http://" + rawurl;
        }
        else
 {
            rawurl = "https://" + rawurl;
        }
        var (url, err) = url.Parse(rawurl);
        if (err != null) {
            return (_addr_null!, error.As(errors.New("cgi: failed to parse host and REQUEST_URI into a URL: " + rawurl))!);
        }
        r.URL = url;
    }
    if (r.URL == null) {
        (url, err) = url.Parse(uriStr);
        if (err != null) {
            return (_addr_null!, error.As(errors.New("cgi: failed to parse REQUEST_URI into a URL: " + uriStr))!);
        }
        r.URL = url;
    }
    var (remotePort, _) = strconv.Atoi(params["REMOTE_PORT"]); // zero if unset or invalid
    r.RemoteAddr = net.JoinHostPort(params["REMOTE_ADDR"], strconv.Itoa(remotePort));

    return (_addr_r!, error.As(null!)!);
}

// Serve executes the provided Handler on the currently active CGI
// request, if any. If there's no current CGI environment
// an error is returned. The provided handler may be nil to use
// http.DefaultServeMux.
public static error Serve(http.Handler handler) {
    var (req, err) = Request();
    if (err != null) {
        return error.As(err)!;
    }
    if (req.Body == null) {
        req.Body = http.NoBody;
    }
    if (handler == null) {
        handler = http.DefaultServeMux;
    }
    ptr<response> rw = addr(new response(req:req,header:make(http.Header),bufw:bufio.NewWriter(os.Stdout),));
    handler.ServeHTTP(rw, req);
    rw.Write(null); // make sure a response is sent
    err = rw.bufw.Flush();

    if (err != null) {
        return error.As(err)!;
    }
    return error.As(null!)!;
}

private partial struct response {
    public ptr<http.Request> req;
    public http.Header header;
    public nint code;
    public bool wroteHeader;
    public bool wroteCGIHeader;
    public ptr<bufio.Writer> bufw;
}

private static void Flush(this ptr<response> _addr_r) {
    ref response r = ref _addr_r.val;

    r.bufw.Flush();
}

private static http.Header Header(this ptr<response> _addr_r) {
    ref response r = ref _addr_r.val;

    return r.header;
}

private static (nint, error) Write(this ptr<response> _addr_r, slice<byte> p) {
    nint n = default;
    error err = default!;
    ref response r = ref _addr_r.val;

    if (!r.wroteHeader) {
        r.WriteHeader(http.StatusOK);
    }
    if (!r.wroteCGIHeader) {
        r.writeCGIHeader(p);
    }
    return r.bufw.Write(p);
}

private static void WriteHeader(this ptr<response> _addr_r, nint code) {
    ref response r = ref _addr_r.val;

    if (r.wroteHeader) { 
        // Note: explicitly using Stderr, as Stdout is our HTTP output.
        fmt.Fprintf(os.Stderr, "CGI attempted to write header twice on request for %s", r.req.URL);
        return ;
    }
    r.wroteHeader = true;
    r.code = code;
}

// writeCGIHeader finalizes the header sent to the client and writes it to the output.
// p is not written by writeHeader, but is the first chunk of the body
// that will be written. It is sniffed for a Content-Type if none is
// set explicitly.
private static void writeCGIHeader(this ptr<response> _addr_r, slice<byte> p) {
    ref response r = ref _addr_r.val;

    if (r.wroteCGIHeader) {
        return ;
    }
    r.wroteCGIHeader = true;
    fmt.Fprintf(r.bufw, "Status: %d %s\r\n", r.code, http.StatusText(r.code));
    {
        var (_, hasType) = r.header["Content-Type"];

        if (!hasType) {
            r.header.Set("Content-Type", http.DetectContentType(p));
        }
    }
    r.header.Write(r.bufw);
    r.bufw.WriteString("\r\n");
    r.bufw.Flush();
}

} // end cgi_package
