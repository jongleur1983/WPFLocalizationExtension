using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using WPFLocalizeExtension.Providers;

namespace WPFLocalizeExtension.Engine
{
    public class Gap : DependencyObject
    {
        public static readonly DependencyProperty ReplacementStrategyProperty = DependencyProperty.RegisterAttached(
            nameof(GapTextControl.ReplacementStrategy),
            typeof(GapTextControl.ReplacementStrategy?),
            typeof(Gap),
            new UIPropertyMetadata(default(GapTextControl.ReplacementStrategy?)));

        public static void SetReplacementStrategy(DependencyObject obj, GapTextControl.ReplacementStrategy value)
        {
            obj.SetValue(ReplacementStrategyProperty, value);
        }

        public static GapTextControl.ReplacementStrategy? GetReplacementStrategy(DependencyObject obj)
        {
            return (GapTextControl.ReplacementStrategy?) obj.GetValue(ReplacementStrategyProperty);
        }
        
    }
}
