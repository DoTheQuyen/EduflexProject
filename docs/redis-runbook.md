# Redis Runbook

Operational reference for the Redis instance backing eduflex's two Redis-dependent
features: the feedback/course-promotion cache-aside layer, and the
`eduflex:notifications` pub/sub channel feeding the SignalR notification bell.

## What's running

- Container: `redis` (image `redis:7-alpine`), defined in `docker-compose.yml`.
- Port: `6379`, mapped straight through to the host.
- No persistence volume configured — data is memory-only and is lost on
  container restart. Acceptable today because nothing stored in Redis is the
  system of record: cached data rebuilds itself from MongoDB on the next
  cache-aside miss, and pub/sub messages are inherently transient (see
  "Known limitations" below).
- No password/TLS configured — fine for local dev, **must** be added before
  any shared/production deployment (`requirepass` + a real connection string
  with credentials).

## Health check

```bash
docker exec -it redis redis-cli ping
```

Expect `PONG`. If this hangs or errors, the container isn't responding —
check `docker ps` to confirm it's even running before anything else.

## Restart

```bash
docker restart redis
```

Since there's no persistence volume, this clears all cached keys and drops
any in-flight pub/sub subscriptions. The app's `IConnectionMultiplexer`
(registered in `Program.cs`) auto-reconnects on its own — watch the app's
own log output for:

```
Redis connection failed on <endpoint>: <failure type>
...
Redis connection restored on <endpoint>
```

If "restored" never appears after a restart, the app's connection string
(`ConnectionStrings:RedisConnection`) or Docker networking is the next thing
to check, not Redis itself.

## Inspecting state

```bash
docker exec -it redis redis-cli
127.0.0.1:6379> dbsize                 # total key count
127.0.0.1:6379> keys *                 # list all keys (fine at this scale; avoid on a large prod dataset — use SCAN instead)
127.0.0.1:6379> get feedback:latest
127.0.0.1:6379> get coursepromotions:featured
127.0.0.1:6379> ttl feedback:latest    # seconds until this cache entry expires
127.0.0.1:6379> info memory            # memory usage stats
```

Known cache keys used by this app:

| Key | Populated by | Busted by |
|---|---|---|
| `feedback:latest` | `FeedbackService.GetLatestFeedback` on a cache miss | `CreateFeedback` / `DeleteFeedback` |
| `coursepromotions:featured` | `CoursePromotionService.GetFeaturedActiveCoursePromotions` on a cache miss | Create/Update/Delete on course promotions |

Both expire after 10 minutes even without an explicit bust, as a safety net.

## Pub/sub channel

Single shared channel: `eduflex:notifications`. Any service publishes a
`NotificationMessage` (`Module`, `EntityId`, `Summary`, `TargetRole`) as JSON;
`NotificationListener` (a hosted background service in the API) is the only
subscriber, and fans each message out to SignalR clients grouped by role.

To manually test the channel end-to-end without touching the app's business
logic:

```
127.0.0.1:6379> publish eduflex:notifications "{\"Module\":\"Test\",\"EntityId\":\"1\",\"Summary\":\"manual test\",\"TargetRole\":\"Staff\"}"
```

The reply is the number of subscribers that received it — `0` means the API
isn't running or its listener failed to subscribe (check the app's log for
`"Subscribed to Redis channel eduflex:notifications"` at startup).

## Known limitations / failure modes

- **Redis pub/sub does not persist messages.** If `NotificationListener`
  isn't connected at the moment something publishes, that notification is
  gone forever — no replay, no queue. If notifications start "going
  missing," check whether the API was mid-restart when the triggering event
  happened, before assuming a bug.
- **Cache miss storm**: if the cache is cleared (restart, or every key
  expiring around the same time) while traffic is high, many concurrent
  requests can all miss the cache simultaneously and hit MongoDB at once.
  At eduflex's current scale this is a non-issue, but it's the standard
  reason larger systems add request coalescing/locking around cache
  repopulation — worth knowing as a concept even though it isn't implemented
  here.
- **No auth on the connection.** Anyone who can reach port 6379 can read/
  write/flush this Redis instance. Only acceptable because it's local dev
  today.
