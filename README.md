# C# Redis-Compatible Server

This repository contains my solution to the
["Build Your Own Redis" challenge](https://codecrafters.io/challenges/redis).

Built a Redis-compatible server using a single-threaded event loop for deterministic execution.

Implemented RESP parsing, command routing, blocking commands, replication, and pub-sub under concurrent load.

## What I have done

- Implemented a TCP server host in `src/Hosting/RedisServerHost.cs` that:
  - accepts concurrent client connections,
  - reads initial RDB state on startup,
  - supports replica handshake when started with `--replicaof`.
- Implemented RESP parsing in `src/Resp/RespParser.cs` with support for:
  - simple strings, errors, integers, bulk strings, and arrays,
  - partial-frame handling via consumed-length tracking.
- Built a deterministic command execution model in `src/Resp/CommandEventLoop.cs`:
  - command envelopes are queued through a channel,
  - all command + cache work is executed on a single exclusive scheduler lane.
- Added command routing in `src/Resp/RespExecutor.cs` for:
  - normal command mode,
  - `MULTI`/`EXEC` transaction mode,
  - pub-sub constrained mode.
- Implemented authentication and ACL basics:
  - `AUTH`,
  - `ACL WHOAMI`,
  - `ACL GETUSER`,
  - `ACL SETUSER`.
- Implemented core Redis-style command groups:
  - strings: `GET`, `SET`, `INCR`,
  - general: `PING`, `ECHO`, `KEYS`, `TYPE`,
  - lists: `LPUSH`, `RPUSH`, `LPOP`, `LLEN`, `LRANGE`, `BLPOP`,
  - streams: `XADD`, `XRANGE`, `XREAD` (including blocking mode),
  - sorted sets: `ZADD`, `ZRANK`, `ZRANGE`, `ZCARD`, `ZSCORE`, `ZREM`,
  - geospatial: `GEOADD`, `GEOPOS`, `GEODIST`, `GEOSEARCH`.
- Implemented pub-sub support:
  - `SUBSCRIBE`, `PUBLISH`, `UNSUBSCRIBE`,
  - per-channel subscriber tracking and fan-out delivery.
- Implemented replication flow:
  - replica handshake with `PING`, `REPLCONF`, `PSYNC`,
  - `FULLRESYNC` response and RDB payload transfer,
  - write-command propagation to replicas,
  - replica ACK tracking and `WAIT` semantics.
- Added server/runtime options:
  - `--port`,
  - `--replicaof`,
  - `--dir`,
  - `--dbfilename`.

## Architecture choices

- Used an event-loop owner-lane model (`LoopOwnerContext`) so cache operations are serialized and deterministic, even with many client connections.
- Kept networking concurrency separate from command-state mutation: client handlers parse input concurrently, but execution is centralized through the command loop.
- Used keyed DI registration for commands so dispatch stays simple and adding new commands is mostly a registration + command class change.
- Modelled blocking operations (`BLPOP`, blocking `XREAD`, `WAIT`) with explicit waiter objects and `TaskCompletionSource` signaling for predictable timeout/cancellation behavior.
- Separated replication concerns into dedicated components (`HandshakeCoordinator`, `ReplicaConnectionRegistry`) to isolate handshake, propagation, and ACK accounting.

## What I have learnt

- A single-writer execution lane dramatically reduces race conditions in stateful protocol servers.
- RESP parsing becomes much easier to reason about when parser output includes exact consumed bytes, not just parsed values.
- Blocking commands are primarily a waiter lifecycle problem: register correctly, signal once, and always clean up on timeout/disconnect.
- Replication correctness depends on explicit offset accounting and acknowledgement flow, not just forwarding writes.
- Distinct command modes (normal, transaction, pub-sub) make server behavior clearer and avoid subtle command-context bugs.

## Run locally

Ensure .NET 9 is installed.

Run:

```sh
./your_program.sh
```

Optional examples:

```sh
# Run on a custom port
./your_program.sh --port 6380

# Start as a replica of localhost:6379
./your_program.sh --port 6381 --replicaof "localhost 6379"
```
