using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;

namespace MyMixes
{
    public class Track : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public string Name { get; set; }
        public string FullPath { get; set; }
        public bool isProject { get; set; }

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
                OnPropertyChanged("Order");
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
