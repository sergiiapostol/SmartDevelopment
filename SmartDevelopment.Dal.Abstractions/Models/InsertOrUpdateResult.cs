namespace SmartDevelopment.Dal.Abstractions.Models
{
    public class InsertOrUpdateResult
    {
        public InsertOrUpdateResult(long inserted, long updated)
        {
            Inserted = inserted;
            Updated = updated;
        }

        public long Inserted { get; }

        public long Updated { get; }
    }
}