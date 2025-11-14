namespace ReportingService
{
    public class MongoSettings
    {
        public string ConnectionString { get; set; } = "mongodb://mongodb:27017";
        public string DatabaseName { get; set; } = "TaskManagementDB";
    }
}