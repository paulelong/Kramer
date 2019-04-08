using System.Diagnostics;
using Xamarin.Forms;


namespace MyMixes
{
    public class TrackDataTemplateSelector : DataTemplateSelector
    {
        public DataTemplate ProjectTemplate { get; set; }

        public DataTemplate TrackTemplate { get; set; }

        protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
        {
            if(item != null)
            {

#if GORILLA
                Newtonsoft.Json.Linq.JObject jo = (Newtonsoft.Json.Linq.JObject)item;
                Debug.Print("JO={0}\n", jo.ToString());
                if(jo.Count >= 0)
                {
                    if ((string)jo["isProject"] == "1")
                    {
                        return ProjectTemplate;
                    }
                    else
                    {
                        return TrackTemplate;
                    }

                }
                return ProjectTemplate;
#else
                if(item is Track)
                {
                    return ((Track)item).isProject ? ProjectTemplate : TrackTemplate;
                }
                else
                {
                    return ProjectTemplate;
                }
#endif
                //else
                //{
                //    Newtonsoft.Json.Linq.JObject jo = (Newtonsoft.Json.Linq.JObject)item;
                //    if((string)jo["isproject"] == "1")
                //    {
                //        return ProjectTemplate;
                //    }
                //    else
                //    {
                //        return TrackTemplate;
                //    }
                //}
            }
            else
            {
                return ProjectTemplate;
            }
        }
    }

}