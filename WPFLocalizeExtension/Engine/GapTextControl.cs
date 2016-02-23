#region Copyright information
// <copyright file="GapTextControl.cs">
//     Licensed under Microsoft Public License (Ms-PL)
//     http://wpflocalizeextension.codeplex.com/license
// </copyright>
// <author>Peter Wendorff</author>
// <author>Uwe Mayer</author>
#endregion

using System.Collections.Generic;
using System.Linq;

namespace WPFLocalizeExtension.Engine
{
    using System;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Text.RegularExpressions;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Data;
    using System.Windows.Documents;
    using System.Windows.Markup;
    using System.Xml;

    /// <summary>
    /// A gap text control.
    /// </summary>
    // TODO: proper handling of \n in string contents
    [TemplatePart(Name = PART_TextBlock, Type = typeof(TextBlock))]
    public class GapTextControl : Control
    {
        public enum ReplacementStrategy
        {
            /// <summary>
            /// Do not replace any gap when it's duplicated
            /// </summary>
            ReplaceNothingOnDuplicates,
            /// <summary>
            /// Always replace only the first item
            /// </summary>
            ReplaceFirst,
            /// <summary>
            /// Try to replace any gap. Throw an Exception on failure.
            /// </summary>
            ReplaceAllOrThrowException,
            /// <summary>
            /// Replace all. If it fails on subsequent replacements, keep the first replacement, 
            /// but use <see cref="ReplaceNothingOnDuplicates"/> for those.
            /// </summary>
            ReplaceAllOrFirst,
            /// <summary>
            /// Throw exception whenever the same FrameworkElement is replaced more than once.
            /// </summary>
            ThrowExecption
        }

        #region Dependency Properties
        /// <summary>
        /// This property is the string that may contain gaps for controls.
        /// </summary>
        public static readonly DependencyProperty FormatStringProperty = DependencyProperty.Register(
            nameof(FormatString),
            typeof(string),
            typeof(GapTextControl),
            new PropertyMetadata(string.Empty, OnFormatStringChanged));

        /// <summary>
        /// If this property is set to true there is no error thrown 
        /// when the FormatString contains less gaps than placeholders are available.
        /// Missing placeholders for available elements may be a problem, 
        /// as something else may refer to the element in a binding e.g. by name, 
        /// but the element is not available in the visual tree.
        /// 
        /// As an example consider a submit button would be missing due to a missing placeholder in the FormatString.
        /// </summary>
        public static readonly DependencyProperty IgnoreLessGapsProperty = DependencyProperty.Register(
            nameof(IgnoreLessGaps),
            typeof(bool),
            typeof(GapTextControl),
            new PropertyMetadata(false));

        /// <summary>
        /// If this property is true, any FormatString that refers to the same string item multiple times produces an exception.
        /// </summary>
        public static readonly DependencyProperty DuplicateReplacementStrategyProperty = DependencyProperty.Register(
            nameof(DuplicateReplacementStrategy),
            typeof(ReplacementStrategy),
            typeof(GapTextControl),
            new PropertyMetadata(ReplacementStrategy.ReplaceAllOrThrowException));

        /// <summary>
        /// property that stores the items to be inserted into the gaps.
        /// any item that can be inserted as such into the TextBox get's inserted itself. 
        /// All other items are converted to Text using their ToString() implementation.
        /// </summary>
        public static readonly DependencyProperty GapsProperty = DependencyProperty.Register(
            nameof(Gaps),
            typeof(ObservableCollection<object>),
            typeof(GapTextControl),
            new PropertyMetadata(default(ObservableCollection<object>), OnGapsChanged));

        private static void OnGapsChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
            // TODO: make sure there's an event handler on CollectionChanged!

            // re-assemble children:
            if (dependencyPropertyChangedEventArgs.OldValue != dependencyPropertyChangedEventArgs.NewValue)
            {
                var self = (GapTextControl)dependencyObject;
                self.OnContentChanged();
            }
        }
        #endregion

