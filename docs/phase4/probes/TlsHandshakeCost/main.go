package main

import (
	"fmt"
	"io"
	"net/http"
	"net/http/httptest"
	"time"
)

// Answers the one question left open on net/http's h2 write-deadline divergence: is our TLS
// handshake merely SLOWER than the deadline Go meets, or is the deadline applied where Go does
// not apply it? This measures only the first half -- handshake cost with NO WriteTimeout set --
// so the two explanations can be told apart instead of guessed between.
//
// TestWriteDeadlineEnforcedPerStream sets WriteTimeout = timeout/2 and its harness escalates
// 250ms -> 500ms -> 1s. A handshake comfortably under 125ms makes "too slow" implausible; one
// near or above it makes it the likely story.
func main() {
	ts := httptest.NewTLSServer(http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		io.WriteString(w, "ok")
	}))
	defer ts.Close()

	c := ts.Client()
	// Defeat connection reuse so every iteration pays a full handshake.
	c.Transport.(*http.Transport).DisableKeepAlives = true

	const n = 10
	var worst time.Duration
	var total time.Duration
	for i := 0; i < n; i++ {
		start := time.Now()
		res, err := c.Get(ts.URL)
		if err != nil {
			fmt.Println("request error:", err)
			return
		}
		io.ReadAll(res.Body)
		res.Body.Close()
		d := time.Since(start)
		total += d
		if d > worst {
			worst = d
		}
	}
	fmt.Printf("handshakes=%d mean=%dms worst=%dms\n", n, total.Milliseconds()/n, worst.Milliseconds())
}
