using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using Xamarin.Forms;

namespace MyMixes
{
    public class QueuedTrack : BaseTrack
    {
        public string Project { get; set; }
       
        static public QueuedTrack FindQueuedTrack(View v)
        {
            Grid g = (Grid)v.Parent;
            QueuedTrack t = (QueuedTrack)g.BindingContext;
            return t;
        }
    }
}
