using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SkiaSharp;

namespace SkImageResizer
{
    public class SKImageProcess
    {
        /// <summary>
        /// 清空目的目錄下的所有檔案與目錄
        /// </summary>
        /// <param name="destPath">目錄路徑</param>
        public void Clean(string destPath)
        {
            if (!Directory.Exists(destPath))
            {
                Directory.CreateDirectory(destPath);
            }
            else
            {
                var allImageFiles = Directory.GetFiles(destPath, "*", SearchOption.AllDirectories);

                foreach (var item in allImageFiles)
                {
                    File.Delete(item);
                }
            }
        }

        /// <summary>
        /// 找出指定目錄下的圖片
        /// </summary>
        /// <param name="srcPath">圖片來源目錄路徑</param>
        /// <returns></returns>
        public List<string> FindImages(string srcPath)
        {
            List<string> files = new List<string>();
            files.AddRange(Directory.GetFiles(srcPath, "*.png", SearchOption.AllDirectories));
            files.AddRange(Directory.GetFiles(srcPath, "*.jpg", SearchOption.AllDirectories));
            files.AddRange(Directory.GetFiles(srcPath, "*.jpeg", SearchOption.AllDirectories));
            return files;
        }

        /// <summary>
        /// 進行圖片的縮放作業
        /// </summary>
        /// <param name="sourcePath">圖片來源目錄路徑</param>
        /// <param name="destPath">產生圖片目的目錄路徑</param>
        /// <param name="scale">縮放比例</param>
        public void ResizeImages(string sourcePath, string destPath, double scale)
        {
            if (!Directory.Exists(destPath))
            {
                Directory.CreateDirectory(destPath);
            }

            var allFiles = FindImages(sourcePath);
            foreach (var filePath in allFiles)
            {
                var bitmap = SKBitmap.Decode(filePath);
                var imgPhoto = SKImage.FromBitmap(bitmap);
                var imgName = Path.GetFileNameWithoutExtension(filePath);

                var sourceWidth = imgPhoto.Width;
                var sourceHeight = imgPhoto.Height;

                var destinationWidth = (int) (sourceWidth * scale);
                var destinationHeight = (int) (sourceHeight * scale);

                using var scaledBitmap = bitmap.Resize(
                    new SKImageInfo(destinationWidth, destinationHeight),
                    SKFilterQuality.High);
                using var scaledImage = SKImage.FromBitmap(scaledBitmap);
                using var data = scaledImage.Encode(SKEncodedImageFormat.Jpeg, 100);
                using var s = File.OpenWrite(Path.Combine(destPath, imgName + ".jpg"));
                data.SaveTo(s);
            }
        }

        public async Task ResizeImagesAsync(string sourcePath, string destPath, double scale)
        {
            await ResizeImagesAsync(sourcePath, destPath, scale, CancellationToken.None);
        }

        public async Task ResizeImagesAsync(string sourcePath, string destPath, double scale, CancellationToken cancellationToken)
        {
            if (!Directory.Exists(destPath))
            {
                Directory.CreateDirectory(destPath);
            }

            await Task.Yield();

            var allFiles = FindImages(sourcePath);
            var allTasks = new List<Task>();
            foreach (var filePath in allFiles)
            {
                allTasks.Add(ResizeImageAsync(destPath, scale, filePath, cancellationToken));
            }

            try
            {
                await Task.WhenAll(allTasks);
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine($"Cancel count:{allTasks.Count(a => a.IsCanceled)}");
                foreach (var task in allTasks)
                {
                    switch (task.Status)
                    {
                        case TaskStatus.RanToCompletion:
                            Console.WriteLine($"Task Id: {task.Id} complete");
                            break;
                        case TaskStatus.Canceled:
                            Console.WriteLine($"Task Id: {task.Id} canceled");
                            break;
                        case TaskStatus.Faulted:
                            Console.WriteLine($"Task Id: {task.Id} fault");
                            break;
                    } 
                }
            }
        }

        private Task ResizeImageAsync(string destPath, double scale, string filePath, CancellationToken cancellationToken)
        {
            return Task.Run(() =>
            {
                var bitmap = SKBitmap.Decode(filePath);
                var imgPhoto = SKImage.FromBitmap(bitmap);
                var imgName = Path.GetFileNameWithoutExtension(filePath);

                var sourceWidth = imgPhoto.Width;
                var sourceHeight = imgPhoto.Height;

                var destinationWidth = (int) (sourceWidth * scale);
                var destinationHeight = (int) (sourceHeight * scale);
                cancellationToken.ThrowIfCancellationRequested();
                using var scaledBitmap = bitmap.Resize(
                    new SKImageInfo(destinationWidth, destinationHeight),
                    SKFilterQuality.High);

                using var scaledImage = SKImage.FromBitmap(scaledBitmap);
                using var data = scaledImage.Encode(SKEncodedImageFormat.Jpeg, 100);
                using var s = File.OpenWrite(Path.Combine(destPath, imgName + ".jpg"));
                data.SaveTo(s);
            }, cancellationToken);
        }
    }
}