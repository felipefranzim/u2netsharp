using EmbedIO.WebSockets;
using System.Text;
using System.Text.Json;

namespace U2NetSharp.Server.BGRemoval
{
    public class BackgroundRemovalServer : WebSocketModule
    {
        private U2NetSharp.BackgroundRemoval _backgroundRemoval;
        public BackgroundRemovalServer(string path) : base(path, true)
        {
            _backgroundRemoval = new U2NetSharp.BackgroundRemoval();
        }

        protected override async Task OnMessageReceivedAsync(IWebSocketContext context, byte[] buffer, IWebSocketReceiveResult result)
        {
            string message = Encoding.UTF8.GetString(buffer);

            try
            {
                var request = JsonSerializer.Deserialize<BackgroundRemovalRequest>(message);

                if(string.IsNullOrEmpty(request!.ImageBase64))
                    await SendAsync(context, JsonSerializer.Serialize(new BackgroundRemovalResponse(success: false, errorMessage: "Nenhuma imagem foi enviada para a remoção de fundo")));

                var image = RemoveDataImageHeader(request!.ImageBase64);

                var imageWithoutBackground = _backgroundRemoval.RemoveBackground(Convert.FromBase64String(image));

                if (imageWithoutBackground != null)
                    await SendAsync(context, JsonSerializer.Serialize(new BackgroundRemovalResponse(success: true, image: Convert.ToBase64String(imageWithoutBackground))));
                else
                    await SendAsync(context, JsonSerializer.Serialize(new BackgroundRemovalResponse(success: false, errorMessage: "A imagem com o fundo removido não foi retornada" )));
            }
            catch (JsonException ex)
            {
                await SendAsync(context, JsonSerializer.Serialize(new BackgroundRemovalResponse(success: false, errorMessage: $"Houve um erro ao remover o fundo da imagem: {ex.Message}")));
            }
        }

        private static string RemoveDataImageHeader(string base64String)
        {
            // Verifica se a string contém o cabeçalho "data:image"
            const string dataImagePrefix = "data:image";
            if (base64String.StartsWith(dataImagePrefix))
            {
                // Localiza a posição do "base64," e remove tudo até lá
                int index = base64String.IndexOf("base64,") + "base64,".Length;
                return base64String.Substring(index);
            }

            // Retorna a string original caso o cabeçalho não esteja presente
            return base64String;
        }
    }
}
