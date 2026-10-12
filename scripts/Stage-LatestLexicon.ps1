# Fetch and validate the latest officially published ExamLex-Lexicon DB at build time.
# The published manifest and SHA256 are authoritative. Never use demo data.
$ErrorActionPreference='Stop'
$api='https://api.github.com/repos/KiYouJyo/ExamLex-Lexicon/releases/latest'
$headers=@{'User-Agent'='ExamLex-ReleaseBuilder';'Accept'='application/vnd.github+json'}
$release=Invoke-RestMethod -Uri $api -Headers $headers
if(-not $release.tag_name) { throw 'No published ExamLex-Lexicon release.' }
$manifestAsset=@($release.assets | Where-Object name -EQ 'lexicon-manifest.json') | Select-Object -First 1
$dbAsset=@($release.assets | Where-Object name -EQ 'examlex-lexicon.db') | Select-Object -First 1
if(-not $manifestAsset -or -not $dbAsset) { throw 'Published lexicon release is missing DB or manifest.' }
$dest=Join-Path $PSScriptRoot '..\src\ExamLex\Assets\Lexicon'
New-Item -ItemType Directory -Force -Path $dest | Out-Null
$manifest=Join-Path $dest 'lexicon-manifest.json'
$db=Join-Path $dest 'examlex-lexicon.db'
Invoke-WebRequest -Uri $manifestAsset.browser_download_url -Headers $headers -OutFile $manifest
$meta=Get-Content -LiteralPath $manifest -Raw | ConvertFrom-Json
if($meta.schema_version -ne 1 -or $meta.version -ne $release.tag_name -or
    $meta.filename -ne 'examlex-lexicon.db' -or
    $meta.sha256 -notmatch '^[0-9a-fA-F]{64}$') {
    throw 'Published manifest schema, version or SHA256 is invalid.'
}
Invoke-WebRequest -Uri $dbAsset.browser_download_url -Headers $headers -OutFile $db
$actual=(Get-FileHash $db -Algorithm SHA256).Hash
if($actual -ine $meta.sha256) { throw 'Downloaded lexicon SHA256 does not match release manifest.' }

# Python sqlite3 is installed explicitly in CI. Verify schema and word count.
$verify=@'
import sqlite3, sys, json
db, manifest = sys.argv[1:3]
m = json.load(open(manifest,encoding="utf-8"))
conn = sqlite3.connect("file:"+db.replace("\\","/")+"?mode=ro",uri=True)
assert conn.execute("PRAGMA integrity_check").fetchone()[0]=="ok", "SQLite integrity_check failed"
assert conn.execute("PRAGMA user_version").fetchone()[0]==1, "SQLite schema version incorrect"
required=("entries","senses","pronunciations","inflections","sense_translations",
          "semantic_relations","expressions","examples","example_translations","exam_tags")
tables={row[0] for row in conn.execute("SELECT name FROM sqlite_master WHERE type='table'")}
assert set(required)<=tables, "Dictionary tables are missing"
count=conn.execute("SELECT count(*) FROM entries").fetchone()[0]
assert count==m["entry_count"] and count>0, "Dictionary entry count mismatch"
print(f"Verified {count} headwords, canonical SQLite schema v1")
conn.close()
'@
$verify | python - $db $manifest
if($LASTEXITCODE -ne 0){throw 'Canonical lexicon verification failed.'}
Write-Host "Bundled ExamLex-Lexicon $($release.tag_name), $($meta.entry_count) headwords, SHA256=$actual"
if($env:GITHUB_ENV) {
    "EXAMLEX_BUNDLED_LEXICON=$($release.tag_name)" | Out-File $env:GITHUB_ENV -Encoding utf8 -Append
}
