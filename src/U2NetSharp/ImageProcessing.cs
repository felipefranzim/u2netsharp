using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace U2NetSharp;

public static class ImageProcessing
{
    public static byte[] PrepareImageSize(byte[] imageBytes, int width)
    {
        int height = 0;
        using var image = SixLabors.ImageSharp.Image.Load<Rgb24>(imageBytes);

        if (image.Width > width)
        {
            var aspectRatio = (float)image.Width / image.Height;
            height = (int)(width / aspectRatio);
            image.Mutate(x => x.Resize(width, height));
        }

        using var ms = new MemoryStream();
        image.SaveAsJpeg(ms, new SixLabors.ImageSharp.Formats.Jpeg.JpegEncoder() { Quality = 100 });
        return ms.ToArray();
    }

    public static float[] PreprocessImage(Stream imageStream)
    {
        using var image = SixLabors.ImageSharp.Image.Load<Rgb24>(imageStream);
        image.Mutate(x =>
        {
            //x.Grayscale();             // Converte para escala de cinza
            x.GaussianBlur(2.0f);      // Reduz ruídos
            x.Resize(320, 320);
        });

        // Valores de média e desvio padrão em RGB (testar com ordem BGR para comparar resultados também)
        float[] mean = { 0.485f, 0.456f, 0.406f }; // RGB (testar com ordem BGR para comparar resultados também)
        float[] std = { 0.229f, 0.224f, 0.225f };

        var input = new float[3 * 320 * 320];
        for (int y = 0; y < 320; y++)
        {
            for (int x = 0; x < 320; x++)
            {
                var pixel = image[x, y];

                int idx = y * 320 + x;

                // RGB (testar com ordem BGR para comparar resultados também)
                input[0 * 320 * 320 + idx] = ((pixel.R / 255f) - mean[0]) / std[0];
                input[1 * 320 * 320 + idx] = ((pixel.G / 255f) - mean[1]) / std[1];
                input[2 * 320 * 320 + idx] = ((pixel.B / 255f) - mean[2]) / std[2];
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
            //x.GaussianBlur(3); // Suaviza bordas, para evitar bordas duras ou detalhes excessivos nas transições -> Ex: 1 (desfoque mais leve) | 5 (desfoque mais intenso)
            x.Resize(originalWidth, originalHeight, KnownResamplers.Lanczos3);
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

                // Mistura o alpha de forma gradual
                output[x, y] = new Rgba32(
                    (byte)((image[x, y].R * alpha + white.R * (255 - alpha)) / 255),
                    (byte)((image[x, y].G * alpha + white.G * (255 - alpha)) / 255),
                    (byte)((image[x, y].B * alpha + white.B * (255 - alpha)) / 255),
                    255
                );
            }
        }

        return output;
    }

    public static Image<L8> SmoothBorders(Image<Rgba32> image, Image<L8> mask)
    {
        for (int y = 0; y < image.Height; y++)
        {
            for (int x = 0; x < image.Width; x++)
            {
                byte alpha = mask[x, y].PackedValue;

                // Reduz o alpha gradualmente nas bordas
                if (alpha > 0 && alpha < 255)
                {
                    alpha = (byte)(alpha * 0.9); // Ajustar o fator conforme necessário. O 0.9 tem tido efeito positivo na maioria dos testes
                }

                mask[x, y] = new L8(alpha);
            }
        }

        return mask;
    }

    public static void Binarize(Image<L8> image, float threshold)
    {
        // Binariza uma imagem em escala de cinza com base em um limiar
        // Pixels acima ou iguais ao limiar ficam brancos (255), enquanto os abaixo ficam pretos (0)

        image.ProcessPixelRows(accessor =>
        {
            for (int y = 0; y < accessor.Height; y++)
            {
                var row = accessor.GetRowSpan(y);
                for (int x = 0; x < row.Length; x++)
                {
                    // Acessa o valor do pixel usando PackedValue
                    var pixelValue = row[x].PackedValue;

                    // Se o valor do pixel for maior ou igual ao threshold, a gente transforma ele em branco; caso contrário, em preto
                    row[x] = pixelValue >= (byte)(threshold * 255) ? new L8(255) : new L8(0);
                }
            }
        });
    }

    /// <summary>
    /// Aplica a operação de Erosão na imagem
    /// Contrai áreas brancas ao redor de pixels brilhantes.
    /// </summary>
    public static void Erode(this Image<L8> image, int radius)
    {
        // Faz uma cópia da imagem para referência durante a erosão
        var copy = image.Clone();

        // Itera diretamente sobre os pixels
        for (int y = 0; y < image.Height; y++)
        {
            for (int x = 0; x < image.Width; x++)
            {
                // Verifica se algum pixel vizinho é preto
                if (IsNeighborBlack(copy, x, y, radius))
                {
                    image[x, y] = new L8(0); // Torna o pixel preto
                }
            }
        }
    }

