using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;

namespace MyMixes
{
    public class Track : BaseTrack
    {
        public string Project
        {
            get
            {
                return Path.GetFileName(ProjectPath);
            }
        }

        public string ProjectPath { get; set; }
        //public bool isProject { get; set; }
        public int TrackNum { get; set; }

        private bool cloudProviderSet = false;
        private CloudStorage.CloudProviders cloudProvider = CloudStorage.CloudProviders.NULL;
        public CloudStorage.CloudProviders CloudProvider
        {
            get
            {
                if (!cloudProviderSet)
                {
                    cloudProvider = PersistentData.GetProvider(Project, Name, LastModifiedDate.ToString());
                    if(cloudProvider != CloudStorage.CloudProviders.NULL)
                    {
                        cloudProviderSet = true;
                    }
                }

                return cloudProvider;
            }
            set
            {
                if(value != cloudProvider)
                {
                    if (value == CloudStorage.CloudProviders.NULL)
                    {
                        cloudProviderSet = false;
                        PersistentData.ResetProvider(Project, Name, LastModifiedDateString);
                    }
                    else
                    {
                        PersistentData.SetProvider(Project, Name, LastModifiedDateString, value);
                        cloudProviderSet = true;
                    }
                }
            }
        }

        public string cloudRoot = null;
        public string CloudRoot
        {
            get
            {
                if (string.IsNullOrEmpty(cloudRoot))
                {
                    cloudRoot = PersistentData.GetCloudRoot(Project, Name, LastModifiedDate.ToString());
                }

                return cloudRoot;
            }
            set
            {
                if(value == null)
                {
                    cloudRoot = null;
                    PersistentData.ResetCloudRoot(Project, Name, LastModifiedDate.ToString());
                }
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

        private bool trackPlaying = false;
        public bool TrackPlaying
        {
            get
            {
                return trackPlaying;
            }
            set
            {
                if(value != trackPlaying)
                {
                    trackPlaying = value;
                    OnPropertyChanged("PlayListImage");

                    if (trackPlaying)
                    {
                        playListImage = "PauseBt.png";
                    }
                    else
                    {
                        playListImage = "PlayBt.png";
                    }
                }
            }
        }

        private string playListImage = "PlayBt.png";
        public string PlayListImage
        {
            get
            {
                return playListImage;
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



        //protected virtual void OnPropertyChanged(string propertyName)
        //{
        //    var changed = PropertyChanged;
        //    if (changed != null)
        //    {
        //        PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        //    }
        //}
    }
}
