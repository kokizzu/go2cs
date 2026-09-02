package main

import (
	"fmt"
	"net/http"
	"net/http/httptest"
	"os"
	"strconv"
	"time"
)

// COORD's discrimination for net/http's h2 write-deadline divergence, done at budgets the real
// test cannot reach: tryTimeouts caps at 1s (WriteTimeout 500ms), and our TLS handshake measures
// ~691ms mean / ~1130ms worst, so the real test can never distinguish the two explanations.
//
//	passes at SOME budget -> performance gap (we just cannot meet 500ms)
//	fails at EVERY budget -> semantic: the deadline spans the handshake where Go's does not
//
// Mirrors testWriteDeadlineEnforcedPerStream (serve_test.go:1008) in h2 mode: WriteTimeout =
// timeout/2, first request must SUCCEED, second must ERROR because the handler sleeps.
func attempt(timeout time.Duration) error {
	firstRequest := make(chan bool, 1)

	ts := httptest.NewUnstartedServer(http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		select {
		case firstRequest <- true:
		default:
			time.Sleep(timeout)
		}
	}))
	ts.EnableHTTP2 = true
	ts.Config.WriteTimeout = timeout / 2
	ts.StartTLS()
	defer ts.Close()

	c := ts.Client()

	r, err := c.Get(ts.URL)
	if err != nil {
		return fmt.Errorf("Get #1: %v", err)
	}
	r.Body.Close()

	r, err = c.Get(ts.URL)
	if err == nil {
		r.Body.Close()
		return fmt.Errorf("Get #2 expected error, got nil")
	}
	return nil
}

func main() {
	// The argument is the BUDGET, and the server's WriteTimeout is budget/2 -- so the run that
	// tests Go's real ceiling (WriteTimeout 500ms, the largest tryTimeouts ever sets) is
	// `WriteDeadlineBudget 1000`, NOT 500. Every result line prints both numbers so the mapping
	// cannot be misread from the output either; an earlier prose instruction of mine got this
	// wrong and was caught only because the reader checked this source instead.
	budgets := []time.Duration{250 * time.Millisecond, time.Second, 4 * time.Second, 16 * time.Second}
	if len(os.Args) > 1 {
		if ms, err := strconv.Atoi(os.Args[1]); err == nil {
			budgets = []time.Duration{time.Duration(ms) * time.Millisecond}
		}
	}
	for _, b := range budgets {
		if err := attempt(b); err != nil {
			fmt.Printf("budget %-6v (WriteTimeout %-6v) FAIL  %v\n", b, b/2, err)
		} else {
			fmt.Printf("budget %-6v (WriteTimeout %-6v) PASS\n", b, b/2)
		}
	}
}
