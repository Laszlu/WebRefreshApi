# Design System

Modern, clean, easy to navigate. Every design decision should make content easier to find and read, not call attention to itself.

## Design principles

- Clarity over decoration. No unnecessary visual flourishes, gradients, animations, or ornamental elements.
- Generous whitespace over dense layouts. When in doubt, add space rather than a visual divider.
- Content hierarchy communicated through size and weight, not color or decoration.
- Every page must work identically well on a phone and a desktop monitor — this is not optional or a secondary concern.

## Colors

--color-bg: #ffffff
--color-bg-subtle: #f7f7f8
--color-text: #1a1a1a
--color-text-muted: #5f5f66
--color-border: #e2e2e5
--color-primary: #2952e3
--color-primary-hover: #1f3fb8
--color-accent: #f7f7f8

Use `--color-primary` only for interactive elements (links, buttons, focus states) and small accents. Never use it as a large background fill. Text on `--color-bg` must use `--color-text`; never place `--color-text-muted` on anything but `--color-bg` or `--color-bg-subtle`.

## Typography

--font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif

--text-sm: 0.875rem     (14px — captions, metadata)
--text-base: 1rem       (16px — body text)
--text-lg: 1.25rem      (20px — section intros)
--text-xl: 1.75rem      (28px — h2)
--text-2xl: 2.5rem      (40px — h1)

--line-height-body: 1.6
--line-height-heading: 1.2

--font-weight-normal: 400
--font-weight-medium: 500
--font-weight-bold: 700

Headings use `--font-weight-bold`. Body text uses `--font-weight-normal`. Never use more than one font family.

## Spacing scale

Use only these values for margin, padding, and gap — no arbitrary pixel values.

--space-1: 0.5rem   (8px)
--space-2: 1rem      (16px)
--space-3: 1.5rem    (24px)
--space-4: 2.5rem    (40px)
--space-5: 4rem       (64px)
--space-6: 6rem       (96px)

Use `--space-5` or `--space-6` between major page sections. Use `--space-2` or `--space-3` between related elements within a section.

## Layout

- Max content width: 1200px, centered, with `--space-3` side padding on mobile.
- Single-column layout on mobile (below 768px). Multi-column only where the design spec's component patterns explicitly call for it (e.g. feature grids), and only above 768px.
- Breakpoints:
    - Mobile: up to 767px
    - Desktop: 768px and up
- Navigation: horizontal nav bar on desktop. On mobile, collapse into a toggled menu (hamburger icon), using the mobile nav toggle JS behavior.

## Components

**Header / nav**
Logo or site title on the left, nav links on the right (desktop) or behind a toggle (mobile). Sticky to the top of the viewport. Background `--color-bg`, bottom border `--color-border`.

**Hero section**
Large heading (`--text-2xl`), optional supporting text below it (`--text-lg`, `--color-text-muted`), optional single image. Generous vertical padding (`--space-6`).

**Content section**
Heading (`--text-xl`) followed by body text (`--text-base`, `--line-height-body`). Standard section padding (`--space-5` top and bottom).

**Feature / card grid**
Used when a section's content is naturally a list of items (services, features, team members). Cards in a responsive grid: single column on mobile, 2–3 columns on desktop depending on item count. Each card: subtle background (`--color-bg-subtle`), `--space-3` internal padding, no heavy borders or shadows — a background color change is enough to separate a card from the page.

**Buttons / links as CTAs**
Solid background `--color-primary`, white text, `--space-1` vertical / `--space-2` horizontal padding, no border-radius beyond a subtle 4px. Hover state: `--color-primary-hover`. Focus state: visible outline, never removed.

**Footer**
`--color-bg-subtle` background, `--space-4` padding, smaller text (`--text-sm`, `--color-text-muted`). Repeats the same nav links as the header, plus any contact/legal content provided.

## Accessibility baseline

- Minimum contrast ratio 4.5:1 for body text against its background (the palette above satisfies this).
- Every interactive element has a visible focus state — do not remove default focus outlines without replacing them.
- One `h1` per page. Heading levels must not skip (no `h3` directly after `h1`).
- All images require `alt` text (provided by the SiteSpec) — never left empty unless the source explicitly marks an image as decorative.