# ClipBeam Protocol v1

**Purpose:**  
Defines the gRPC contract used for real-time clipboard synchronization between ClipBeam clients (Windows, Android, etc).

## Structure

- `clipbeam.proto` — main protobuf schema.
- `package clipbeam.v1` — versioned package name (increment when breaking changes occur).

## Design Overview

1. **Handshake Phase** — Devices exchange `Hello` / `HelloAck` to negotiate capabilities and authentication.
2. **Sync Stream** — Both peers enter a bidirectional streaming session exchanging `Envelope` messages.
3. **Data Transfer** — Clipboard content is split into `DataStart` and `DataBody` chunks.
4. **Flow Control** — Receivers send `Ack` / `Nack` to control throughput and retransmissions.
5. **Keepalive** — `Ping` / `Pong` ensure connection liveness.

## Compatibility Rules

- Fields must **never be renumbered** or removed.
- Add new fields with new numbers; older clients will safely ignore them.
- Use the reserved range `1000–1999` for future internal extensions.
- Breaking changes → increment `package clipbeam.v2`.

## Code Generation

```bash
# .NET
dotnet build windows/src/ClipBeam.Proto/ClipBeam.Proto.csproj

# Android
./gradlew :android:transport-grpc:generateDebugProto
