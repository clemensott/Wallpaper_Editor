using System;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Media.Imaging;

namespace Wallpaper_Editor
{
    class Program
    {
        private const int completeWidth = 4720, leftWidth = 1280, middleWidth = 1920, rightWidth = 1280, height = 1080;
        private static volatile int a = 0;
        private static string[] pics;
        private static object obj = new object();

        static void Main(string[] args)
        {
            if (args.Length != 1) return;
            //pics = Directory.GetFiles(@"D:\Clemens\Videos\Fraps\SplitSecond\Bilder\Neu");
            pics = Directory.GetFiles(args[0]);

            Thread t1 = new Thread(new ThreadStart(DoThread));
            Thread t2 = new Thread(new ThreadStart(DoThread));
            Thread t3 = new Thread(new ThreadStart(DoThread));
            Thread t4 = new Thread(new ThreadStart(DoThread));

            t1.Start();
            t2.Start();
            t3.Start();
            t4.Start();

            t1.Join();
            t2.Join();
            t3.Join();
            t4.Join();
        }

        private static void DoThread()
        {
            int i;

            while (true)
            {
                i = GetIndexPlusPlus();

                if (i >= pics.Length) return;

                EditPic(pics[i]);
            }
        }

        private static int GetIndexPlusPlus()
        {
            lock (obj)
            {
                return a++;
            }
        }

        private static void EditPic(string path)
        {
            try
            {
                string nPath = Path.Combine(Path.GetDirectoryName(path), "Edit", Path.GetFileName(path));

                if (File.Exists(nPath)) return;

                byte[] oPixelData, nPixelData;
                byte[,,] oPixelMatrix, nPixelMatrix;

                BitmapSource oBmp = new BitmapImage(new Uri(path));
                WriteableBitmap nBmp = new WriteableBitmap(leftWidth + middleWidth + rightWidth, height, oBmp.DpiX, oBmp.DpiY, oBmp.Format, null);

                int bytePerPixel = oBmp.Format.BitsPerPixel / 8;
                int oStride = oBmp.PixelWidth * bytePerPixel, nStride = nBmp.PixelWidth * bytePerPixel;
                int gapWidth = (completeWidth - (leftWidth + middleWidth + rightWidth)) / 2;

                if (oBmp.PixelWidth < 4720 || oBmp.PixelHeight < 1080 || File.Exists(nPath))
                {
                    return;
                }

                oPixelData = new byte[oStride * height];
                nPixelData = new byte[nStride * height];

                oBmp.CopyPixels(oPixelData, oStride, 0);

                oPixelMatrix = GetAsPixelDataMatrix(oPixelData, bytePerPixel);
                nPixelMatrix = new byte[leftWidth + middleWidth + rightWidth, height, bytePerPixel];

                SetPixel(ref nPixelMatrix, 0, middleWidth, oPixelMatrix, leftWidth + gapWidth);
                SetPixel(ref nPixelMatrix, middleWidth, rightWidth, oPixelMatrix, leftWidth + gapWidth + middleWidth + gapWidth);
                SetPixel(ref nPixelMatrix, middleWidth + leftWidth, rightWidth, oPixelMatrix, 0);

                nPixelData = GetAsArray(nPixelMatrix);

                nBmp.WritePixels(new Int32Rect(0, 0, leftWidth + middleWidth + rightWidth, height), nPixelData, nStride, 0);

                if (!Directory.Exists(Path.GetDirectoryName(nPath))) Directory.CreateDirectory(Path.GetDirectoryName(nPath));

                BitmapEncoder encoder = GetBitmapEncoder(nPath);

                encoder.Frames.Add(BitmapFrame.Create(nBmp));

                using (var stream = new FileStream(nPath, FileMode.Create))
                {
                    encoder.Save(stream);
                }

                return;
            }
            catch
            {
                Console.WriteLine(path);
            }
        }

        private static byte[,,] GetAsPixelDataMatrix(byte[] data, int bytePerPixel)
        {
            int width = data.Length / height / bytePerPixel;
            byte[,,] matrix = new byte[width, height, bytePerPixel];

            for (int i = 0; i < height; i++)
            {
                for (int j = 0; j < width; j++)
                {
                    for (int k = 0; k < bytePerPixel; k++)
                    {
                        int a = i * width * bytePerPixel + j * bytePerPixel + k;
                        matrix[j, i, k] = data[i * width * bytePerPixel + j * bytePerPixel + k];
                    }
                }
            }

            return matrix;
        }

        private static void SetPixel(ref byte[,,] setMatrix, int setX, int width, byte[,,] getMatrix, int getX)
        {
            int bytePerPixel = setMatrix.GetLength(2);

            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    for (int k = 0; k < bytePerPixel; k++)
                    {
                        byte b =
                        setMatrix[setX + i, j, k] = getMatrix[getX + i, j, k];
                    }
                }
            }
        }

        private static byte[] GetAsArray(byte[,,] matrix)
        {
            int width = matrix.GetLength(0), bytePerPixel = matrix.GetLength(2);
            byte[] array = new byte[width * height * bytePerPixel];

            for (int i = 0; i < height; i++)
            {
                for (int j = 0; j < width; j++)
                {
                    for (int k = 0; k < bytePerPixel; k++)
                    {
                        array[i * width * bytePerPixel + j * bytePerPixel + k] = matrix[j, i, k];
                    }

                    if (j > 2000) { }
                }
            }

            return array;
        }

        private static BitmapEncoder GetBitmapEncoder(string path)
        {
            switch (Path.GetExtension(path).ToLower())
            {
                case ".bmp":
                    return new BmpBitmapEncoder();

                case ".jpg":
                    return new JpegBitmapEncoder();

                case ".jpeg":
                    return new JpegBitmapEncoder();

                case ".gif":
                    return new GifBitmapEncoder();

                case ".png":
                    return new PngBitmapEncoder();

                default:
                    return new BmpBitmapEncoder();
            }
        }
    }
}
