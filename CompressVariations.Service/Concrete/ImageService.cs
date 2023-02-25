using CompressVariations.Service.Absolute;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Processors.Transforms;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace CompressVariations.Service.Concrete
{
    public class ImageService : IImageService
    {
        public ImageService()
        {
        }

        /// <summary>
        /// Will produce images with quality, sampler, size(width,height) and mode variations for specified file.
        /// </summary>
        /// <param name="imageFilePath"></param>
        /// <returns></returns>
        public (bool, string) ProduceVariationsForSingleImage(string imageFilePath, string? providedFileName)
        {
            (bool result, string message) checkFileRequirementsResult = CheckFileRequirements(imageFilePath);
            if (checkFileRequirementsResult.result is false)
                return checkFileRequirementsResult;

            //Create result directory in same folder as given imageFilePath, can be customized.
            string outputDirectory = Path.Combine(Path.GetDirectoryName(imageFilePath), "Results");
            if (!Directory.Exists(outputDirectory))
                Directory.CreateDirectory(outputDirectory);
            List<IResampler> resamplers = GetResamplers();

            //Create qualities for iterations. This list can be customize
            List<int> qualities = new List<int>() { 10, 20, 25, 30, 40, 50, 60, 70, 75, 80, 90, 100 };

            //Load image
            using var image = Image.Load(imageFilePath);

            //Produces image sizes in 16:9, can be customize
            List<Size> sizes = ProduceImageSizes(240, 360, 16.0 / 9.0);

            //To calculate how long it is take to complete all variations
            var msCalculator = new Stopwatch();
            msCalculator.Start();
            foreach (var size in sizes)
            {
                foreach (var quality in qualities)
                {
                    foreach (IResampler resampler in resamplers)
                    {
                        foreach (ResizeMode resizeMode in Enum.GetValues(typeof(ResizeMode)))
                        {
                            //Manuel mode require TargetRectangle definition on ResizeOptions definition.
                            //TODO: this mod will also added to variation creations
                            //For more details check https://docs.sixlabors.com/api/ImageSharp/SixLabors.ImageSharp.Processing.ResizeOptions.html
                            if (resizeMode == ResizeMode.Manual)
                                continue;
                            try
                            {
                                var resizeOptions = new ResizeOptions
                                {
                                    Size = size,
                                    Mode = resizeMode,
                                    Sampler = KnownResamplers.Lanczos8,
                                };
                                //Compress job
                                image.Mutate(x => x.Resize(resizeOptions));

                                //Save as physical file.
                                //If you want to group by any given parameters, you can do it in this section.
                                //just remove comments in line:79-80 then customize as you want in line:80.
                                //Directory.CreateDirectory(Path.Combine(outputhPath, $"{size.Width}_{size.Height}"));
                                string newPath;
                                if (providedFileName is null)
                                    newPath = Path.Combine(outputDirectory, /*$"{size.Width}_{size.Height}",*/
                                    //To remove long file name comes from resampler name space
                                    $"{resampler.GetType().Name}_{resizeOptions.Mode}_{size.Width}_{size.Height}_{quality}.jpeg");
                                else
                                {
                                    newPath = Path.Combine(outputDirectory, providedFileName);
                                    if (!Directory.Exists(newPath))
                                        Directory.CreateDirectory(newPath);
                                    newPath = Path.Combine(newPath,
                                   //To remove long file name comes from resampler name space
                                   $"{resampler.GetType().Name}_{resizeOptions.Mode}_{size.Width}_{size.Height}_{quality}.jpeg");
                                }
                                using var outputStream = new FileStream(newPath, FileMode.Create);
                                image.Save(outputStream, new JpegEncoder() { Quality = quality });
                            }
                            catch (Exception e)
                            {
                                return (false, e.Message);
                            }
                        }
                    }
                }
            }

            msCalculator.Stop();
            short totalSecond = (short)TimeSpan.FromMilliseconds(msCalculator.ElapsedMilliseconds).TotalSeconds;
            return (true, $"{totalSecond} second long took to create all variations");
        }

        /// <summary>
        /// Get all SixLabor.ImageSharp Resamplers. For unwanted modes you can use condition checking.
        /// </summary>
        /// <returns></returns>
        private static List<IResampler> GetResamplers()
        {
            List<IResampler> resamplers = new();

            //Extract resamplers from static definition class.
            foreach (var property in typeof(KnownResamplers).GetProperties())
            {
                if (property.PropertyType == typeof(IResampler))
                {
                    IResampler resampler = (IResampler)property.GetValue(null);
                    resamplers.Add(resampler);
                }
            }

            return resamplers;
        }

        /// <summary>
        /// Will produce images with quality, sampler, size(width,height) and mode variations in specified directory and its sub directories
        /// </summary>
        /// <param name="imagesDirectoryPath"></param>
        /// <returns></returns>
        public (bool, string) ProduceVariationsForMultipleImageImage(string imagesDirectoryPath)
        {
            if (!Directory.Exists(imagesDirectoryPath))
                return (false, "Directory does not exist");

            //To calculate how long it is take to complete all variations with given directory
            var msCalculator = new Stopwatch();
            msCalculator.Start();

            //To get all files in given directory and their all sub directories
            string[] files = Directory.GetFiles(imagesDirectoryPath, "*", SearchOption.AllDirectories);

            foreach (string file in files)
            {
                FileInfo fi = new(file);
                (bool result, string message) result = ProduceVariationsForSingleImage(file, fi.Name.Split(".")[0]);
                if (!result.result)
                    return result;
            }

            msCalculator.Stop();
            short totalSecond = (short)TimeSpan.FromMilliseconds(msCalculator.ElapsedMilliseconds).TotalSeconds;
            return (true, $"{totalSecond} second long took to create all variations");
        }

        #region Helpers

        /// <summary>
        /// Check if file is exist, if file extension is jpeg or even if its windows and mime type is image/jpeg
        /// </summary>
        /// <param name="imageFilePath"></param>
        /// <returns></returns>
        private static (bool, string) CheckFileRequirements(string imageFilePath)
        {
            FileInfo fi = new(imageFilePath);
            if (!fi.Exists)
                return (false, "Specified file does not exist");
            if (fi.Extension != ".jpeg")
                return (false, "Specified file is not a jpeg");
            if (GetMimeType(imageFilePath, out string mimeTypeResult))
            {
                if (mimeTypeResult == "image/jpeg")
                    return (true, "File is jpeg");
                else return (false, $"File mime type is not image/jpeg, it is:{mimeTypeResult}");
            }
            return (true, $"File mime type is unknown but file can be consider as jpeg because:{mimeTypeResult}");
        }

        /// <summary>
        /// Determine if given file's mime type is image/jpeg for windows platform.
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="mimeType"></param>
        /// <returns></returns>
        /// <remark>TODO: Will try to add generic mime type detector for all platforms.</remark>
        private static bool GetMimeType(string fileName, out string mimeType)
        {
            switch (Environment.OSVersion.Platform)
            {
                case PlatformID.Win32NT:
                case PlatformID.Win32S:
                case PlatformID.Win32Windows:
                case PlatformID.WinCE:
                    mimeType = "application/unknown";
                    string ext = Path.GetExtension(fileName).ToLower();
                    Microsoft.Win32.RegistryKey regKey = Microsoft.Win32.Registry.ClassesRoot.OpenSubKey(ext);
                    if (regKey != null && regKey.GetValue("Content Type") != null)
                        mimeType = regKey.GetValue("Content Type").ToString();
                    return true;

                case PlatformID.Unix:
                    mimeType = "Running on Linux or another Unix-based OS";
                    return false;

                default:
                    mimeType = "Running on another operating system";
                    return false;
            }
        }

        /// <summary>
        /// Will produce height and widths with given minimum to maximum height range, with provided aspect ratio
        /// Ex: 240,360,16.0/9.0 will produce;
        ///<br>430,242</br>
        ///<br>434,244</br>
        ///<br>448,252</br>
        ///<br>462,260</br>
        ///<br>466,262</br>
        ///<br>480,270</br>
        ///<br>494,278</br>
        ///<br>498,280</br>
        ///<br>512,288</br>
        ///<br>526,296</br>
        ///<br>530,298</br>
        ///<br>544,306</br>
        ///<br>558,314</br>
        ///<br>562,316</br>
        ///<br>576,324</br>
        ///<br>590,332</br>
        ///<br>594,334</br>
        ///<br>608,342</br>
        ///<br>622,350</br>
        ///<br>626,352</br>
        ///<br>640,360</br>
        /// </summary>
        /// <param name="minHeight">Minimum height to start generate sizes</param>
        /// <param name="maxHeight">Maximum height to stop generate sizes</param>
        /// <param name="aspectRatio">Aspect Ratio to generate sizes</param>
        /// <returns></returns>
        private static List<Size> ProduceImageSizes(int minHeight, int maxHeight, double aspectRatio)
        {
            List<Size> sizes = new List<Size>();

            for (int height = minHeight; height <= maxHeight; height++)
            {
                int width = (int)Math.Round(height * aspectRatio / 2.0) * 2;
                if (Math.Abs((double)width / height - aspectRatio) < 0.0001)
                {
                    sizes.Add(new(width, height));
                }
            }

            //or preset for common video resolutions for 16:9:
            //sizes = new List<Size>()
            //{
            //    new Size(256,144),
            //    new Size(426,240),
            //    new Size(512,288),
            //    new Size(640,360),
            //    new Size(854,480),
            //    new Size(960,536),
            //    new Size(1024,576),
            //    new Size(1280,720),
            //    new Size(1600,900),
            //    new Size(1920,1080)
            //};

            //or preset for common video resolutions for 4:3:
            //sizes = new List<Size>()
            //{
            //   new Size(320,240),
            //   new Size(480,360),
            //   new Size(640,480),
            //   new Size(768,576)
            //};
            return sizes;
        }

        #endregion Helpers
    }
}