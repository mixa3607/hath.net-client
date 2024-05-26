namespace ArkProjects.Hath.WebService.Misc;

public interface IHathFileResult : IResult
{
    RequestedFile RequestedFile { get; }
}