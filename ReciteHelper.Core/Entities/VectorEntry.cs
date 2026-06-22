using System;
using System.Collections.Generic;
using System.Text;

namespace ReciteHelper.Core.Entities;

public class VectorEntry
{
    public required int Id { get; set; } = 0;

    /// <summary>
    /// Semantic metadata associated with this text chunk.
    /// Contains tags and summary information.
    /// </summary>
    public required Semantics Semantics { get; set; }

    public required string Text { get; set; }
    public required float[] Vector { get; set; }
    public required string SourceFile { get; set; }
    public DateTime CreatedAt { get; set; }
}
