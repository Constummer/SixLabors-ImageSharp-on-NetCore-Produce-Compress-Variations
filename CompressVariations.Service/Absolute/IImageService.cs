namespace CompressVariations.Service.Absolute
{
    public interface IImageService
    {
        (bool Result, string Message) ProduceVariationsForSingleImage(string imageFilePath, string providedFileName = null);

        (bool Result, string Message) ProduceVariationsForMultipleImageImage(string imagesDirectoryPath);
    }
}