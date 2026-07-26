Feedback primitives. `StatusBadge` is the lifecycle/QA pill; `Tag` is the code/metadata chip.

```jsx
<StatusBadge tone="pass" />
<StatusBadge tone="active">In Progress</StatusBadge>
<StatusBadge tone="hold" />  <StatusBadge tone="fail" />
<Tag color="blue">WDG-100</Tag>
<Tag color="teal" removable onRemove={fn}>Grade A</Tag>
```

`StatusBadge` tones: `pass` (teal), `active` (blue), `hold` (gold), `rework` (orange), `fail` (red), `pending`/`done` (neutral). Each has a sensible default label so `<StatusBadge tone="pass" />` already reads "Pass". `Tag` defaults to monospace because it usually holds an identifier.
