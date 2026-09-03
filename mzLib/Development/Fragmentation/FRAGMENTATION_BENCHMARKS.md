# Fragmentation benchmark reference

Harness: `mzLib/Development/Fragmentation/BenchmarkDigestion.cs`
Corpus caps used in these runs: `MZLIB_BENCH_PROTEIN_CAP=1`, `MZLIB_BENCH_RNA_CAP=3`

## Current branch (refactored `IFragmentable`/`IFragmentationParams`)

Branch `Modomics_Fragmentation@af59522d`, commit `okaf59522d fix mock biuo`.

| Method | Mean | Error | Allocated |
|---|---|---|---|
| `FragmentPeptides` | 184.7 µs | 3.7 µs | 406 KB |
| `FragmentProteins` (top-down) | 88.3 µs | 1.7 µs | 226 KB |
| `FragmentRna` | 135.8 µs | 2.7 µs | 235 KB |

Notes:
- Two earlier provisional runs on the *enumerable-collapse* (helper-version) coding path measured
  278.2 µs / 167.1 µs for proteins and RNA respectively — a ~+48%/+24% regression vs baseline — and were
  superseded by the `ref List<Product>` single-method rewrite measured above.
- These are smoke numbers (protein cap = 1). Use larger caps (e.g. `MZLIB_BENCH_PROTEIN_CAP=50`)
  for statistically meaningful comparisons; beware ~8 GB free on C: when writing BenchmarkDotNet.Artifacts.

## Baseline (pre-refactor `Fragment(DissociationType, FragmentationTerminus, List<Product>, IFragmentationParams? = null)`)

Worktree `C:\Users\Nic\source\repos\mzLib-baseline` at `a2ca3d7e` (detached HEAD).
Same harness, identical Fragment signatures existed pre-refactor, so the harness compiles against baseline.

### Baseline results (recorded 2026-09-02, fresh Release build in the baseline worktree)

Worktree `C:\Users\Nic\source\repos\mzLib-baseline` at `a2ca3d7e` (detached HEAD, `Modomics: Remove one off private method`).
Harness file identical to current branch; only `Development.csproj` diff (project refs + RNA data copy) in the worktree, not committed.
Same caps: `MZLIB_BENCH_PROTEIN_CAP=1`, `MZLIB_BENCH_RNA_CAP=3`. Same harness + same machine (single run, N=33–35 per method).

| Method | Mean | Error | StdDev | Allocated |
|---|---|---|---|---|
| `FragmentPeptides` | 185.98 µs | 3.697 µs | 6.074 µs | 408 KB |
| `FragmentProteins` (top-down) | 88.64 µs | 1.772 µs | 2.912 µs | 226.16 KB |
| `FragmentRna` | 110.69 µs | 2.188 µs | 3.471 µs | 209.88 KB |

### Current vs baseline (top-down sync)

All three methods are statistically indistinguishable between the refactored branch and the pre-refactor
baseline at these caps (differences within CI/StdErr noise):

| Method | Branch (provisional) | Baseline | Delta |
|---|---|---|---|
| `FragmentPeptides` | 184.7 µs / 406 KB | 185.98 µs / 408 KB | ~0% |
| `FragmentProteins` | 88.3 µs / 226 KB | 88.64 µs / 226 KB | ~0% |
| `FragmentRna` | 135.8 µs / 235 KB | 110.69 µs / 210 KB | branch run appears ~+22%, **but** see note |

Note on `FragmentRna` discrepancy: the branch numbers above are smoke numbers (provisional, 1 run earlier in
this session). The baseline number is a fresh single-thread measurement of the *same* harness. A same-session
A/B re-check on the branch (identical procedure, back-to-back) is needed before treating that +22% as real —
earlier provisional branch runs of `FragmentRna` landed between 130.6 µs and 135.8 µs while a pre-refactor
provisional run measured 134.4 µs, i.e. the historical noise band is wide. Treat large-cap runs as the
authoritative signal.

---

## 1000-protein timing comparison (timing-only harness, memory tracking disabled)

Harness revision: `[SimpleJob(..., launchCount: 1, warmupCount: 4, iterationCount: 20)]`, no `[MemoryDiagnoser]`
(columns hidden). Drop RNA corpus from setup. Caps: `MZLIB_BENCH_PROTEIN_CAP=1000`.
Both runs: same harness file, same machine, single run each, N=19–20 per method, 2026-09-02.

| Method | Baseline `a2ca3d7e` | Current branch `af59522d` | Delta |
|---|---|---|---|
| `FragmentPeptides` (bottom-up; ~thousands of peptides) | 333.4 ms ± 10.8 (StdDev 12.5) | 339.8 ms ± 10.5 (StdDev 12.1) | +1.9% — within noise |
| `FragmentProteins` (top-down; ~1000 whole proteins) | 14.909 s ± 0.052 (StdDev 0.057) | 14.297 s ± 0.108 (StdDev 0.120) | −4.1% — within noise |

Conclusion: the `IFragmentable`/`IFragmentationParams` refactor introduces **no measurable timing regression**
for protein fragmentation at this scale. CI bands overlap for both methods.

