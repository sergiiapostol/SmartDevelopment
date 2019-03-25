namespace SmartDevelopment.AzureStorage.Blobs
{
    public interface IContentTypeResolver
    {
        string GetContentType(string fileExtension);
    }
}
