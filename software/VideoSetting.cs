// Author: Leonardo Tazzini (http://electro-logic.blogspot.it)

using System.Collections.Generic;

namespace CameraVision
{
    public class VideoSetting
    {
        public string Description { get; set; }
        public List<Register> Registers { get; set; }

        public override string ToString()
        {
            return Description;
        }
    }
}
