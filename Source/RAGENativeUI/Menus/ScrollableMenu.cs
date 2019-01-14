namespace RAGENativeUI.Menus
{
    using System;
    using System.Collections.Generic;

    // note: do not modifiy the Items collection directly(Add, Remove, Clear,...), changes will be overwritten once UpdateItems() is called
    public class ScrollableMenu : Menu
    {
        private ScrollableMenuPagesCollection pages;

        public event TypedEventHandler<ScrollableMenu, SelectedPageChangedEventArgs> SelectedPageChanged;

        public ScrollableMenuPagesCollection Pages
        {
            get
            {
                if (pages == null)
                {
                    pages = new ScrollableMenuPagesCollection(this);
                }
                return pages;
            }
        }

        public ScrollableMenuPage SelectedPage
        {
            get => (ScrollerItem.SelectedIndex >= 0 && ScrollerItem.SelectedIndex < Pages.Count) ? Pages[ScrollerItem.SelectedIndex] : null;
            set => ScrollerItem.SelectedIndex = Pages.IndexOf(value);
        }

        public MenuItemScroller ScrollerItem { get; }

        public ScrollableMenu(string title, string subtitle, string scrollerItemText) : base(title, subtitle)
        {
            Throw.IfNull(scrollerItemText, nameof(scrollerItemText));

            ScrollerItem = new MenuItemPagesScroller(scrollerItemText, Pages);
            ScrollerItem.SelectedIndexChanged += OnScrollerSelectedIndexChanged;
            Items.Add(ScrollerItem);
        }

        protected override void OnVisibleChanged(VisibleChangedEventArgs e)
        {
            if (e.IsVisible)
            {
                UpdateItems();
            }

            base.OnVisibleChanged(e);
        }

        protected virtual void OnSelectedPageChanged(SelectedPageChangedEventArgs e)
        {
            SelectedPageChanged?.Invoke(this, e);
        }

        private void OnScrollerSelectedIndexChanged(MenuItemScroller sender, SelectedIndexChangedEventArgs e)
        {
            int oldIndex = e.OldIndex;
            int newIndex = e.NewIndex;
            ScrollableMenuPage oldPage = (oldIndex >= 0 && oldIndex < Pages.Count) ? Pages[oldIndex] : null;
            ScrollableMenuPage newPage = (newIndex >= 0 && newIndex < Pages.Count) ? Pages[newIndex] : null;
            OnPropertyChanged(nameof(SelectedPage));
            OnSelectedPageChanged(new SelectedPageChangedEventArgs(oldIndex, newIndex, oldPage, newPage));
            UpdateItems();
        }

        // clears the items collection and adds the scroller item and the items from the current page
        internal void UpdateItems()
        {
            int index = ScrollerItem.SelectedIndex;
            Items.Clear();
            Items.Add(ScrollerItem);
            if (index >= 0 && index < Pages.Count)
            {
                foreach (MenuItem item in Pages[index].Items)
                {
                    Items.Add(item);
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (!IsDisposed && disposing)
            {
                ScrollerItem.SelectedIndexChanged -= OnScrollerSelectedIndexChanged;
            }

            base.Dispose(disposing);
        }


        private sealed class MenuItemPagesScroller : MenuItemScroller
        {
            private ScrollableMenuPagesCollection pages;

            public ScrollableMenuPagesCollection Pages { get { return pages; } set { Throw.IfNull(value, nameof(value)); pages = value; } }

            public MenuItemPagesScroller(string text, ScrollableMenuPagesCollection pages) : base(text, String.Empty)
            {
                Throw.IfNull(pages, nameof(pages));

                Pages = pages;
            }

            public override int GetOptionsCount()
            {
                return pages.Count;
            }

            public override string GetSelectedOptionText()
            {
                int index = SelectedIndex;
                if (index >= 0 && index < Pages.Count)
                    return Pages[index].Text;
                return "-";
            }
        }
    }


    public sealed class ScrollableMenuPage
    {
        private string text;
        private List<MenuItem> items;

        public string Text
        {
            get => text;
            set
            {
                Throw.IfNull(value, nameof(value));
                text = value;
            }
        }

        public List<MenuItem> Items
        {
            get => items;
            set
            {
                Throw.IfNull(value, nameof(value));
                items = value;
            }
        }

        public ScrollableMenuPage(string text)
        {
            Throw.IfNull(text, nameof(text));

            Text = text;
            Items = new List<MenuItem>();
        }
    }


    public sealed class ScrollableMenuPagesCollection : BaseCollection<ScrollableMenuPage>
    {
        public ScrollableMenu Owner { get; }

        public ScrollableMenuPagesCollection(ScrollableMenu owner) : base()
        {
            Owner = owner;
        }

        public override void Add(ScrollableMenuPage item)
        {
            base.Add(item);
            Owner.UpdateItems();
        }

        public override void Insert(int index, ScrollableMenuPage item)
        {
            base.Insert(index, item);
            Owner.UpdateItems();
        }

        public override bool Remove(ScrollableMenuPage item)
        {
            bool b = base.Remove(item);
            if (b)
                Owner.UpdateItems();
            return b;
        }

        public override void RemoveAt(int index)
        {
            base.RemoveAt(index);
            Owner.UpdateItems();
        }

        public override void Clear()
        {
            base.Clear();
            Owner.UpdateItems();
        }
    }
}
