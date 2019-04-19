using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Foundation;
using MyMixes;
using UIKit;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;
using yMixes.iOS;

[assembly: ExportRenderer(typeof(ExtendedViewCell), typeof(ExtendedViewCellRenderer))]
namespace yMixes.iOS
{
    public class ExtendedViewCellRenderer : ViewCellRenderer
    {
        public override UITableViewCell GetCell(Cell item, UITableViewCell reusableCell, UITableView tv)
        {
            var cell = base.GetCell(item, reusableCell, tv);
            var view = item as ExtendedViewCell;
            cell.SelectedBackgroundView = new UIView
            {
                BackgroundColor = view.SelectedBackgroundColor.ToUIColor(),
            };

            return cell;
        }

    }
}