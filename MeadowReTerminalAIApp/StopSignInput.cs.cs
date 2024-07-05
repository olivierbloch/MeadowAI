using Microsoft.ML.Data;
using Microsoft.ML.Transforms.Image;

namespace StopSignDetection
{
    public struct ImageSettings
    {
        public const int imageHeight = 320;
        public const int imageWidth = 320;
    }


    public class StopSignInput
    {


        [ImageType(ImageSettings.imageHeight, ImageSettings.imageWidth)]
        public MLImage image { get; set; }

    }

}
