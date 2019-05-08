using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace MyMixes
{
    public class SliderX : Slider
    {
        public static readonly BindableProperty AccurateValueProperty =
            BindableProperty.Create<SliderX, Double>(s => s.AccurateValue, 0, BindingMode.TwoWay, coerceValue: CoerceValue);

        public double AccurateValue
        {
            get { return (double)this.GetValue(SliderX.AccurateValueProperty); }
            set { this.SetValue(SliderX.AccurateValueProperty, value); }
        }

        private bool locked;

        protected override async void OnPropertyChanged(string propertyName = null)
        {
            base.OnPropertyChanged(propertyName);

            if (propertyName == SliderX.AccurateValueProperty.PropertyName)
            {
                this.locked = true;
                this.Value = this.AccurateValue;
                await Task.Yield(); // fix for deferred setter
                this.locked = false;
            }
            else if (propertyName == Slider.ValueProperty.PropertyName && !this.locked)
            {
                this.AccurateValue = this.Value;
            }
        }

        // to make sure Value in Range [Minimum, Maximum] (copy-paste from the Slider implementation)
        private static double CoerceValue(BindableObject bindable, double value)
        {
            var s = (Slider)bindable;
            return Math.Min(s.Maximum, Math.Max(value, s.Minimum));
        }
    }
}
