using Shared.Utills;
using System;
using System.Configuration;
using System.Reflection;

namespace Shared
{
    public static class SETTINGS
    {
        private static readonly string Uri = new Uri(Assembly.GetExecutingAssembly().GetName().CodeBase).LocalPath;

        public static readonly string PIPELINE_DB_CONNECTION = ConfigurationManager.OpenExeConfiguration(Uri).AppSettings.Settings["Connection"].Value;

        public static readonly string XML_FOLDER = ConfigurationManager.OpenExeConfiguration(Uri).AppSettings.Settings["XmlFolder"].Value;

        public static readonly int PREPARE_PIPELINE_THREAD_COUNT = ConfigurationManager.OpenExeConfiguration(Uri).AppSettings.Settings["PrepareThreadCount"].Value.AsInt();

        public static readonly int ORDER_PIPELINE_THREAD_COUNT = ConfigurationManager.OpenExeConfiguration(Uri).AppSettings.Settings["OrderThreadCount"].Value.AsInt();

        public static readonly int UPLOAD_XML_THREAD_COUNT = ConfigurationManager.OpenExeConfiguration(Uri).AppSettings.Settings["UploadThreadCount"].Value.AsInt();

        public static readonly int PICKUP_ATTEMPT_DELAY = ConfigurationManager.OpenExeConfiguration(Uri).AppSettings.Settings["PickUpAttemptDelayHour"].Value.AsInt();

        public static readonly int FIRST_PICKUP_ATTEMPT_DELAY = ConfigurationManager.OpenExeConfiguration(Uri).AppSettings.Settings["FirstPickUpAttemptDelayHour"].Value.AsInt();
    }
}
