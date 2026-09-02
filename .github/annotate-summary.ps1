<#
.SYNOPSIS
    Echo a job summary as GitHub ANNOTATIONS, so a leg's headline is readable through the REST
    API alone.

.DESCRIPTION
    A leg's evidence normally lands in two places, and BOTH are served from Azure blob storage:
    the job summary and the uploaded artifact (`productionresultssa*.blob.core.windows.net`, via
    a 302 from api.github.com). A host whose egress policy allows `api.github.com` but denies that
    blob domain can therefore dispatch a run, read every job and step CONCLUSION -- and not one
    line of what the run measured. That is the exact position a restricted-egress lane is in, and
    it turns the matrix into a pass/fail light: a darwin census that walls reports "failure" and
    nothing else, which is precisely the reading CIMatrix.md says not to take.

    Annotations are different: they come back from `GET /repos/{owner}/{repo}/check-runs/{id}
    /annotations` as JSON from api.github.com itself, no redirect and no blob domain. Emitting the
    summary as annotations therefore makes every leg's headline retrievable with nothing but the
    API -- and, incidentally, puts it on the run page where a human sees it without opening the
    summary tab.

    Nothing is replaced: the step summary and the artifact are written exactly as before. This is
    an additional, cheaper-to-read copy of the same text.

.NOTES
    GitHub keeps at most 10 annotations PER LEVEL per step, so the chunk count is capped well
    under that and the caller is told when text was dropped rather than losing it silently.
    Message length is capped too -- GitHub truncates a long annotation, and a truncated table is
    worse than a stated omission.
#>
[CmdletBinding()]
param(
    # Prefix for each annotation's title, e.g. 'census darwin/osx-arm64'.
    [Parameter(Mandatory)]
    [string] $Title,

    # The summary lines, in order. Emitted verbatim apart from workflow-command escaping.
    #
    # AllowEmptyString is load-bearing, not defensive: PowerShell rejects a Mandatory [string[]]
    # argument the moment ONE element is the empty string, and every summary this is called with
    # is full of them (a markdown table needs blank lines around it). Without it the helper throws
    # `Cannot bind argument to parameter 'Lines' because it is an empty string` on every real
    # call while passing any hand-written test whose lines all have content -- a helper that could
    # never fire, which is how it shipped in the first version.
    [Parameter(Mandatory)]
    [AllowEmptyCollection()]
    [AllowEmptyString()]
    [string[]] $Lines,

    # Per-annotation character budget. GitHub truncates beyond roughly 4 KB; stay clear of it.
    [int] $MaxChars = 3000,

    # Per-level annotation cap is 10; leave room for a runner-emitted warning or two.
    [int] $MaxChunks = 8
)

Set-StrictMode -Version Latest

# Workflow-command escaping. The message needs %/CR/LF escaped; a PROPERTY value additionally
# needs ':' and ',' escaped or the parser splits the command on them.
function ConvertTo-CommandData([string] $Text) {
    $Text.Replace('%', '%25').Replace("`r", '%0D').Replace("`n", '%0A')
}

function ConvertTo-CommandProperty([string] $Text) {
    (ConvertTo-CommandData $Text).Replace(':', '%3A').Replace(',', '%2C')
}

# Pack lines into as few chunks as the budget allows, never splitting a line across two.
$chunks = New-Object System.Collections.Generic.List[string]
$current = New-Object System.Text.StringBuilder

foreach ($line in $Lines) {
    $text = if ($null -eq $line) { '' } else { $line }

    # A single line over budget cannot be packed; emit what fits and say so, rather than dropping
    # it or blowing the annotation.
    if ($text.Length -gt $MaxChars) {
        $text = $text.Substring(0, $MaxChars - 3) + '...'
    }

    if ($current.Length -gt 0 -and ($current.Length + 1 + $text.Length) -gt $MaxChars) {
        $chunks.Add($current.ToString())
        $current = New-Object System.Text.StringBuilder
    }

    if ($current.Length -gt 0) { $null = $current.Append("`n") }
    $null = $current.Append($text)
}

if ($current.Length -gt 0) { $chunks.Add($current.ToString()) }

$emitted = [Math]::Min($chunks.Count, $MaxChunks)

for ($i = 0; $i -lt $emitted; $i++) {
    $label = if ($chunks.Count -eq 1) { $Title } else { "$Title ($($i + 1)/$($chunks.Count))" }
    Write-Host "::notice title=$(ConvertTo-CommandProperty $label)::$(ConvertTo-CommandData $chunks[$i])"
}

# A stated omission beats a silent one: the reader learns the summary was longer than the
# annotation budget and that the artifact holds the rest.
if ($chunks.Count -gt $emitted) {
    $dropped = $chunks.Count - $emitted
    $note = "$dropped of $($chunks.Count) summary chunk(s) exceeded the annotation budget and were not emitted; the full text is in the job summary and the uploaded artifact."
    Write-Host "::notice title=$(ConvertTo-CommandProperty "$Title (truncated)")::$(ConvertTo-CommandData $note)"
}
