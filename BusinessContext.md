# Kasheif (كاشف) — Business & Domain Context

> Sourced from the live site (kasheif.com) and the domain model reviewed in the project codebase.
> Kasheif is the live product name; "HalalStocks" is the internal codebase/namespace name.

## 1. What Kasheif Is

Kasheif ("كاشف" — literally "revealer/discoverer") is an Arabic-language, Shariah-compliance screening
platform for stocks listed on the Egyptian Exchange (EGX). Its tagline positions it around portfolio
integrity: **"نقاء محفظتك، يبدأ من هنا"** ("Your portfolio's purity starts here"). The core value
proposition is letting Egyptian investors quickly check whether a given stock is permissible to hold
under Islamic finance principles, and if not, by how much its income needs to be purified.

The site carries an explicit disclaimer that its content is informational, not financial advice —
consistent with the regulatory sensitivity of publishing investment-adjacent guidance.

## 2. Shariah Classification — the core domain concept

Every stock on the platform is classified into one of several Shariah-compliance categories. The
live site's stock screener surfaces this as filterable status categories, including (at minimum):

- **Compliant** — permissible to hold as-is.
- **Non-compliant** — not permissible.
- **Awaiting financial statements** — not yet classifiable, pending the company's latest disclosures.
- **Disclosure blocked** — the company's Shariah position cannot currently be published (regulatory or
  data-availability reasons).

Underlying this, the platform also tracks a more granular **purity grade** per stock — because a stock
can be "compliant with minor purification required" rather than a flat yes/no. This is why the
purification calculator (below) exists as a companion tool: even a permissible-to-hold stock can carry
a small percentage of impermissible income (e.g. incidental interest income) that a Muslim investor is
expected to donate away rather than keep.

Stocks are organized by sector/category and can belong to one or more market indices (e.g. EGX 30,
Shariah-specific indices), which the site's screening/filter UI exposes to users.

## 3. Core User-Facing Features

### Stock screening (the flagship feature)
Browse and filter EGX-listed stocks by Shariah status, sector, and index. This is the free, public,
always-available core of the product — the reason most visitors come to the site.

### Purification calculator
Lets an investor enter their buy/sell activity for a stock and calculates the portion of profit that
must be purified (donated) due to impermissible income exposure, producing a branded, Arabic
PDF report they can keep for their own records. A companion "saved calculations" feature lets logged-in
users persist and revisit past calculation sessions rather than recreating them each time.

### Zakat calculator
A separate religious-obligation calculator: zakat owed on an investment portfolio, referencing the
current gold price to determine the nisab (minimum wealth threshold for zakat to apply).

### Capital purification calculator
The third calculator, addressing a different obligation from the one above: not the profit an investor
realised, but the share of the *company's own capital* standing behind their holding that is tied up in
interest-bearing activity. The investor picks a stock and enters how many shares they own; from the
company's total shares and total assets the tool derives their ownership percentage, their slice of the
company's capital, and — applying the stock's non-Shariah investment ratio — the amount to purify.
Results are saved per user into two lists, unpaid and paid, so an investor can work through their
obligations over time and mark each one settled. This depends on two company figures (total shares,
total assets) that an admin must enter per stock; until they do, the calculator tells the user the data
isn't available rather than guessing.

The three calculators are presented together in the site header under "أدوات التطهير", and all five
user-facing tools are surfaced as a card grid ("أدوات كاشف") on the home page.

### Managed portfolio request (lead generation)
A form where a user expresses interest in a professionally managed, Shariah-compliant portfolio —
capital amount, investment horizon, purity preference, preferred contact method. This routes into an
internal admin pipeline (new → under review → contacted → active client / rejected), functioning as
Kasheif's lead-gen funnel into what appears to be a paid advisory/portfolio-management side of the
business, distinct from the free screening tool.

### Stock recommendations (gated)
An admin-curated list of actionable stock calls — trend, support/resistance levels, stop-loss, a
recommendation action (trade / buy the dip / hold / expired), and purity notes — available only to
approved logged-in users. Access requires an explicit request-and-admin-approval step, positioning this
as a higher-trust, higher-value tier of content compared to the freely browsable stock screener. This
mirrors the same "request → admin review → approved access" pattern as the portfolio request feature,
suggesting a consistent business model of gating premium engagement behind manual admin vetting rather
than self-serve payment (at least in the current version).

The *content* stays gated, but the feature is no longer invisible to logged-out visitors: التوصيات now
appears in the site header for everyone, and the page — along with its two sibling tabs, المستشار الآلي
and المحفظة الفعلية — shows a public teaser explaining the service with a prompt to register or sign
in. This makes the paid/vetted tier discoverable and indexable, rather than a dead end that silently
redirected anonymous visitors to a login form.

### Articles / educational content
An admin-published articles section — market commentary, educational content on Shariah investing
concepts — serving both SEO/content-marketing purposes and user education.

## 4. Contact & Distribution Channels

The live site surfaces WhatsApp and Telegram as direct contact/community channels (alongside the
managed-portfolio request form as a more formal lead-capture path) — consistent with common
Egyptian/MENA consumer product distribution patterns where messaging-app channels often outperform
email for engagement.

## 5. Target Audience

Individual retail investors in the Egyptian stock market who want their investments to align with
Islamic finance principles — ranging from casual investors who just want a quick compliance check on a
stock they're considering, to more engaged users who calculate their own purification/zakat obligations,
to a smaller segment interested in either hands-off managed portfolios or curated recommendations (the
two "gated"/higher-touch offerings).

## 6. Business Model Signals (inferred from feature structure, not stated explicitly)

- **Free tier:** stock screening, articles — the acquisition/SEO layer.
- **Self-serve utility tier:** purification and zakat calculators — free but requires login for the
  persisted "saved sessions" variant, functioning as a soft signup incentive.
- **Lead-generation tier:** managed portfolio request — funnels warm leads to what is presumably a
  human-advised, possibly fee-based portfolio management service outside the app itself.
- **Gated content tier:** stock recommendations — requires explicit admin approval per user rather than
  self-serve payment, suggesting either a manually-vetted relationship-based offering (e.g. tied to
  being a portfolio client) or a feature still evolving toward a future paid-subscription model.

## 7. Product Cadence

The recommendations module was added as a discrete, additive feature on top of an already-live,
already-deployed product — consistent with an iterative, client-driven feature pipeline rather than
large periodic releases. Prior additions (per the migration history) followed the same pattern:
financial reviews, portfolio requests, zakat records, calculation sessions, and site settings were each
added incrementally over recent months. The capital purification calculator is the most recent
addition and followed the same shape: two admin-entered fields on the stock record first, then the
calculator built on top of them.

Two navigation/discoverability changes shipped alongside it, and both point the same way — the product
is being tuned to surface what already exists rather than only adding new capability: the tools were
gathered into one header menu and a home-page card grid, and the gated recommendations tier was given a
public teaser instead of an anonymous redirect.
