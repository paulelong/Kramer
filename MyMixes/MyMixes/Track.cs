using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace MyMixes
{
    public class Track
    {
        public string Name { get; set; }
        public string FullPath { get; set; }
        public bool isProject { get; set; }

        public void Print()
        {
            
            Debug.Print("{2} {0} {1}\n", Name, FullPath, isProject ? "Project" : "Track");
        }
    }
}
