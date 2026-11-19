using System.Windows;
using System.Windows.Controls;

namespace Ikiz.App.Controls;

public sealed class ResultTemplateSelector : DataTemplateSelector
{
    public DataTemplate? GroupTemplate { get; set; }
    public DataTemplate? FileTemplate { get; set; }

    public override DataTemplate? SelectTemplate(object item, DependencyObject container)
        => item is ViewModels.GroupHeaderVm ? GroupTemplate : FileTemplate;
}
