using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;

namespace MyMixes
{
    class ProjectPickerData : INotifyPropertyChanged
    {
        private bool isVisible = false;
        public bool IsVisible
        {
            get
            {
                return isVisible;
            }
            set
            {
                if(isVisible != value)
                {
                    isVisible = value;
                    OnPropertyChanged("IsVisible");
                }
            }
        }


        private bool isRunning = false;
        public bool IsRunning
        {
            get
            {
                return isRunning;
            }
            set
            {
                if (isRunning != value)
                {
                    isRunning = value;
                    OnPropertyChanged("IsRunning");
                }
            }
        }

        private string busyText = "";
        public string BusyText
        {
            get
            {
                return busyText;
            }
            set
            {
                if (busyText != value)
                {
                    busyText = value;
                    OnPropertyChanged("BusyText");
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
