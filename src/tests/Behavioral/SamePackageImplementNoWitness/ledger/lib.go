// ledger plays encoding/binary: it declares an EXPORTED interface plus value-receiver
// implementers and — unlike ForeignValueImplementSuppression's hue — never converts one to
// the other itself. Go satisfies an interface structurally, so this assembly really does
// implement those pairs; there is simply no cast anywhere for the converter to record from
// (`var BigEndian bigEndian` carries no `var _ ByteOrder = BigEndian` witness). The declaring
// side must therefore record what Go already says is true, or every consumer mints its own
// `ledger_<T>ᴠMetric` adapter — a SECOND IDENTITY for one Go value.
//
// The POINTER method set is now recorded on the same principle, so Tally below moved from
// negative to positive: its `*Tally → Metric` pair is realized ONCE, as ledger's own
// `TallyжMetric`, instead of once per consumer. Everything else below the positives is still a
// NEGATIVE the recording rule has to leave alone, and each one is live rather than merely
// asserted.
package ledger

// Metric is the EXPORTED interface. Exported is load-bearing: a record is a CROSS-ASSEMBLY
// contract, and no other assembly can name an unexported interface.
type Metric interface {
	Value() int
}

// Counter's VALUE method set implements Metric. Nothing in this package converts one to the
// other, so pre-fix its metadata carried no pair at all.
type Counter struct {
	N int
}

func (c Counter) Value() int { return c.N }

// Gauge is a second implementer over a named NUMERIC rather than a struct, so no fix can be a
// one-shape special case (encoding/binary has two, image/color five).
type Gauge int

func (g Gauge) Value() int { return int(g) * 2 }

// Meter is the DELEGATE NEGATIVE: a named FUNC type whose value method set also implements
// Metric, and which is likewise never converted here. A C# delegate cannot be a partial
// struct, so go2cs-gen realizes such a pair as an adapter CLASS — recording it would have a
// consumer hand a bare delegate to an interface slot (CS0029). net/http's HandlerFunc →
// Handler is the corpus instance, and it is the reason the rule gates on the target's Go
// underlying not being a *types.Signature.
type Meter func() int

func (m Meter) Value() int { return m() }

// Tally implements Metric only through its POINTER method set — the POSITIVE for that form, and
// the exact shape the syscall Sockaddr regression had. A pointer record is an adapter-class
// EXISTENCE signal rather than an implicit-conversion licence, so recording it makes the
// consumer reference ledger's own `TallyжMetric` instead of minting `ledger_TallyжMetric`. Both
// adapters alias the same box and compare equal, so the failure this prevents is not a wrong
// answer but a NON-DETERMINISTIC one: each adapter's module initializer registers itself with
// AdapterRegistry first-wins, so which class a type assert re-wraps into followed assembly load
// order.
type Tally struct {
	N int
}

func (t *Tally) Value() int { return t.N }

// tick is the UNEXPORTED-TARGET NEGATIVE, and it exists only for the POINTER form. A value
// record is consumed by an implicit conversion and needs no name, so an unexported target is
// harmless there and the value rule gates only the interface. A POINTER record is consumed by
// NAMING the generated `<T>ж<Iface>` class, and ImplementGenerator scopes that class `internal`
// unless BOTH sides are exported — so recording this pair would advertise a class no other
// assembly can reference (CS0122). ledger's metadata must stay empty of it.
type tick struct {
	N int
}

func (t *tick) Value() int { return t.N }

// Count keeps tick live WITHOUT converting it to Metric.
func Count(n int) int { return (&tick{N: n}).Value() }

// Box is the GENERIC NEGATIVE. A type parameter cannot appear in an assembly-attribute type
// argument (CS0246), so neither side of a recorded pair may be generic.
type Box[T any] struct {
	V T
}

func (b Box[T]) Value() int { return 3 }

// probe is the UNEXPORTED-INTERFACE NEGATIVE, and well satisfies it with a VALUE receiver.
// Nothing converts one to the other — Depth calls through the concrete type — so no cast can
// record the pair either, and the metadata must stay empty of it: no other assembly could
// name `probe` to consult a record for it.
type probe interface {
	depth() int
}

type well struct {
	D int
}

func (w well) depth() int { return w.D }

// Depth keeps well live WITHOUT converting it to probe.
func Depth(d int) int { return well{D: d}.depth() }
