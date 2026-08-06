package main

import (
	"fmt"
	"os"
	"time"
)

func main() {
	r, w, err := os.Pipe()
	if err != nil {
		fmt.Println("pipe error:", err)
		return
	}
	done := make(chan string, 1)
	go func() {
		var buf [16]byte
		_, rerr := r.Read(buf[:])
		if rerr != nil {
			done <- rerr.Error()
		} else {
			done <- "read returned no error"
		}
	}()
	time.Sleep(300 * time.Millisecond)
	r.Close()
	select {
	case msg := <-done:
		fmt.Println("read unblocked:", msg)
	case <-time.After(5 * time.Second):
		fmt.Println("read did NOT unblock")
	}
	w.Close()
}
