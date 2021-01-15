using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Text;

namespace StepFunc_Lab04
{
    public static class ImageUtilities
    {
        
        public static int ScaleWidth(int originalHeight, int newHeight, int originalWidth)
        {
            var scale = Convert.ToDouble(newHeight) / Convert.ToDouble(originalHeight);

            return Convert.ToInt32(originalWidth * scale);
        }
    }
}
