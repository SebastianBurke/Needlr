# Deploying Needlr to a DigitalOcean droplet

This is the v1 deployment shape: a single droplet running the API + Postgres+PostGIS +
Caddy in three Docker containers. Cost runs ~$15-30/mo depending on droplet size and
whether you turn on weekly snapshots. It's the right call until traffic justifies a
managed-services stack.

## What you need before you start

- A registered domain (or subdomain) pointed at the droplet's IPv4 via an A record.
  Caddy provisions Let's Encrypt certificates automatically on first start, but the DNS
  has to resolve before that works.
- A Stripe account in test mode (live keys can wait until you're ready to take real
  bookings). See § Stripe webhook below for how to wire it up.
- A SendGrid account (any plan; the free tier handles 100 emails/day) if you want
  outbound email. Optional — leaving the keys empty disables the email channel.
- VAPID keys for web push (optional). Generate once with
  `npx web-push generate-vapid-keys` and treat them like long-lived secrets.

## 1. Create the droplet

A 2 GB / 1 vCPU regular droplet ($12/mo) handles v1 traffic comfortably. Use Ubuntu
LTS — Docker's apt repo supports it out of the box.

```sh
# After SSHing into the droplet for the first time, harden it:
adduser ops                       # don't run anything as root
usermod -aG sudo ops
ufw allow OpenSSH
ufw allow 80                      # caddy http (let's encrypt challenge + redirect)
ufw allow 443                     # caddy https
ufw enable

# Disable SSH password login. Edit /etc/ssh/sshd_config to set:
#   PasswordAuthentication no
# then `systemctl reload ssh`. Make sure you can ssh in via key first.
```

Optional but recommended:

- `unattended-upgrades` for OS security patches (`apt install unattended-upgrades`).
- DigitalOcean's weekly droplet snapshots (~$2.40/mo for a 2 GB droplet).
- A separate user-managed swap file if you ever expect memory spikes.

## 2. Install Docker

```sh
curl -fsSL https://get.docker.com | sudo sh
sudo usermod -aG docker ops       # log out and back in for this to take effect
```

Confirm with `docker --version` and `docker compose version` (Compose v2 ships with
modern Docker; no separate install needed).

## 3. Clone the repo

```sh
sudo mkdir -p /srv/needlr
sudo chown ops:ops /srv/needlr
cd /srv/needlr
git clone https://github.com/SebastianBurke/Needlr.git .
```

The `deploy/` folder has everything we need: the Compose file, the Caddy config, and
the env template.

## 4. Configure secrets

```sh
cp deploy/.env.example deploy/.env
chmod 600 deploy/.env             # only ops can read it
$EDITOR deploy/.env
```

Fill in every required value:

- `NEEDLR_DOMAIN` — the FQDN you pointed at this droplet
- `NEEDLR_TLS_EMAIL` — Let's Encrypt sends expiry notifications here
- `POSTGRES_PASSWORD` — `openssl rand -base64 32`
- `JWT_SIGNING_KEY` — `openssl rand -base64 48`

Stripe + SendGrid + VAPID can be left blank for the first boot; the app starts cleanly
without them and the booking-payment / email / push features become available the
moment you fill the values in and `docker compose restart api`.

## 5. Build and start

```sh
docker compose --env-file deploy/.env -f deploy/compose.yaml build
docker compose --env-file deploy/.env -f deploy/compose.yaml up -d
docker compose --env-file deploy/.env -f deploy/compose.yaml logs -f api
```

The first start runs EF migrations and seeds the Montréal jurisdiction + the 32
canonical tattoo styles. Watch the logs for `DataSeeder completed.` — that's the
signal that the schema is up.

Caddy provisions a certificate on its first request to `https://NEEDLR_DOMAIN`. If
DNS isn't propagated yet, requests return a self-signed cert until it is. Wait a few
minutes, then visit the domain — you should see the WASM client load.

## 6. Promote your account to admin

The API has no admin-creation endpoint by design. Register an account through the
public flow, then set the role in the database:

