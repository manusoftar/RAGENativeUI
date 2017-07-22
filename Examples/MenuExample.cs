namespace Examples
{
    using System.Drawing;

    using Rage;
    using Rage.Attributes;
    using Graphics = Rage.Graphics;

    using RAGENativeUI;
    using RAGENativeUI.Menus;
    using RAGENativeUI.Menus.Rendering;
    using RAGENativeUI.Utility;

    internal static class MenuExample
    {
        [ConsoleCommand(Name = "MenuExample", Description = "Example showing RAGENativeUI menu API")]
        private static void Command()
        {
            MenusManager menusMgr = new MenusManager();

            Menu menu = new Menu("title", "SUBTITLE");
            menu.Location = new PointF(480, 17);

            menu.Items.Add(new MenuItem("item #0"));
            menu.Items.Add(new MenuItemCheckbox("cb #0") { State = MenuItemCheckboxState.Empty });
            menu.Items.Add(new MenuItemCheckbox("cb #1") { State = MenuItemCheckboxState.Cross });
            menu.Items.Add(new MenuItemCheckbox("cb #2") { State = MenuItemCheckboxState.Tick });
            menu.Items.Add(new MenuItemEnumScroller("enum scroller #0", typeof(GameControl)));
            menu.Items.Add(new MenuItemEnumScroller<GameControl>("enum scroller #1"));
            menu.Items.Add(new MenuItemNumericScroller("num scroller #0"));
            menu.Items.Add(new MenuItemNumericScroller("num scroller #1") { ThousandsSeparator = true, Minimum = -50000.0m, Maximum = 50000.0m, Value = 0.0m, Increment = 100.0m });
            menu.Items.Add(new MenuItemNumericScroller("num scroller #2") { Increment = 1.0m, Hexadecimal = true });
            menu.Items.Add(new MenuItemNumericScroller("num scroller #3") { Minimum = -1.0m, Maximum = 1.0m, Value = 0.0m, Increment = 0.00005m, DecimalPlaces = 5 });
            menu.Items.Add(new MenuItemListScroller("list scroller #0"));
            menu.Items.Add(new MenuItemListScroller("list scroller #1", new[] { "text #1", "other text #2", "and text #3" }));

            menu.SelectedIndexChanged += (s, oldIndex, newIndex) => { Game.DisplayHelp($"Selected index changed from #{oldIndex} to #{newIndex}"); };
            menu.VisibleChanged += (s, visible) => { Game.DisplayHelp($"Visible now: {visible}"); };

            for (int i = 0; i < menu.Items.Count; i++)
            {
                int idx = i;

                if (menu.Items[idx] is MenuItemCheckbox)
                {
                    (menu.Items[idx] as MenuItemCheckbox).StateChanged += (s, oldState, newState) => { Game.DisplayHelp($"Checkbox at index #{idx} state changed from {oldState} to {newState}"); };
                    continue;
                }

                if(menu.Items[idx] is MenuItemScroller)
                {
                    (menu.Items[idx] as MenuItemScroller).SelectedIndexChanged += (s, oldIndex, newIndex) => { Game.DisplayHelp($"Scroller at index #{idx} selected index changed from #{oldIndex} to #{newIndex}"); };
                }

                menu.Items[idx].Activated += (s, origin) => { Game.DisplayHelp($"Activated item at index #{idx}"); };
            }

            menusMgr.Menus.Add(menu);

            MenuSkin redSkin = new MenuSkin(@"RAGENativeUI Resources\menu-red-skin.png");
            GameFiber.StartNew(() =>
            {
                while (true)
                {
                    GameFiber.Yield();
                    
                    if(Game.IsKeyDown(System.Windows.Forms.Keys.Y))
                    {
                        if (menu.Skin == MenuSkin.DefaultSkin)
                            menu.Skin = redSkin;
                        else
                            menu.Skin = MenuSkin.DefaultSkin;
                    }

                    if (Game.IsKeyDown(System.Windows.Forms.Keys.T))
                    {
                        if(menusMgr.IsAnyMenuVisible)
                        {
                            menusMgr.HideAllMenus();
                        }
                        else
                        {
                            menusMgr.ShowAllMenus();
                        }
                    }
                    
                    if (Game.IsKeyDown(System.Windows.Forms.Keys.Add))
                        menu.MaxItemsOnScreen++;
                    else if (Game.IsKeyDown(System.Windows.Forms.Keys.Subtract) && menu.MaxItemsOnScreen > 0)
                        menu.MaxItemsOnScreen--;
                }
            });
        }
    }
}

