namespace ReportingService
{
    public class MongoSettings
    {
        public string ConnectionString { get; set; } = "mongodb://host.docker.internal:27017";
        public string DatabaseName { get; set; } = "TaskManagementDB";
    }
}