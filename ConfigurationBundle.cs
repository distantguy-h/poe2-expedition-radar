using System.Collections.Generic;

internal sealed record ConfigurationBundle(OffsetsDocument OffsetDocument, RuntimeOffsets Offsets, IReadOnlyList<Aob.Pattern> Patterns, RuneMonolithCatalog Catalog, string AppVersion);
