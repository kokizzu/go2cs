package main

import (
	"io"
	"strings"
	"sync"
	"unicode"
)

// The Tour of Go tags every write it renders with the stream that produced it and
// colors the result: standard error is red, and the Tour's own notices (a program
// exit, a kill, a timeout) are muted. Tour of go2cs reproduces that tagging so the
// .NET pane reads exactly like the Go pane beside it.
const (
	outputStdout = "stdout"
	outputStderr = "stderr"
	outputSystem = "system"
)

// outputSegment is one run of stage output, tagged with its stream.
type outputSegment struct {
	Kind string `json:"kind"`
	Text string `json:"text"`
}

// outputStream collects a command's streams in the order they arrive, merging
// consecutive writes from the same stream so the transcript stays as close to
// what a terminal would have shown as separate pipes allow.
type outputStream struct {
	mu       sync.Mutex
	segments []outputSegment
}

func (s *outputStream) writer(kind string) io.Writer {
	return streamWriter{stream: s, kind: kind}
}

func (s *outputStream) collect() []outputSegment {
	s.mu.Lock()
	defer s.mu.Unlock()
	return append([]outputSegment(nil), s.segments...)
}

func (s *outputStream) append(kind, text string) {
	if text == "" {
		return
	}
	s.mu.Lock()
	defer s.mu.Unlock()
	if last := len(s.segments) - 1; last >= 0 && s.segments[last].Kind == kind {
		s.segments[last].Text += text
		return
	}
	s.segments = append(s.segments, outputSegment{Kind: kind, Text: text})
}

type streamWriter struct {
	stream *outputStream
	kind   string
}

func (w streamWriter) Write(p []byte) (int, error) {
	w.stream.append(w.kind, string(p))
	return len(p), nil
}

// appendSystemSegment follows a stage's own output with one of the host's
// notices, spaced exactly as the Tour spaces its own: a single newline, and no
// trimming of what came before. So the blank line ahead of the notice appears
// precisely when the program's last line ended with one -- a program whose
// output stops mid-line keeps the notice on the very next line.
func appendSystemSegment(segments []outputSegment, line string) []outputSegment {
	if len(segments) == 0 {
		return []outputSegment{{Kind: outputSystem, Text: line}}
	}
	return append(segments, outputSegment{Kind: outputSystem, Text: "\n" + line})
}

// trimSegments trims the transcript exactly as strings.TrimSpace trims the joined
// text, so a tagged transcript and a plain one still read identically.
func trimSegments(segments []outputSegment) []outputSegment {
	for len(segments) > 0 {
		segments[0].Text = strings.TrimLeftFunc(segments[0].Text, unicode.IsSpace)
		if segments[0].Text != "" {
			break
		}
		segments = segments[1:]
	}
	for len(segments) > 0 {
		last := len(segments) - 1
		segments[last].Text = strings.TrimRightFunc(segments[last].Text, unicode.IsSpace)
		if segments[last].Text != "" {
			break
		}
		segments = segments[:last]
	}
	return segments
}

func joinSegments(segments []outputSegment) string {
	var text strings.Builder
	for _, segment := range segments {
		text.WriteString(segment.Text)
	}
	return text.String()
}
