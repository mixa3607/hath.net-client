using ArkProjects.Hath.ClientApi;
using ArkProjects.Hath.ClientApi.Responses;

namespace ArkProjects.Hath.WebService.MockHathApi;

public class MockHathClient: HathClient
{
    public MockHathClient(ISettingsStorage settingsStorage) : base(settingsStorage)
    {
    }

    public override Task<HathClientPropsResponse> ServerStatAsync(CancellationToken ct = default)
    {
        var text = GetTextFromFile("server_stat.txt");
        var (status, msg, props) = ParsePropertiesResponse(text);
        return Task.FromResult(new HathClientPropsResponse()
        {
            StatusCode = 200,
            RawText = text,
            Status = status,
            Message = msg,
            Properties = props,
        });
    }

    public override Task<HathClientPropsResponse> ClientLoginAsync(CancellationToken ct = default)
    {
        var text = GetTextFromFile("client_login.txt");
        var (status, msg, props) = ParsePropertiesResponse(text);
        return Task.FromResult(new HathClientPropsResponse()
        {
            StatusCode = 200,
            RawText = text,
            Status = status,
            Message = msg,
            Properties = props,
        });
    }

    public override Task<HathClientCertResponse> GetCertificateAsync(CancellationToken ct = default)
    {
        var bytes = GetBytesFromFile("cert.bin");
        return Task.FromResult(new HathClientCertResponse()
        {
            CertBytes = bytes,
            Message = null,
            StatusCode = 200,
            Status = "OK"
        });
    }


    private string GetTextFromFile(string fileName) => 
        File.ReadAllText(Path.Combine("./MockHathApi/files", fileName));

    private byte[] GetBytesFromFile(string fileName) =>
        File.ReadAllBytes(Path.Combine("./MockHathApi/files", fileName));
}