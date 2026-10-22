#!/bin/bash
# Start a built Readarr against an empty data folder and check that it serves, enforces auth, accepts a
# command and saves a setting. Each of these has broken without any unit test noticing.
#
#   smoke_test.sh path/to/Readarr
set -euo pipefail

binary=$1
data=$(mktemp -d)
port=8787
base="http://localhost:$port"
log="$data/smoke.log"

cleanup() {
    kill "$pid" 2>/dev/null || true
    wait "$pid" 2>/dev/null || true
}

fail() {
    echo "::error::Smoke test: $1"
    tail -50 "$log" || true
    exit 1
}

Readarr__Server__AllowedHosts=readarr.test "$binary" -nobrowser -data="$data" > "$log" 2>&1 &
pid=$!
trap cleanup EXIT

for _ in $(seq 1 90); do
    curl -fsS "$base/ping" > /dev/null 2>&1 && break
    kill -0 "$pid" 2>/dev/null || fail "Readarr exited during startup"
    sleep 2
done
curl -fsS "$base/ping" > /dev/null || fail "/ping did not answer within 3 minutes"
echo "started"

status=$(curl -s -o /dev/null -w '%{http_code}' -H 'Host: elsewhere.test' "$base/ping")
[ "$status" = 400 ] || fail "a request for a host not in Allowed Hosts returned $status, expected 400"
curl -fsS -H 'Host: readarr.test' "$base/ping" > /dev/null || fail "a request for an allowed host was refused"
echo "allowed hosts enforced"

status=$(curl -s -o /dev/null -w '%{http_code}' "$base/api/v1/system/status")
[ "$status" = 401 ] || fail "an unauthenticated API call returned $status, expected 401"
echo "auth enforced"

key=$(sed -n 's:.*<ApiKey>\(.*\)</ApiKey>.*:\1:p' "$data/config.xml")
[ -n "$key" ] || fail "no API key in $data/config.xml"
api() { curl -fsS -H "X-Api-Key: $key" -H "Content-Type: application/json" "$@"; }

name=$(api -X POST -d '{"name":"CheckHealth"}' "$base/api/v1/command" | jq -r '.name') || fail "posting a command failed"
[ "$name" = "CheckHealth" ] || fail "a posted command came back as '$name'; the body was not read"
echo "command accepted"

config=$(api "$base/api/v1/config/ui")
changed=$(echo "$config" | jq '.firstDayOfWeek = (if .firstDayOfWeek == 1 then 0 else 1 end)')
api -X PUT -d "$changed" "$base/api/v1/config/ui/$(echo "$config" | jq -r '.id')" > /dev/null || fail "saving a setting failed"
expected=$(echo "$changed" | jq '.firstDayOfWeek')
actual=$(api "$base/api/v1/config/ui" | jq '.firstDayOfWeek')
[ "$actual" = "$expected" ] || fail "a saved setting read back as $actual, expected $expected"
echo "setting saved"