    /// <summary>
    /// Aplica a operação de Dilatação na imagem
    /// Expande áreas brancas ao redor de pixels brilhantes
    /// </summary>
    public static void Dilate(this Image<L8> image, int radius)
    {
        // Faz uma cópia da imagem para referência durante a dilatação
        var copy = image.Clone();

        // Itera diretamente sobre os pixels
        for (int y = 0; y < image.Height; y++)
        {
            for (int x = 0; x < image.Width; x++)
            {
                // Verifica se algum pixel vizinho é branco
                if (IsNeighborWhite(copy, x, y, radius))
                {
                    image[x, y] = new L8(255); // Torna o pixel branco
                }
            }
        }
    }

    /// <summary>
    /// Verifica se algum pixel vizinho está branco (255) dentro do raio.
    /// </summary>
    private static bool IsNeighborWhite(Image<L8> image, int x, int y, int radius)
    {
        for (int offsetY = -radius; offsetY <= radius; offsetY++)
        {
            for (int offsetX = -radius; offsetX <= radius; offsetX++)
            {
                int neighborX = x + offsetX;
                int neighborY = y + offsetY;

                // Verifica se o pixel vizinho está dentro dos limites da imagem
                if (neighborX >= 0 && neighborX < image.Width &&
                    neighborY >= 0 && neighborY < image.Height)
                {
                    if (image[neighborX, neighborY].PackedValue == 255)
                    {
                        return true;
                    }
                }
            }
        }
        return false;
    }

    /// <summary>
    /// Verifica se algum pixel vizinho está preto (valor 0) dentro do raio.
    /// </summary>
    private static bool IsNeighborBlack(Image<L8> image, int x, int y, int radius)
    {
        for (int offsetY = -radius; offsetY <= radius; offsetY++)
        {
            for (int offsetX = -radius; offsetX <= radius; offsetX++)
            {
                int neighborX = x + offsetX;
                int neighborY = y + offsetY;

                if (neighborX >= 0 && neighborX < image.Width &&
                    neighborY >= 0 && neighborY < image.Height)
                {
                    if (image[neighborX, neighborY].PackedValue == 0)
                    {
                        return true;
                    }
                }
            }
        }
        return false;
    }

    public static Image<L8> CombineWithOriginalAlpha(Image<L8> originalMask, Image<L8> featheredMask)
    {
        var combinedMask = originalMask.Clone();

        for (int y = 0; y < originalMask.Height; y++)
        {
            for (int x = 0; x < originalMask.Width; x++)
            {
                byte originalAlpha = originalMask[x, y].PackedValue;
                byte featheredAlpha = featheredMask[x, y].PackedValue;

                // Preserva o alpha original para áreas internas (alpha >= 200)
                if (originalAlpha >= 200)
                {
                    combinedMask[x, y] = new L8(originalAlpha);
                }
                else
                {
                    // Usa o alpha suavizado para áreas externas
                    combinedMask[x, y] = new L8(Math.Max(originalAlpha, featheredAlpha));
                }
            }
        }

        return combinedMask;
    }

    public static Image<L8> FeatherMaskOptimized(Image<L8> mask, int featherRadius)
    {
        var featheredMask = mask.Clone();

        // Array para armazenar a intensidade da máscara
        float[,] distances = new float[mask.Width, mask.Height];

        // Passo 1: Inicialização o array com base nos valores da máscara
        for (int y = 0; y < mask.Height; y++)
        {
            for (int x = 0; x < mask.Width; x++)
            {
                distances[x, y] = mask[x, y].PackedValue == 255 ? 0 : float.MaxValue;
            }
        }

        // Passo 2: Aplicar a transformacao de distancia (passagem para frente)
        for (int y = 0; y < mask.Height; y++)
        {
            for (int x = 0; x < mask.Width; x++)
            {
                if (x > 0)
                    distances[x, y] = Math.Min(distances[x, y], distances[x - 1, y] + 1);
                if (y > 0)
                    distances[x, y] = Math.Min(distances[x, y], distances[x, y - 1] + 1);
            }
        }

        // Passo 3: Aplicar a transformacao de distancia (Ppassagem para trás)
        for (int y = mask.Height - 1; y >= 0; y--)
        {
            for (int x = mask.Width - 1; x >= 0; x--)
            {
                if (x < mask.Width - 1)
                    distances[x, y] = Math.Min(distances[x, y], distances[x + 1, y] + 1);
                if (y < mask.Height - 1)
                    distances[x, y] = Math.Min(distances[x, y], distances[x, y + 1] + 1);
            }
        }

        // Passo 4: Aplicar feathering com base no raio
        for (int y = 0; y < mask.Height; y++)
        {
            for (int x = 0; x < mask.Width; x++)
            {
                float alphaFactor = Math.Min(1, Math.Max(0, (featherRadius - distances[x, y]) / featherRadius));
                //float alphaFactor = Math.Min(1, distances[x, y] / featherRadius);
                byte originalAlpha = mask[x, y].PackedValue;

                // Preserva o alpha original se for totalmente preenchido
                if (originalAlpha == 255)
                {
                    featheredMask[x, y] = new L8(originalAlpha);
                }
                else
                {
                    featheredMask[x, y] = new L8((byte)(originalAlpha * alphaFactor));
                }
            }
        }

        return featheredMask;
    }
}
