using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace CyberLab.Resources.Controls
{
    [TemplatePart(Name = "PART_ItemsHolder", Type = typeof(Panel))]
    public class CachedTabControl : TabControl
    {
        private Panel _itemsHolderPanel;

        public CachedTabControl()
        {
            this.SelectionChanged += OnSelectionChanged;
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            _itemsHolderPanel = GetTemplateChild("PART_ItemsHolder") as Panel;
            UpdateSelectedItem();
        }

        private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.Source != this) return;
            UpdateSelectedItem();
        }

        private void UpdateSelectedItem()
        {
            if (_itemsHolderPanel == null) return;

            // Generate content for each item and hide/show them
            for (int i = 0; i < Items.Count; i++)
            {
                var item = Items[i];
                var isSelected = (i == SelectedIndex);

                // Try to find if we already created a content for it
                var contentPresenter = FindChildContentPresenter(item);

                if (contentPresenter == null)
                {
                    if (isSelected)
                    {
                        contentPresenter = new ContentPresenter
                        {
                            Content = item,
                            ContentTemplate = this.ContentTemplate,
                            ContentTemplateSelector = this.ContentTemplateSelector,
                            ContentStringFormat = this.ContentStringFormat
                        };
                        _itemsHolderPanel.Children.Add(contentPresenter);
                    }
                }

                if (contentPresenter != null)
                {
                    contentPresenter.Visibility = isSelected ? Visibility.Visible : Visibility.Collapsed;
                }
            }
        }

        private ContentPresenter FindChildContentPresenter(object data)
        {
            foreach (UIElement child in _itemsHolderPanel.Children)
            {
                if (child is ContentPresenter cp && cp.Content == data)
                {
                    return cp;
                }
            }
            return null;
        }

        protected override void OnItemsChanged(NotifyCollectionChangedEventArgs e)
        {
            base.OnItemsChanged(e);
            
            if (_itemsHolderPanel == null) return;
            
            if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                _itemsHolderPanel.Children.Clear();
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove && e.OldItems != null)
            {
                foreach (var oldItem in e.OldItems)
                {
                    var cp = FindChildContentPresenter(oldItem);
                    if (cp != null) _itemsHolderPanel.Children.Remove(cp);
                }
            }
            
            UpdateSelectedItem();
        }
    }
}

