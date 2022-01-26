using Rosreestr.Sessions.Classes;

namespace Rosreestr.Sessions
{
    public interface IRosreestrNumberSearchSession
    {
        bool Found { get; set; }
        RequestDownloadInfo DownloadRequest(string numRequest, string dirPath);
    }
}
