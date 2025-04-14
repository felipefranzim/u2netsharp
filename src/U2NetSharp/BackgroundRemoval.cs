using Microsoft.ML.OnnxRuntime.Tensors;
using Microsoft.ML.OnnxRuntime;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace U2NetSharp;

public sealed class BackgroundRemoval
{
    private InferenceSession? _inferenceSession = null;
    private bool UseLightweightModel = false;

    public BackgroundRemoval(bool useLightweightModel = false)
    {
        UseLightweightModel = useLightweightModel;
    }

    private void LoadModel()
    {
        if(_inferenceSession == null)
        {
            Console.WriteLine("Loading model...");
            _inferenceSession = new InferenceSession($"{AppDomain.CurrentDomain.BaseDirectory}OnnxModels\\u2net{(UseLightweightModel ? "p" : "")}.onnx");
        }
    }

    public byte[]? RemoveBackground(byte[] image)
    {
        try
        {
            using (var ms = new MemoryStream(image))
            {
                Console.WriteLine("Processing image...");
                var inputData = ImageProcessing.PreprocessImage(ms);

                LoadModel();

                var inputTensor = new DenseTensor<float>(inputData, new[] { 1, 3, 320, 320 });
                var inputs = new List<NamedOnnxValue> { NamedOnnxValue.CreateFromTensor("input.1", inputTensor) };

                Console.WriteLine("Getting results...");
                using var results = _inferenceSession!.Run(inputs);

                var outputResult = results.First(r => r.AsTensor<float>().Dimensions.SequenceEqual(new[] { 1, 1, 320, 320 }));
                var output = outputResult.AsTensor<float>().ToArray();

                using(var originalMs = new MemoryStream(image))
                {
                    using var originalImage = Image.Load<Rgba32>(originalMs);

                    Console.WriteLine("Applying mask...");
                    var mask = ImageProcessing.PostprocessMask(output, originalImage.Width, originalImage.Height);
                    mask.Mutate(x =>
                    {
                        x.GaussianBlur(2); // Suaviza bordas abruptas
                    });

                    ImageProcessing.Binarize(mask, 0.6f); // Binariza a máscara

                    var final = ImageProcessing.ApplyMaskWithWhiteBackground(originalImage.CloneAs<Rgba32>(), mask);

                    using (var finalMs = new MemoryStream())
                    {
                        final.SaveAsJpeg(finalMs, new SixLabors.ImageSharp.Formats.Jpeg.JpegEncoder() { Quality = 100 });

                        return finalMs.ToArray();
                    }
                }
            }
        }
        catch(Exception ex)
        {
            Console.WriteLine("Error: " + ex.Message);
            return null;
        }
    }
}
