Data-display primitives. `Card` (surface), `StatCard` (KPI tile), `PortBadge` (domain port chip).

```jsx
<Card title="Widget Manufacturing" subtitle="WDG-MFG-01 · v1" actions={<Button size="sm">Edit</Button>}>
  …
</Card>
<StatCard label="Active jobs" value="12" delta="+3 this week" deltaTone="up" icon={<i className="bi bi-briefcase" />} />
<PortBadge type="Material" direction="in" label="Raw widget" />
<PortBadge type="Parameter" direction="in" label="Spindle RPM" />
<PortBadge type="Characteristic" direction="out" label="Hole Ø" />
<PortBadge type="Condition" direction="out" label="First-article OK" />
```

`PortBadge` encodes the SequencerPro port model: Material=blue, Parameter=gold (X-vars), Characteristic=teal (Y-vars), Condition=orange (pass/fail). Use it on Step cards and process diagrams.
