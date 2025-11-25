# Welcome to mzLib

## Overview

**mzLib** is a comprehensive, open-source .NET library for mass spectrometry data analysis and computational proteomics/transcriptomics. Built on .NET 8, mzLib provides a unified framework for working with mass spectrometry data, biological sequences, chemical formulas, and computational workflows for omics research.

### Key Capabilities

- **Mass Spectrometry Data Processing**: Read, write, and analyze MS data in multiple formats (mzML, Thermo RAW, etc.)
- **Omics Analysis**: Protein and RNA sequence analysis with digestion, modification, and fragmentation support
- **Chemical Calculations**: Accurate mass and isotope distribution calculations
- **Spectral Analysis**: Deconvolution, averaging, and matching algorithms
- **Retention Time Prediction**: Machine learning-based chromatographic retention time prediction
- **Database Management**: Load and process protein/RNA databases with decoy generation
- **Quantification**: Label-free quantification via FlashLFQ

### Quick Start

```bash
# Install via NuGet
dotnet add package mzLib
```

```csharp
using MassSpectrometry;
using Proteomics;
using Chemistry;

// Load MS data
var msDataFile = MsDataFileReader.GetDataFile("sample.mzML");
var scans = msDataFile.GetAllScansList();

// Work with proteins
var protein = new Protein("PEPTIDESEQUENCE", "P12345");
var peptides = protein.Digest(new DigestionParams("trypsin"), fixedMods, variableMods);

// Calculate masses
var formula = ChemicalFormula.ParseFormula("C6H12O6");
double mass = formula.MonoisotopicMass;
```

## Documentation Structure

### Core Functionality

