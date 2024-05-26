using ArkProjects.Hath.ClientApi.Responses;

namespace ArkProjects.Hath.ClientApi;

public class HathClientException : Exception
{
    public IHathClientResponse Response { get; }

    public HathClientException(string message, IHathClientResponse resp, Exception? innerException = null) : base(
        message, innerException)
    {
        Response = resp;
    }
}