        #region Constructors
        static GapTextControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(GapTextControl), new FrameworkPropertyMetadata(typeof(GapTextControl)));
        }

        /// <summary>
        /// Creates a new instance.
        /// </summary>
        public GapTextControl()
        {
            Gaps = new ObservableCollection<object>();
            Gaps.CollectionChanged += (sender, args) => OnContentChanged();
        }
        #endregion

        #region Properties matching the DependencyProperties
        /// <summary>
        /// Gets or set the format string.
        /// </summary>
        public string FormatString
        {
            get { return (string)GetValue(FormatStringProperty); }
            set { SetValue(FormatStringProperty, value); }
        }

        public bool IgnoreLessGaps
        {
            get { return (bool)GetValue(IgnoreLessGapsProperty); }
            set { SetValue(IgnoreLessGapsProperty, value); }
        }

        public ReplacementStrategy DuplicateReplacementStrategy
        {
            get { return (ReplacementStrategy)GetValue(DuplicateReplacementStrategyProperty); }
            set { SetValue(DuplicateReplacementStrategyProperty, value); }
        }

        /// <summary>
        /// Gets or sets the gap collection.
        /// </summary>
        public ObservableCollection<object> Gaps
        {
            get { return (ObservableCollection<object>)GetValue(GapsProperty); }
            set { SetValue(GapsProperty, value); }
        }
        #endregion

        #region Constants
        /// <summary>
        /// Pattern to split the FormatString, see https://github.com/SeriousM/WPFLocalizationExtension/issues/78#issuecomment-163023915 for documentation ( TODO!!!)
        /// </summary>
        public const string RegexPattern = @"(.*?){(\d*)}";
        #endregion

        #region Constants for TemplateParts
        // ReSharper disable once InconsistentNaming
        private const string PART_TextBlock = "PART_TextBlock";
        #endregion

        #region Sub-Controls
        private TextBlock theTextBlock = new TextBlock();
        #endregion

        #region DependencyProperty changed event handlers
        private static void OnFormatStringChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue != e.NewValue)
            {
                var self = (GapTextControl)d;
                self.OnContentChanged();
            }
        }

        private T DeepCopy<T>(T obj)
            where T : DependencyObject
        {
            var xaml = XamlWriter.Save(obj);
            var stringReader = new StringReader(xaml);
            var xmlTextReader = new XmlTextReader(stringReader);
            var result = (T)XamlReader.Load(xmlTextReader);

            var enumerator = obj.GetLocalValueEnumerator();
            while (enumerator.MoveNext())
            {
                var dp = enumerator.Current.Property;
                var be = BindingOperations.GetBindingExpression(obj, dp);
                if (be?.ParentBinding?.Path != null)
                    BindingOperations.SetBinding(result, dp, be.ParentBinding);
            }

            return result;
        }

        private void OnContentChanged()
        {
            // Re-arrange the children:
            theTextBlock.Inlines.Clear();
            
            if (FormatString != null)
            {
                var matchedUpToIndex = 0;

                // store in a hashSet which gaps have been replaced already:
                var usedGaps = new HashSet<int>();

                // 1) determine which items are to be used as string and which are to be inserted as controls:
                // allowed according to https://msdn.microsoft.com/de-de/library/system.windows.documents.inlinecollection%28v=vs.110%29.aspx are
                // Inline, String (creates an implicit Run), UIElement (creates an implicit InlineUIContainer with the supplied UIElement inside), 
                if (Gaps != null)
                {
                    // get a set of indices that should not be replaced anywhere, used especially where nothing should be replaced on duplicates:
                    var indicesNotToReplace = DuplicateReplacementStrategy == ReplacementStrategy.ReplaceNothingOnDuplicates
                        ? GetDuplicateReplacements()
                        : new int[0];

                    var match = Regex.Match(FormatString, RegexPattern);
                    while (match.Success)
                    {
                        // Handle match here...
                        var wholeMatch = match.Groups[0].Value; // contains string and simple placeholder at the end.

                        // has still to be formatted as it may contain placeholders with format specifiers that are not found by the patteern.
                        // TODO or even better bound accordingly by lex:loc binding
                        var formatStringPartial = match.Groups[1].Value;
                        
                        // it's secure to parse an int here as this follows from the regex.
                        var itemIndex = int.Parse(match.Groups[2].Value);

                        matchedUpToIndex += wholeMatch.Length;

                        // get next match:
                        match = match.NextMatch();

                        // add the inlines:
                        // 1) the prefix that is formatted with the whole gaps parameters:
                        theTextBlock.Inlines.Add(string.Format(formatStringPartial, Gaps));

                        // Check availability of a classified gap.
                        if (Gaps.Count <= itemIndex)
                            continue;
                        // check if this index should be replaced at all:
                        if (indicesNotToReplace.Contains(itemIndex))
                            continue;

                        var gap = Gaps[itemIndex];

                        // get ReplacementStrategy for the gap to be replaced next:
                        var customReplacementStrategy = DuplicateReplacementStrategy;

                        var dependencyGap = gap as DependencyObject;
                        if (dependencyGap != null)
                        {
                            customReplacementStrategy = Gap.GetReplacementStrategy(dependencyGap) ?? DuplicateReplacementStrategy;
                        }

                        // 2) the item encoded in the placeholder:
                        // apply the substitution depending on the settings:
                        switch (customReplacementStrategy)
                        {
                            case ReplacementStrategy.ReplaceAllOrFirst:
                                try
                                {
                                    ReplaceGap(gap);
                                }
                                catch (Exception)
                                {
                                    if (usedGaps.Add(itemIndex))
                                    {
                                        // first replacement failed, so we throw the exception:
                                        throw;
                                    }
                                    // else it's a repeated replacement and we don't care.
                                }
                                break;
                            case ReplacementStrategy.ReplaceAllOrThrowException:
                                // don't try and catch exceptions, as  we throw an exception in any case, especially when they are due to duplicate gaps.
                                ReplaceGap(gap);
                                break;
                            case ReplacementStrategy.ReplaceFirst:
                                // only replace if we didn't before:
                                if (!usedGaps.Add(itemIndex))
                                {
                                    ReplaceGap(gap);
                                }
                                break;
                            case ReplacementStrategy.ReplaceNothingOnDuplicates:
                                // has been checked beforehand!
                                break;
                            case ReplacementStrategy.ThrowExecption:
                                if (!usedGaps.Add(itemIndex))
                                {
                                    throw new InvalidOperationException(
                                        $"ReplacementStrategy {ReplacementStrategy.ThrowExecption} forbids to use the same element twice.");
                                }
                                break;
                        }
                    }
                }

                // add the remaining part:
                theTextBlock.Inlines.Add(string.Format(FormatString.Substring(matchedUpToIndex), Gaps));

                InvalidateVisual();
            }
            else
            {
                throw new Exception("FormatString is not a string!");
            }
        }

        private int[] GetDuplicateReplacements()
        {
            var allReplacements = new List<int>();
            var match = Regex.Match(FormatString, RegexPattern);

            while (match.Success)
            {
                allReplacements.Add(int.Parse(match.Groups[2].Value));
                match.NextMatch();
            }

            // group by number, take only those where after taking one out any other is left:
            return allReplacements
                .GroupBy(i => i)
                .Where(group => group.Skip(1).Any())
                .Select(group => group.Key)
                .ToArray();
        }

        private void ReplaceGap(object gap)
        {
            if (gap is UIElement)
            {
                var uiElementItem = DeepCopy((UIElement)gap);
                theTextBlock.Inlines.Add(uiElementItem);
            }
            else if (gap is Inline)
            {
                var inlineItem = DeepCopy((Inline)gap);
                theTextBlock.Inlines.Add(inlineItem);
            }
            else if (gap != null)
                theTextBlock.Inlines.Add(gap.ToString());
        }

        #endregion

        #region Template stuff
        /// <summary>
        /// Will be called prior to display of the control.
        /// </summary>
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            AttachToVisualTree();
        }

        private void AttachToVisualTree()
        {
            if (Template != null)
            {
                var textBlock = Template.FindName(PART_TextBlock, this) as TextBlock;
                
                // ReSharper disable once PossibleUnintendedReferenceComparison
                if (textBlock != theTextBlock)
                {
                    theTextBlock = textBlock;
                    OnContentChanged();
                }
            }
        }
        #endregion
    }
}