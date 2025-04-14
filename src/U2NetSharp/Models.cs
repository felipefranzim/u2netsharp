using Microsoft.ML.Data;

namespace U2NetSharp;

public class ImageInput
{
    [VectorType(3, 320, 320)]
    [ColumnName("input.1")]
    public float[] Data { get; set; }
}

public class ImageOutput
{
    [VectorType(1, 320, 320)]
    [ColumnName("output")]
    public float[] Output { get; set; }
}
