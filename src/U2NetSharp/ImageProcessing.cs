using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace U2NetSharp;

public static class ImageProcessing
{
    public static float[] PreprocessImage(Stream imageStream)
    {
        using var image = SixLabors.ImageSharp.Image.Load<Rgb24>(imageStream);
        image.Mutate(x =>
        {
            x.Grayscale();             // Converte para escala de cinza
            x.GaussianBlur(2.5f);      // Reduz ruídos
            x.Resize(320, 320);
        });

        float[] mean = { 0.485f, 0.456f, 0.406f }; // BGR
        float[] std = { 0.229f, 0.224f, 0.225f };

        var input = new float[3 * 320 * 320];
        for (int y = 0; y < 320; y++)
        {
            for (int x = 0; x < 320; x++)
            {
                var pixel = image[x, y];

                int idx = y * 320 + x;

                // BGR order
                input[0 * 320 * 320 + idx] = ((pixel.B / 255f) - mean[0]) / std[0];
                input[1 * 320 * 320 + idx] = ((pixel.G / 255f) - mean[1]) / std[1];
                input[2 * 320 * 320 + idx] = ((pixel.R / 255f) - mean[2]) / std[2];
            }
        }

        return input;
    }

    public static Image<L8> PostprocessMask(float[] output, int originalWidth, int originalHeight)
    {
        var mask = new Image<L8>(320, 320);
        for (int y = 0; y < 320; y++)
        {
            for (int x = 0; x < 320; x++)
            {
                float value = output[y * 320 + x];
                byte intensity = (byte)(Math.Clamp(value, 0f, 1f) * 255);
                mask[x, y] = new L8(intensity);
            }
        }

        mask.Mutate(x =>
        {
            x.GaussianBlur(2); // Suaviza bordas, para evitar bordas duras ou detalhes excessivos nas transições -> Ex: 1 (desfoque mais leve) | 5 (desfoque mais intenso)
            x.Resize(originalWidth, originalHeight, KnownResamplers.Bicubic);
        });
        return mask;
    }

    public static Image<Rgba32> ApplyMask(Image<Rgba32> image, Image<L8> mask)
    {
        var output = new Image<Rgba32>(image.Width, image.Height);
        for (int y = 0; y < image.Height; y++)
        {
            for (int x = 0; x < image.Width; x++)
            {
                var originalPixel = image[x, y];
                byte alpha = mask[x, y].PackedValue;
                output[x, y] = new Rgba32(originalPixel.R, originalPixel.G, originalPixel.B, alpha);
            }
        }
        return output;
    }

    public static Image<Rgba32> ApplyMaskWithWhiteBackground(Image<Rgba32> image, Image<L8> mask)
    {
        var output = new Image<Rgba32>(image.Width, image.Height);
        var white = new Rgba32(255, 255, 255);

        for (int y = 0; y < image.Height; y++)
        {
            for (int x = 0; x < image.Width; x++)
            {
                byte alpha = mask[x, y].PackedValue;

                if (alpha > 10) // Mantém o pixel se a máscara indica que faz parte da pessoa
                {
                    output[x, y] = image[x, y];
                }
                else // Caso contrário, pinta de branco
                {
                    output[x, y] = white;
                }
            }
        }

        return output;
    }

    /// <summary>
    /// Binariza uma imagem em escala de cinza (L8) com base em um limiar.
    /// Pixels acima ou iguais ao limiar tornam-se brancos (255), enquanto os abaixo tornam-se pretos (0).
    /// </summary>
    /// <param name="image">Imagem em escala de cinza (L8).</param>
    /// <param name="threshold">Limiar para binarização (0.0 a 1.0).</param>
    public static void Binarize(Image<L8> image, float threshold)
    {
        image.ProcessPixelRows(accessor =>
        {
            for (int y = 0; y < accessor.Height; y++)
            {
                var row = accessor.GetRowSpan(y);
                for (int x = 0; x < row.Length; x++)
                {
                    // Acessa o valor do pixel usando PackedValue
                    var pixelValue = row[x].PackedValue;

                    // Se o valor do pixel for maior ou igual ao threshold, torne branco; caso contrário, preto
                    row[x] = pixelValue >= (byte)(threshold * 255) ? new L8(255) : new L8(0);
                }
            }
        });
    }
}