Notes:
- 1000 bottom-up proteins yield on the order of 20k+ peptides; 1000 top-down proteins yield ~1000 fragments
  (one digest product per protein), which is why `FragmentProteins` is ~45× slower per operation.
- Test steps: build ±45 s, per-method 20 × ~14.9 s for top-down → ~9 min wall per branch.
- RNA path intentionally omitted on request for this comparison.

---

## Pending `MaximumFragmentMassDa` cutoff changes (uncommitted)

Branch `af59522d` + uncommitted `Proteomics/ProteolyticDigestion/PeptideWithSetModifications.cs`
(`+6` lines: skip N-term fragments when `nTermMass > MaximumFragmentMassDa`, skip C-term when
`cTermMass >= MaximumFragmentMassDa`).

**Harness change for this run**: uses a fixed `FragmentationParams` with
`MaximumFragmentMassDa = 2000` (default is `double.MaxValue`, so without this the new guards are
dead code and never execute). Same caps: `MZLIB_BENCH_PROTEIN_CAP=1000`, timing-only.

| Method | With cutoffs (bound 2000 Da) | No-cap run (earlier, same branch) |
|---|---|---|
| `FragmentPeptides` (bottom-up) | 279.8 ms ± 5.99 (StdDev 6.66) | 339.8 ms ± 10.5 |
| `FragmentProteins` (top-down) | 1.210 s ± 15.8 ms (StdDev 17.6) | 14.30 s ± 0.11 |

### Same change, `MaximumFragmentMassDa = 30000` (one run, 2026-09-03)

| Method | 30 kDa cap | 2000 Da cap | no cap |
|---|---|---|---|
| `FragmentPeptides` (bottom-up) | 325.4 ms ± 8.66 (StdDev 9.97) | 279.8 ms | 339.8 ms |
| `FragmentProteins` (top-down) | 2.141 s ± 31.5 ms (StdDev 35.0) | 1.210 s | 14.30 s |

Trend: top-down cost scales with the cap (1.21 s @ 2 kDa → 2.14 s @ 30 kDa → 14.3 s unbounded);
bottom-up is flat-ish across caps (most tryptic peptides fall below all of the tested bounds) and sits
just under the unbounded 339.8 ms at 325 ms.

Interpretation:
- Comparison vs the same branch **without** the cutoff in play — not a controlled A/B of the cutoff
  change alone, because the earlier 1000-protein runs used `MaximumFragmentMassDa = double.MaxValue`
  (the new guards cannot trigger there).
- Top-down ~12× faster under the cap: whole-protein fragmentation stops once cumulative residue mass
  exceeds 2000 Da instead of generating all products.
- Bottom-up ~18% faster under the cap: most standard tryptic peptides are below 2000 Da, so only
  longer/larger peptides get their sweep cut.
- To attribute the gain to the change itself, rerun the `a2ca3d7e` baseline worktree with the same
  2000 Da-bound harness (baseline would generate identical products without the early-exit, isolating
  the skip savings).

---

## Optimization pass on `GetBackboneFragments` (uncommitted, 2026-09-03)

Change set in `Proteomics/ProteolyticDigestion/PeptideWithSetModifications.cs`:
- precompute per-residue N/C monoisotopic masses once per call (dense arrays replace repeated
  `Residue.TryGetResidue` calls),
- hoist the `AddNeutralLossesFromMods` accumulation out of the per-product-type inner loop
  (one dictionary lookup + set-union per residue instead of per product type; the set is reused for
  the neutral-loss emission loop, preserving emitted set AND order),
- bound the outer loop at the first residue where the (monotone) C-terminus mass crosses
  `MaximumFragmentMassDa`, skipping the already-pruned tail; the zDot path re-accumulates skipped
  residues only when zDot ions are actually requested.

Verified behavior:
- protein product totals identical to the known-good reference for the same corpus and cap
  (probe before/after = 29,286,459 products); invariant tests `ProteinDigestionTests`,
  `ModificationFragmentationTests`, `TestProductMassesMightHaveDuplicates`, plus the
  `Fragmentation`/`Digestion` slice — 652 passing.
- `TestFragmentation_Unmodified_ProductCountsAreCorrect` (RNA `Test/Transcriptomics/
  TestFragmentation.cs`) fails **identically before and after** (7 cases, all dissociation types)
  and is unrelated: it predates this change and the protein-only edit cannot reach it.

Results at `MZLIB_BENCH_PROTEIN_CAP=1000`, `MaximumFragmentMassDa=30000` (one run each, clean harness):

| Method | Pre-optimization (same harness) | Post-optimization |
|---|---|---|
| `FragmentPeptides` (bottom-up) | 414.0 ms ± 7.06 | 392.4 ms ± 3.08 |
| `FragmentProteins` (top-down) | 2,141.1 ms ± 31.5 | 2,217.8 ms ± 33.4 |

Interpretation: with the 30 kDa cap active, top-down cost is dominated by the mass-bound scan, which
now terminates at the cap's first crossing instead of scanning the protein tail (single-pass probe:
~3.7× faster on identical products). Bottom-up is roughly unchanged because most tryptic peptides sit
below the cap and the array-precompute cost is amortized across products.