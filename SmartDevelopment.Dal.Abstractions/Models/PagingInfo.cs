namespace SmartDevelopment.Dal.Abstractions.Models
{
    public class PagingInfo
    {
        public PagingInfo(int page, int pageSize)
        {
            Page = page;
            PageSize = pageSize;
        }


        public int Page { get; }

        public int PageSize { get; }

        public override string ToString()
        {
            return $"p:{Page} s:{PageSize}";
        }

        public int Take => PageSize;

        public int Skip => Page * PageSize;

        public static PagingInfo Default => new PagingInfo(0, 50);

        public static PagingInfo OneItem => new PagingInfo(0, 1);
    }
}
