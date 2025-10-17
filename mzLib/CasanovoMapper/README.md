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
| `--missed-cleavages` | `-m` | No | Maximum number of missed cleavages (default: `0`) |
| `--min-length` | | No | Minimum peptide length for digestion (default: `7`) |
| `--max-length` | | No | Maximum peptide length for digestion (default: `int.MaxValue`) |
| `--replace-i-with-l` | | No | Replace I with L in sequences (default: `true`) |
| `--chunk-size` | | No | Proteins per chunk for streaming (default: `1000`) |
| `--workers` | | No | Number of parallel worker threads (default: `10`) |

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

## Output

The tool creates mapped mzTab files in the output directory with `_Mapped` suffix. Each record is annotated with:
- **Accession**: Protein accession(s) matching the peptide sequence
- **Database**: Database name(s) where the peptide was found

Multiple matches are pipe-delimited (e.g., `P12345|Q67890`).

## Performance Tips

- **Workers**: Increase for more CPU cores (e.g., `--workers 20`)
- **Chunk size**: Larger chunks (e.g., `2000-5000`) reduce overhead but use more memory
- **Database order**: Place smaller/more likely databases first for faster early termination

## Requirements

- .NET 8.0 or later
- Input files:
  - Casanovo mzTab files (*.mztab)
  - FASTA protein databases
