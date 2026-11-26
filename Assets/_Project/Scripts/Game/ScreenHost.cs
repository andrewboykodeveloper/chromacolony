using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Chromacolony.Game
{
    public sealed class ScreenHost
    {
        public const string ScreenClass = "screen";
        public const string DialogClass = "dialog";
        public const string HiddenClass = "hidden";
        public const string OpenClass = "open";

        private readonly Dictionary<string, VisualElement> _screens = new Dictionary<string, VisualElement>();
        private readonly Dictionary<string, VisualElement> _dialogs = new Dictionary<string, VisualElement>();

        public ScreenHost(VisualElement root)
        {
            root.Query<VisualElement>(className: ScreenClass).ForEach(e => Add(_screens, e));
            root.Query<VisualElement>(className: DialogClass).ForEach(e => Add(_dialogs, e));
            foreach (VisualElement element in _screens.Values)
            {
                element.AddToClassList(HiddenClass);
            }

            foreach (VisualElement element in _dialogs.Values)
            {
                element.AddToClassList(HiddenClass);
            }
        }

        public string CurrentScreen { get; private set; }

        public string CurrentDialog { get; private set; }

        public void Show(string screen)
        {
            VisualElement target = Find(_screens, screen);
            CloseDialog();
            foreach (VisualElement element in _screens.Values)
            {
                element.EnableInClassList(HiddenClass, element != target);
            }

            CurrentScreen = screen;
        }

        public void Open(string dialog)
        {
            VisualElement target = Find(_dialogs, dialog);
            CloseDialog();
            target.RemoveFromClassList(HiddenClass);

            target.schedule.Execute(() => target.AddToClassList(OpenClass)).StartingIn(16);
            CurrentDialog = dialog;
        }

        public void CloseDialog()
        {
            if (CurrentDialog == null)
            {
                return;
            }

            VisualElement element = _dialogs[CurrentDialog];
            element.RemoveFromClassList(OpenClass);
            element.AddToClassList(HiddenClass);
            CurrentDialog = null;
        }

        private static void Add(Dictionary<string, VisualElement> map, VisualElement element)
        {
            if (string.IsNullOrEmpty(element.name))
            {
                throw new InvalidOperationException($"a {string.Join(" ", element.GetClasses())} element needs a name in the UXML");
            }

            map.Add(element.name, element);
        }

        private static VisualElement Find(Dictionary<string, VisualElement> map, string name)
        {
            return map.TryGetValue(name, out VisualElement element) ? element : throw new ArgumentException($"no such screen or dialog in the UXML: {name}");
        }
    }
}
