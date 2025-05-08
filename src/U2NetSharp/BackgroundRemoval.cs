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
            var preparedImage = ImageProcessing.PrepareImageSize(image, 1920);
            using (var ms = new MemoryStream(preparedImage))
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

                using(var originalMs = new MemoryStream(preparedImage))
                {
                    using var originalImage = Image.Load<Rgba32>(originalMs);

                    Console.WriteLine("Post processing mask...");
                    var mask = ImageProcessing.PostprocessMask(output, originalImage.Width, originalImage.Height);

                    mask.Dilate(1);
                    mask.Mutate(x =>
                    {
                        x.GaussianBlur(1.5f); // Suaviza bordas abruptas
                    });

                    Console.WriteLine("Feathering mask...");
                    var feathredMask = ImageProcessing.FeatherMaskOptimized(mask, 2); // Suaviza bordas ainda mais
                    mask = ImageProcessing.CombineWithOriginalAlpha(mask, feathredMask);

                    //ImageProcessing.Binarize(mask, 0.6f); // Binariza a máscara

                    Console.WriteLine("Applying mask...");
                    var final = ImageProcessing.ApplyMaskWithWhiteBackground(originalImage.CloneAs<Rgba32>(), mask);

                    Console.WriteLine("Saving mask...");
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
