class U2NetSharpConnector {
    constructor() {
        this.url = "ws://localhost:8075/background-removal";
        this.socket = null;
    }

    connect(onMessageCallback) {
        this.socket = new WebSocket(this.url);

        this.socket.onopen = () => {
            console.log("Conexão com o serviço de remoção de fundo de imagens aberta...");
        };

        this.socket.onmessage = (event) => {
            console.log("Mensagem recebida do serviço de remoção de fundo de imagens:", event.data);

            try {
                const message = JSON.parse(event.data);
                if (onMessageCallback) {
                    onMessageCallback(message);
                }
            } catch (e) {
                console.error("Erro ao converter mensagem para JSON:", e);
            }
        };

        this.socket.onclose = (event) => {
            console.log("Conexão com o serviço de remoção de fundo de imagens fechada:", event);
        };

        this.socket.onerror = (error) => {
            console.error("Erro no serviço de remoção de fundo de imagens:", error);
        };
    }

    sendMessage(imageBase64, useLieghtweightModel = false) {
        if (this.socket.readyState === WebSocket.OPEN) {
            const jsonMessage = JSON.stringify({
                ImageBase64: imageBase64,
                useLightweightModel: useLieghtweightModel
            });
            this.socket.send(jsonMessage);
            console.log("Mensagem enviada ao servidor de remoção de fundo de imagens:", jsonMessage);
        } else {
            console.error("O serviço de remoção de fundo de imagens não está conectado.");
        }
    }
}

