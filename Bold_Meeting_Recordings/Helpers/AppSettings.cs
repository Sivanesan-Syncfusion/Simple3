namespace Bold_Meeting_Recordings.Helpers
{
    public class AppSettings
    {
        public string BucketName { get; set; }
        public string AccessKey { get; set; }
        public string SecretKey { get; set; }
        public string FolderName { get; set; }
        public string AwsUser { get; set; }
        public string AwsRegion { get; set; }
        public string AwsS3BaseUrl { get; set; }
    }
}