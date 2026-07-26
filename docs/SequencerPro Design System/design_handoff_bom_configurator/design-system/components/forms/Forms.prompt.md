Form controls. Compose `Field` (label/hint/error) around `Input` or `Select`.

```jsx
<Field label="Part number" required hint="Format: WDG-000">
  <Input mono placeholder="WDG-100" />
</Field>
<Field label="Pattern">
  <Select><option>Transform</option><option>Assembly</option><option>Division</option></Select>
</Field>
<Field label="Upper tolerance" error="Must be ≥ nominal">
  <Input invalid suffix="mm" />
</Field>
```

Inputs take `prefix`/`suffix` nodes for icons or units, `mono` for identifiers and measurements, and `invalid` for the red error state. Sizes `sm | md | lg`.
