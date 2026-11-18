-- Kamra product metrics (CAP-12): definitions in metrics.md, tests in MetricsQueryTests.cs.
-- Read-only; returns aggregates only. Run it on the Compose stack (PowerShell), one result per metric:
--
--   Get-Content docs/01_product/metrics.sql -Raw | docker compose exec -T db sh -c 'psql -v ON_ERROR_STOP=1 -U "$POSTGRES_USER" -d "$POSTGRES_DB"'
--
-- The evaluation day is today in Budapest; for another day add: -e PGOPTIONS="-c kamra.as_of=2026-11-16"
-- after "exec -T". Everything before that day's 00:00 (Budapest) counts; the window is the 14 days before it.

-- North Star: saved cookings per active household, for the last two complete Monday-Sunday weeks.
-- "2" is Expiry.SoonExpiringDays; MetricsQueryTests fails if the two differ.
WITH params AS (
    SELECT COALESCE(NULLIF(current_setting('kamra.as_of', true), '')::date,
                    (now() AT TIME ZONE 'Europe/Budapest')::date) AS as_of
),
weeks AS (
    SELECT date_trunc('week', p.as_of::timestamp)::date - back.days AS week_start
    FROM params p CROSS JOIN (VALUES (14), (7)) AS back(days)
),
cookings AS (
    SELECT c."Id", c."HouseholdId", (c."CookedAt" AT TIME ZONE 'Europe/Budapest')::date AS day
    FROM "Cookings" c
),
saved AS (
    SELECT c.day
    FROM cookings c
    WHERE EXISTS (
        SELECT 1
        FROM "StockMovements" m
        WHERE m."CookingId" = c."Id"
          AND m."Delta" < 0
          AND m."ExpiryDateAtMovement" BETWEEN c.day AND c.day + 2
    )
),
activity AS (
    SELECT "HouseholdId", day FROM cookings
    UNION
    SELECT "HouseholdId", ("OccurredAt" AT TIME ZONE 'Europe/Budapest')::date
    FROM "StockMovements"
    WHERE "Reason" = 'Added'
),
counted AS (
    SELECT w.week_start,
           (SELECT count(*) FROM saved s
            WHERE s.day >= w.week_start AND s.day < w.week_start + 7) AS saved_cookings,
           (SELECT count(DISTINCT a."HouseholdId") FROM activity a
            WHERE a.day >= w.week_start AND a.day < w.week_start + 7) AS active_households
    FROM weeks w
)
SELECT 'north_star' AS metric, week_start, saved_cookings, active_households,
       round(saved_cookings::numeric / NULLIF(active_households, 0), 2) AS per_household
FROM counted
ORDER BY week_start;

-- G1: average wasted share of the items whose expiry date falls in the window.
-- Original = entered amount with its corrections; wasted = discarded + still in stock at the end of the window.
WITH params AS (
    SELECT as_of, as_of::timestamp AT TIME ZONE 'Europe/Budapest' AS end_at
    FROM (SELECT COALESCE(NULLIF(current_setting('kamra.as_of', true), '')::date,
                          (now() AT TIME ZONE 'Europe/Budapest')::date) AS as_of) d
),
items AS (
    SELECT i."Id",
           sum(m."Delta") FILTER (WHERE m."Reason" IN ('Added', 'Corrected')) AS original,
           COALESCE(-sum(m."Delta") FILTER (WHERE m."Reason" = 'Discarded'), 0) AS discarded,
           sum(m."Delta") AS remaining
    FROM params p
    JOIN "PantryItems" i ON i."ExpiryDate" >= p.as_of - 14 AND i."ExpiryDate" < p.as_of
    JOIN "StockMovements" m ON m."PantryItemId" = i."Id" AND m."OccurredAt" < p.end_at
    GROUP BY i."Id"
)
SELECT 'g1_waste' AS metric, count(*) AS items,
       round(100 * avg((discarded + remaining) / original), 1) AS wasted_percent
FROM items
WHERE original > 0;

-- G2: share of the households at least 14 days old with 3+ entries in their first 14 days.
-- An entry is one form save or one approved sentence, however many items it holds.
WITH params AS (
    SELECT as_of::timestamp AT TIME ZONE 'Europe/Budapest' AS end_at
    FROM (SELECT COALESCE(NULLIF(current_setting('kamra.as_of', true), '')::date,
                          (now() AT TIME ZONE 'Europe/Budapest')::date) AS as_of) d
),
entries AS (
    SELECT m."HouseholdId", m."OccurredAt" AS saved_at
    FROM "StockMovements" m
    WHERE m."Reason" = 'Added'
      AND NOT EXISTS (SELECT 1 FROM "ItemProposals" ip WHERE ip."PantryItemId" = m."PantryItemId")
    UNION ALL
    SELECT ip."HouseholdId", min(ip."ResolvedAt")
    FROM "ItemProposals" ip
    WHERE ip."State" = 'Approved'
    GROUP BY ip."HouseholdId", ip."SubmittedAt"
),
eligible AS (
    SELECT h."Id",
           (SELECT count(*) FROM entries e
            WHERE e."HouseholdId" = h."Id"
              AND e.saved_at >= h."CreatedAt"
              AND e.saved_at < h."CreatedAt" + interval '14 days') AS entry_count
    FROM "Households" h CROSS JOIN params p
    WHERE h."CreatedAt" + interval '14 days' <= p.end_at
)
SELECT 'g2_retention' AS metric, count(*) AS eligible_households,
       count(*) FILTER (WHERE entry_count >= 3) AS retained_households,
       round(100.0 * count(*) FILTER (WHERE entry_count >= 3) / NULLIF(count(*), 0), 1) AS retained_percent
FROM eligible;

-- G3 (total time): from submitting a sentence to its last approval, for sentences finished in the window.
-- The LLM part of G3 comes from the application log, see metrics.md.
WITH params AS (
    SELECT (as_of - 14)::timestamp AT TIME ZONE 'Europe/Budapest' AS start_at,
           as_of::timestamp AT TIME ZONE 'Europe/Budapest' AS end_at
    FROM (SELECT COALESCE(NULLIF(current_setting('kamra.as_of', true), '')::date,
                          (now() AT TIME ZONE 'Europe/Budapest')::date) AS as_of) d
),
sentences AS (
    SELECT ip."SubmittedAt", max(ip."ResolvedAt") AS finished_at
    FROM "ItemProposals" ip
    WHERE ip."State" = 'Approved'
    GROUP BY ip."HouseholdId", ip."SubmittedAt"
),
timed AS (
    SELECT extract(epoch FROM s.finished_at - s."SubmittedAt")::float8 AS seconds
    FROM sentences s CROSS JOIN params p
    WHERE s.finished_at >= p.start_at AND s.finished_at < p.end_at
)
SELECT 'g3_entry_time' AS metric, count(*) AS sentences,
       round((percentile_cont(0.5) WITHIN GROUP (ORDER BY seconds))::numeric, 1) AS p50_seconds,
       round((percentile_cont(0.9) WITHIN GROUP (ORDER BY seconds))::numeric, 1) AS p90_seconds
FROM timed;
