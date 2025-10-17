# CasanovoMapper

A command-line tool for mapping Casanovo de novo sequencing results to protein databases.

## Overview

CasanovoMapper takes Casanovo mzTab output files and maps the identified peptide sequences to proteins in FASTA databases using in silico proteolytic digestion. This enables protein-level identification from de novo peptide sequences.

## Features

- **Memory-efficient streaming**: Processes large FASTA databases without loading entire files into memory
- **Parallel processing**: Multi-threaded protein digestion and peptide matching for speed
- **Flexible input**: Accepts individual mzTab files or directories containing multiple files
- **Optimized lookups**: Uses binary search with first-character indexing for fast peptide matching
- **Casanovo-compatible**: Supports I→L replacement convention used by Casanovo
- **Customizable digestion**: Configurable protease, missed cleavages, and peptide length filters

## Usage

### Basic Example

```bash
CasanovoMapper \
  --databases /path/to/uniprot_human.fasta,/path/to/contaminants.fasta \
  --casanovo-directory /path/to/casanovo/results \
  --output /path/to/mapped/results
```

### Command-Line Options

| Option | Short | Required | Description |
|--------|-------|----------|-------------|
| `--databases` | `-d` | Yes | Comma-separated list of FASTA database file paths |
| `--casanovo-files` | `-f` | * | Comma-separated list of Casanovo mzTab file paths |
| `--casanovo-directory` | `-i` | * | Directory containing Casanovo mzTab files (*.mztab) |
| `--output` | `-o` | Yes | Output directory for mapped results |
| `--protease` | `-p` | No | Protease used for digestion (default: `trypsin`) |
| `--missed-cleavages` | `-m` | No | Maximum number of missed cleavages (default: `2`) |
| `--min-length` | | No | Minimum peptide length for digestion (default: `7`) |
| `--max-length` | | No | Maximum peptide length for digestion (default: `int.MaxValue`) |
| `--replace-i-with-l` | | No | Replace I with L in sequences (default: `true`) |
| `--chunk-size` | | No | Proteins per chunk for streaming (default: `1000`) |
| `--workers` | | No | Number of parallel worker threads (default: `10`) |
| `--write-filtered-fasta` | | No | Write filtered FASTA files containing only matched proteins (default: `true`) |

\* Either `--casanovo-files` or `--casanovo-directory` must be specified

### Examples

**Map single file with custom digestion:**
```bash
CasanovoMapper \
  -d /databases/human.fasta \
  -f /results/sample1.mztab \
  -o /output \
  -p trypsin \
  -m 2 \
  --min-length 5
```

**Map multiple files from directory:**
```bash
CasanovoMapper \
  -d /db/uniprot.fasta,/db/metagenome.fasta \
  -i /results/casanovo_batch \
  -o /output/mapped \
  --workers 20 \
  --chunk-size 2000
```

**Disable I→L replacement:**
```bash
CasanovoMapper \
  -d /databases/exact.fasta \
  -i /results \
  -o /output \
  --replace-i-with-l false
```

**Skip filtered FASTA output:**
```bash
CasanovoMapper \
  -d /databases/huge.fasta \
  -i /results \
  -o /output \
  --write-filtered-fasta false
```

## Output

The tool creates two types of output files in the output directory:

### 1. Mapped mzTab Files
Files with `_Mapped` suffix containing annotated Casanovo results. Each record is annotated with:
- **Accession**: Protein accession(s) matching the peptide sequence
- **Database**: Database name(s) where the peptide was found

Multiple matches are pipe-delimited (e.g., `P12345|Q67890`).

### 2. Filtered FASTA Files (Optional)
Files with `_Matched.fasta` suffix containing only proteins with at least one matched peptide. This feature:
- **Memory-efficient**: Streams through input FASTAs without loading entire files
- **Standard format**: Writes sequences in 60-character lines
- **I→L reversal**: Automatically reverses I→L replacement if it was applied during reading
- **Per-database**: Creates one filtered FASTA per input database

Disable with `--write-filtered-fasta false` if not needed.

## Performance Tips

- **Workers**: Increase for more CPU cores (e.g., `--workers 20`)
- **Chunk size**: Larger chunks (e.g., `2000-5000`) reduce overhead but use more memory
- **Database order**: Place smaller/more likely databases first for faster early termination

## Requirements

- .NET 8.0 or later
- Input files:
  - Casanovo mzTab files (*.mztab)
  - FASTA protein databases
