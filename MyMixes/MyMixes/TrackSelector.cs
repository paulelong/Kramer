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
                if (((Track)item).isProject)
                {

                }
                return ((Track)item).isProject ? ProjectTemplate : TrackTemplate;
            }
            else
            {
                return ProjectTemplate;
            }
        }
    }

}