#### Chemistry & Fundamentals
- **[Chemistry](https://github.com/smith-chem-wisc/mzLib/wiki/Chemistry)** - Chemical formula operations, mass calculations, periodic table, isotope distributions

#### Mass Spectrometry
- **[Mass Spectrometry](https://github.com/smith-chem-wisc/mzLib/wiki/Mass-Spectrometry)** - MS data structures, scan operations, spectral processing
- **[Spectral Deconvolution](https://github.com/smith-chem-wisc/mzLib/wiki/Spectral-Deconvolution-(Decharging-and-Deisotoping))** - Charge state deconvolution and isotope deisotoping
- **[Spectral Averaging](https://github.com/smith-chem-wisc/mzLib/wiki/Spectral-Averaging)** - Averaging and combining multiple MS scans
- **[Retention Time Prediction](https://github.com/smith-chem-wisc/mzLib/wiki/Retention-Time-Prediction)** - SSRCalc-based chromatographic retention time prediction

### File I/O
- **[MS Data File Reading](https://github.com/smith-chem-wisc/mzLib/wiki/File-Reading:-Mass-Spec)** - Read mzML, Thermo RAW, and other MS formats
- **[Result File Reading](https://github.com/smith-chem-wisc/mzLib/wiki/File-Reading:-Result-Formats)** - Parse search engine results (pepXML, mzIdentML, etc.)
- **[Sequence Database File Reading](https://github.com/smith-chem-wisc/mzLib/wiki/File-Reading:-Sequence-Databases)** - Load protein/RNA databases (FASTA, XML, etc.)

### Omics Framework

#### Foundation
- **[Omics: Base Foundation](https://github.com/smith-chem-wisc/mzLib/wiki/Omics:-Base-Foundation)** - Core interfaces (`IBioPolymer`, `IBioPolymerWithSetMods`) and unified architecture

#### Domain-Specific
- **[Proteomics](https://github.com/smith-chem-wisc/mzLib/wiki/Proteomics)** - Protein analysis, peptide generation, PTMs, SILAC labeling
- **[Transcriptomics](https://github.com/smith-chem-wisc/mzLib/wiki/Transcriptomics)** - RNA analysis, oligonucleotide generation, epitranscriptomic modifications

#### Cross-Cutting Features
- **[Omics: Modifications](https://github.com/smith-chem-wisc/mzLib/wiki/Omics:-Modifications)** - Post-translational modifications (PTMs) and RNA modifications with motif matching
- **[Omics: Digestion](https://github.com/smith-chem-wisc/mzLib/wiki/Omics:-Digestion)** - Enzymatic digestion framework for proteases and RNases
- **[Omics: Fragmentation](https://github.com/smith-chem-wisc/mzLib/wiki/Omics:-Fragmentation)** - MS/MS fragmentation for peptides and oligonucleotides
- **[Omics: Decoy Generation](https://github.com/smith-chem-wisc/mzLib/wiki/Omics:-Decoy-Generation)** - Generate decoy sequences for FDR calculation

## Common Workflows

### Workflow 1: Protein Identification from MS Data

```csharp
using MassSpectrometry;
using Proteomics;
using Proteomics.ProteolyticDigestion;
using UsefulProteomicsDatabases;

// 1. Load MS data
var msDataFile = MsDataFileReader.GetDataFile("sample.mzML");
var ms2Scans = msDataFile.GetAllScansList().Where(s => s.MsnOrder == 2).ToList();

// 2. Load protein database
var proteins = ProteinDbLoader.LoadProteinFasta(
    "database.fasta",
    generateTargets: true,
    decoyType: DecoyType.Reverse,
    isContaminant: false,
    out var errors
);

// 3. Digest proteins
var digestionParams = new DigestionParams(
    protease: "trypsin",
    maxMissedCleavages: 2,
    minPeptideLength: 7,
    maxPeptideLength: 30
);

var theoreticalPeptides = proteins
    .SelectMany(p => p.Digest(digestionParams, fixedMods, variableMods))
    .ToList();

// 4. Match spectra to peptides
foreach (var scan in ms2Scans)
{
    var candidates = theoreticalPeptides
        .Where(p => Math.Abs(p.MonoisotopicMass - scan.PrecursorMass) < tolerance)
        .ToList();
    
    foreach (var peptide in candidates)
    {
        var products = new List<Product>();
        peptide.Fragment(DissociationType.HCD, FragmentationTerminus.Both, products);
        
        // Score matches...
    }
}
```

### Workflow 2: RNA Modification Analysis

```csharp
using Transcriptomics;
using Transcriptomics.Digestion;

// 1. Load RNA sequence
var rna = new RNA(
    sequence: "AUGCCGUACGAU",
    accession: "RNA001",
    name: "tRNA-Ala"
);

// 2. Define RNA modifications
var m6A = new Modification(
    _originalId: "m6A",
    _target: ModificationMotif.GetMotif("A"),
    _chemicalFormula: ChemicalFormula.ParseFormula("CH2")
);

// 3. Digest with RNase
var digestionParams = new RnaDigestionParams(
    rnase: "RNase T1",
    maxMissedCleavages: 1,
    minOligoLength: 3,
    maxOligoLength: 20
);

var oligos = rna.Digest(
    digestionParams,
    new List<Modification>(),
    new List<Modification> { m6A }
).ToList();

// 4. Analyze modification sites
var modifiedOligos = oligos.Where(o => o.NumMods > 0).ToList();
Console.WriteLine($"Found {modifiedOligos.Count} modified oligonucleotides");
```

### Workflow 3: Spectral Processing

```csharp
using MassSpectrometry;
using MassSpectrometry.Deconvolution;
using SpectralAveraging;

// 1. Load MS data
var msDataFile = MsDataFileReader.GetDataFile("sample.mzML");

// 2. Average similar scans
var scansToAverage = msDataFile.GetAllScansList()
    .Where(s => s.MsnOrder == 1 && s.RetentionTime > 10 && s.RetentionTime < 15)
    .ToList();

var averagedScan = SpectraFileAveraging.AverageSpectra(scansToAverage);

// 3. Deconvolute spectrum
var deconvolutionParams = new ClassicDeconvolutionParameters(
    minAssumedChargeState: 1,
    maxAssumedChargeState: 10,
    deconvolutionTolerancePpm: 20,
    intensityRatioLimit: 3
);

var deconvolutedPeaks = Deconvoluter.Deconvolute(
    averagedScan.MassSpectrum,
    deconvolutionParams
).ToList();

Console.WriteLine($"Found {deconvolutedPeaks.Count} deconvoluted peaks");
```

## Architecture Overview

mzLib follows a modular, layered architecture:

```
┌─────────────────────────────────────────┐
│   Applications (MetaMorpheus, etc.)     │
├─────────────────────────────────────────┤
│   Domain Libraries                      │
│   • Proteomics    • Transcriptomics     │
│   • FlashLFQ      • SpectralAveraging   │
├─────────────────────────────────────────┤
│   Core Omics Framework                  │
│   • IBioPolymer   • Modifications       │
│   • Digestion     • Fragmentation       │
├─────────────────────────────────────────┤
│   Mass Spectrometry & Chemistry         │
│   • MassSpectrometry  • Chemistry       │
│   • Deconvolution     • Chromatography  │
├─────────────────────────────────────────┤
│   File I/O & Utilities                  │
│   • Readers    • UsefulProteomicsDatabases │
│   • MzLibUtil  • MzIdentML • PepXML     │
└─────────────────────────────────────────┘
```

### Key Design Principles

1. **Interface-Based Design**: Core functionality defined through interfaces (`IBioPolymer`, `IBioPolymerWithSetMods`)
2. **Unified Framework**: Common patterns for proteins and RNA analysis
3. **Type Safety**: Strong typing with generics and compile-time checks
4. **Performance**: Memory pooling, caching, and parallel processing support
5. **Extensibility**: Easy to add custom enzymes, modifications, and fragmentation rules

## Library Components

### Chemistry
**Purpose**: Foundation for all mass and formula calculations  
**Key Classes**: `ChemicalFormula`, `PeriodicTable`, `IsotopicDistribution`

### MassSpectrometry
**Purpose**: Core MS data structures and operations  
**Key Classes**: `MsDataScan`, `MsDataFile`, `MzSpectrum`, `ChromatographicPeak`

### Omics
**Purpose**: Base framework for biological polymer analysis  
**Key Interfaces**: `IBioPolymer`, `IBioPolymerWithSetMods`, `IDigestionParams`

### Proteomics
**Purpose**: Protein-specific implementations  
**Key Classes**: `Protein`, `PeptideWithSetModifications`, `Protease`

### Transcriptomics
**Purpose**: RNA-specific implementations  
**Key Classes**: `RNA`, `OligoWithSetMods`, `Rnase`

### Readers
**Purpose**: File I/O for various formats  
**Supported Formats**: mzML, Thermo RAW, pepXML, mzIdentML, FASTA, UniProt XML

### UsefulProteomicsDatabases
**Purpose**: Database loading and management  
**Key Classes**: `ProteinDbLoader`, `DecoyProteinGenerator`, `PtmListLoader`

### FlashLFQ
**Purpose**: Label-free quantification  
**Key Classes**: `FlashLfqEngine`, `ChromatographicPeak`, `ProteinGroup`

### SpectralAveraging
**Purpose**: Spectral averaging and combination  
**Key Classes**: `SpectraFileAveraging`, `ScoreBasedAveraging`

### Chromatography
**Purpose**: Retention time prediction  
**Key Classes**: `SSRCalc3RetentionTimePredictor`

## Installation

### NuGet Package Manager

```bash
# Install core library
dotnet add package mzLib

# Or install specific components
dotnet add package mzLib.Chemistry
dotnet add package mzLib.Proteomics
dotnet add package mzLib.Transcriptomics
dotnet add package FlashLFQ
```

### Package Manager Console (Visual Studio)

```powershell
Install-Package mzLib
```

### .csproj Reference

```xml
<ItemGroup>
  <PackageReference Include="mzLib" Version="*" />
</ItemGroup>
```

## Requirements

- **.NET 8.0** or higher
- **Supported Platforms**: Windows, Linux, macOS
- **Optional**: Thermo MSFileReader (for native RAW file support on Windows)

## Getting Help

### Documentation
- Browse the wiki pages listed above for detailed documentation
- Each page includes:
  - Overview and key features
  - System design with class diagrams
  - Comprehensive code examples
  - Common use cases
  - Best practices

### Community
- **GitHub Issues**: [Report bugs or request features](https://github.com/smith-chem-wisc/mzLib/issues)
- **Discussions**: [Ask questions and share ideas](https://github.com/smith-chem-wisc/mzLib/discussions)
- **Pull Requests**: Contributions are welcome!

### Examples
Each wiki page includes working code examples. For additional examples, see:
- Test projects in the repository
- MetaMorpheus source code (uses mzLib extensively)

## Related Projects

### Built on mzLib
- **[MetaMorpheus](https://github.com/smith-chem-wisc/MetaMorpheus)**: Proteomics search engine and PTM discovery tool
- **[FlashLFQ](https://github.com/smith-chem-wisc/FlashLFQ)**: Label-free quantification

## Contributing

We welcome contributions! Please:

1. **Fork** the repository
2. **Create** a feature branch
3. **Commit** your changes with clear messages
4. **Write tests** for new functionality
5. **Submit** a pull request

See [CONTRIBUTING.md](https://github.com/smith-chem-wisc/mzLib/blob/master/CONTRIBUTING.md) for detailed guidelines.

## License

mzLib is licensed under the **MIT License**. See [LICENSE](https://github.com/smith-chem-wisc/mzLib/blob/master/LICENSE) for details.

---

## Quick Navigation

### By Topic

**Beginners**
1. [Chemistry](https://github.com/smith-chem-wisc/mzLib/wiki/Chemistry) - Start here for fundamentals
2. [Mass Spectrometry](https://github.com/smith-chem-wisc/mzLib/wiki/Mass-Spectrometry) - MS data basics
3. [MS Data File Reading](https://github.com/smith-chem-wisc/mzLib/wiki/File-Reading:-Mass-Spec) - Load your first file

**Proteomics Researchers**
1. [Omics: Base Foundation](https://github.com/smith-chem-wisc/mzLib/wiki/Omics:-Base-Foundation) - Understand the framework
2. [Proteomics](https://github.com/smith-chem-wisc/mzLib/wiki/Proteomics) - Protein analysis
3. [Omics: Modifications](https://github.com/smith-chem-wisc/mzLib/wiki/Omics:-Modifications) - PTMs
4. [Omics: Digestion](https://github.com/smith-chem-wisc/mzLib/wiki/Omics:-Digestion) - Enzymatic digestion
5. [Omics: Fragmentation](https://github.com/smith-chem-wisc/mzLib/wiki/Omics:-Fragmentation) - MS/MS spectra

**RNA/Transcriptomics Researchers**
1. [Omics: Base Foundation](https://github.com/smith-chem-wisc/mzLib/wiki/Omics:-Base-Foundation) - Understand the framework
2. [Transcriptomics](https://github.com/smith-chem-wisc/mzLib/wiki/Transcriptomics) - RNA analysis
3. [Omics: Modifications](https://github.com/smith-chem-wisc/mzLib/wiki/Omics:-Modifications) - RNA modifications
4. [Omics: Digestion](https://github.com/smith-chem-wisc/mzLib/wiki/Omics:-Digestion) - RNase digestion
5. [Omics: Fragmentation](https://github.com/smith-chem-wisc/mzLib/wiki/Omics:-Fragmentation) - Oligonucleotide fragmentation

**Advanced Topics**
- [Spectral Deconvolution](https://github.com/smith-chem-wisc/mzLib/wiki/Spectral-Deconvolution-(Decharging-and-Deisotoping)) - Charge state determination
- [Spectral Averaging](https://github.com/smith-chem-wisc/mzLib/wiki/Spectral-Averaging) - Signal enhancement
- [Retention Time Prediction](https://github.com/smith-chem-wisc/mzLib/wiki/Retention-Time-Prediction) - Chromatography
- [Omics: Decoy Generation](https://github.com/smith-chem-wisc/mzLib/wiki/Omics:-Decoy-Generation) - FDR control

### By Task

**Loading Data**
- [MS Data File Reading](https://github.com/smith-chem-wisc/mzLib/wiki/File-Reading:-Mass-Spec)
- [Result File Reading](https://github.com/smith-chem-wisc/mzLib/wiki/File-Reading:-Result-Formats)
- [Sequence Database File Reading](https://github.com/smith-chem-wisc/mzLib/wiki/File-Reading:-Sequence-Databases)

**Processing Sequences**
- [Omics: Digestion](https://github.com/smith-chem-wisc/mzLib/wiki/Omics:-Digestion)
- [Omics: Modifications](https://github.com/smith-chem-wisc/mzLib/wiki/Omics:-Modifications)
- [Omics: Fragmentation](https://github.com/smith-chem-wisc/mzLib/wiki/Omics:-Fragmentation)

**Processing Spectra**
- [Spectral Deconvolution](https://github.com/smith-chem-wisc/mzLib/wiki/Spectral-Deconvolution-(Decharging-and-Deisotoping))
- [Spectral Averaging](https://github.com/smith-chem-wisc/mzLib/wiki/Spectral-Averaging)

**Calculating Properties**
- [Chemistry](https://github.com/smith-chem-wisc/mzLib/wiki/Chemistry)
- [Retention Time Prediction](https://github.com/smith-chem-wisc/mzLib/wiki/Retention-Time-Prediction)

---

**Welcome to mzLib!** Start with the [Chemistry](https://github.com/smith-chem-wisc/mzLib/wiki/Chemistry) or [Omics: Base Foundation](https://github.com/smith-chem-wisc/mzLib/wiki/Omics:-Base-Foundation) pages, or jump directly to the topic that interests you most.
