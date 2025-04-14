namespace U2NetSharp.Server.BGRemoval;

public record BackgroundRemovalRequest(string ImageBase64, bool useLightweightModel = false);
