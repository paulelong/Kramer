using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MyMixes
{
    static class MusicUtils
    {
        static public bool isAudioFormat(string filename)
        {
            switch (Path.GetExtension(filename))
            {
                case ".wav":
                case ".mp3":
                case ".wma":
                    return true;
                default:
                    return false;
            }
        }
    }
}
