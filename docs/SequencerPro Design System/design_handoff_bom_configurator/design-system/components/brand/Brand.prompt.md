Brand lockups for SequencerPro: the `Wordmark` text logo and the `NodeMark` symbol.

```jsx
<NodeMark size={48} />
<Wordmark size={32} />
<Wordmark size={24} color="var(--gold)" />
```

- `Wordmark` renders "SequencerPro" with Coda + the Kelly-Slab "e" flourish. Pass `color="var(--gold)"` for the gold-on-dark lockup used in the product nav.
- `NodeMark` is the four-node sequence symbol. It tints from CSS variables, so it inherits theme colors. Pair them horizontally for a full lockup.
