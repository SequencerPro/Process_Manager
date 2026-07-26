Buttons for SequencerPro. `Button` is the labelled action; `IconButton` is square and icon-only.

```jsx
<Button>Save process</Button>
<Button variant="secondary">Cancel</Button>
<Button variant="accent" iconLeft={<i className="bi bi-plus-lg" />}>New job</Button>
<Button variant="danger" size="sm">Delete</Button>
<IconButton title="Edit" variant="ghost"><i className="bi bi-pencil" /></IconButton>
```

Variants: `primary` (gold on slate — the brand default), `accent` (orange), `secondary` (outline), `ghost`, `danger` (scrap red), `dark` (slate). Sizes `sm | md | lg`. Icons are passed as nodes (the kits use Bootstrap Icons `<i className="bi bi-…" />`).
