namespace Rosreestr.Sessions.Classes
{
    public class RequestDownloadInfo
    {
        public bool NoLink { get; private set; }

        public bool NoRequest { get; private set; }

        public bool IsSuccess { get; private set; }

        public string FilePath { get; set; }

        private RequestDownloadInfo() { }


        public static RequestDownloadInfo CreateNoRequestInfo()
        {
            return new RequestDownloadInfo { NoRequest = true };
        }

        public static RequestDownloadInfo CreateNoLinkInfo()
        {
            return new RequestDownloadInfo { NoLink = true };
        }

        public static RequestDownloadInfo CreateSuccessInfo(string filePath)
        {
            return new RequestDownloadInfo { IsSuccess = true, FilePath = filePath };
        }
    }
}