```sh
docker compose --env-file deploy/.env -f deploy/compose.yaml \
  exec postgres psql -U $POSTGRES_USER -d $POSTGRES_DB \
  -c "INSERT INTO \"AspNetUserRoles\" (\"UserId\", \"RoleId\")
      SELECT u.\"Id\", r.\"Id\"
      FROM \"AspNetUsers\" u, \"AspNetRoles\" r
      WHERE u.\"Email\" = 'you@example.com' AND r.\"Name\" = 'Admin'
      ON CONFLICT DO NOTHING;"
```

Sign out and back in to pick up the new role claim. The Hangfire dashboard
(`/hangfire`) and admin tooling (`/admin`) are now reachable.

## 7. Stripe webhook

Once Stripe keys are in `.env` and the api container is restarted:

1. In the Stripe Dashboard → Developers → Webhooks → "Add endpoint"
2. Endpoint URL: `https://NEEDLR_DOMAIN/api/stripe/webhook`
3. Events to send (minimum):
   - `payment_intent.succeeded`
   - `payment_intent.payment_failed`
   - `account.updated`
4. Copy the signing secret from the dashboard into `STRIPE_WEBHOOK_SECRET` in `.env`,
   then `docker compose restart api`.

Stripe's "Send test event" button on the webhook page is the quickest end-to-end
verification — a successful `payment_intent.succeeded` event flows through, opens a
booking thread, and shows up in the `Hangfire` dashboard's processing logs.

## Updating

```sh
cd /srv/needlr
git pull
docker compose --env-file deploy/.env -f deploy/compose.yaml build api
docker compose --env-file deploy/.env -f deploy/compose.yaml up -d api
```

Migrations run on the api container's startup, so the deploy is one-step. If a
migration is large, expect the api container to take a few seconds longer to become
healthy on the next start.

## Backups

The droplet snapshot covers the full disk including the postgres + uploads volumes,
which is enough recovery for v1. For point-in-time logical backups:

```sh
# Cron entry in /etc/cron.d/needlr-backup, runs nightly at 03:30 local time:
30 3 * * * ops cd /srv/needlr && \
  docker compose --env-file deploy/.env -f deploy/compose.yaml \
    exec -T postgres pg_dump -U $POSTGRES_USER $POSTGRES_DB \
    | gzip > /srv/backups/needlr-$(date +\%Y\%m\%d).sql.gz
```

Rotate older backups (`find /srv/backups -mtime +14 -delete` weekly) and ship the
folder to off-droplet storage (Spaces, S3, B2) before treating this as durable.

## Common operational tasks

| Task | Command |
|---|---|
| Tail the api logs | `docker compose -f deploy/compose.yaml logs -f api` |
| psql shell against the prod DB | `docker compose -f deploy/compose.yaml exec postgres psql -U needlr -d needlr` |
| Restart the api after env change | `docker compose --env-file deploy/.env -f deploy/compose.yaml up -d api` |
| Force a full rebuild | `docker compose -f deploy/compose.yaml build --no-cache api` |
| Inspect uploads volume | `docker compose -f deploy/compose.yaml exec api ls /var/needlr/uploads` |
| Check Caddy's cert status | `docker compose -f deploy/compose.yaml exec caddy caddy list-certificates` |

## When this stops working

The single-droplet model holds until one of:

- **Disk fills.** Uploads grow over time; the booking purge job clears blobs 1 year
  after terminal state, but until then expect ~5 MB per booking. Migrate uploads to
  Cloudflare R2 or DO Spaces before the disk gets tight (the `R2ImageStorage` stub in
  `Needlr.Infrastructure.Storage` is the implementation point).
- **DB performance degrades.** Discovery queries with seeded data hit the existing
  PostGIS GiST index efficiently, but if you add complex reporting on top, move
  Postgres to a managed instance (DO Managed DB, Neon) before fighting the box.
- **You need zero-downtime deploys.** A single api container drops connections during
  restart. The fix is two api replicas behind Caddy with health-checked rotation, or a
  managed app host (Fly, Render) that does this for you.
- **You need redundancy.** A single droplet is a single point of failure. v2-shape
  deploy is when downtime cost outweighs the ops complexity of a multi-instance
  topology.
