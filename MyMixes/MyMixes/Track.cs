using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace MyMixes
{
    public class Track : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public string Name { get; set; }
        public string FullPath { get; set; }
        public string ProjectPath { get; set; }
        public bool isProject { get; set; }
        public int TrackNum { get; set; }
        public string CloudProvider { get; set; }
        public string CloudRoot { get; set; }

        public string Project
        {
            get
            {
                return Path.GetFileName(ProjectPath);
            }
        }

        private string orderButtonText = "+";
        public string OrderButtonText
        {
            get
            {
                return orderButtonText;
            }
            set
            {
                if(orderButtonText != value)
                {
                    orderButtonText = value;
                    OnPropertyChanged("OrderButtonText");
                }
            }
        }

        private string updateListImage = "AddBt2.png";
        public string UpdateListImage
        {
            get
            {
                return updateListImage;
            }
        }

        public bool ReadyToAdd
        {
            set
            {
                if(value)
                {
                    updateListImage = "AddBt2.png";
                }
                else
                {
                    updateListImage = "RemoveBt2.png";
                }
                OnPropertyChanged("UpdateListImage");
            }
        }


        private int order = 0;

        public string Order
        {
            get
            {
                if(order == 0)
                {
                    return "*";
                }
                else
                {
                    return order.ToString();
                }
            }
            set
            {
                int neworder = 0;
                if(int.TryParse(value, out neworder))
                {
                    order = neworder;
                    OnPropertyChanged("Order");
                }
            }

        }

        public int OrderVal
        {
            get
            {
                return order;
            }
            set
            {
                order = value;
                OnPropertyChanged("Order");

            }
        }

        public void Print()
        {
            
            Debug.Print("{2} {0} {1}\n", Name, FullPath, isProject ? "Project" : "Track");
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            var changed = PropertyChanged;
            if (changed != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
