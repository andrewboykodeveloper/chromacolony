using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Chromacolony.Game
{
    /// <summary>
    /// The crews' counts and the columns' "+N" labels: elements of the interface that follow the world, placed from the camera
    /// each frame.
    /// </summary>
    public sealed class CrewLabels
    {
        private readonly Camera _camera;
        private readonly Dictionary<int, Label> _counts = new Dictionary<int, Label>();
        private readonly List<Label> _more = new List<Label>();
        private VisualElement _panelRoot;
        private VisualElement _layer;

        public CrewLabels(Camera camera)
        {
            _camera = camera;
        }

        /// <summary>
        /// Shows the labels in a layer of the interface; called again with the new layer whenever the interface is rebuilt.
        /// </summary>
        public void Attach(VisualElement panelRoot, VisualElement layer)
        {
            _panelRoot = panelRoot;
            _layer = layer;
            foreach (Label more in _more)
            {
                AddToLayer(more);
            }

            foreach (Label count in _counts.Values)
            {
                AddToLayer(count);
            }
        }

        public void AddColumns(int columns)
        {
            for (int c = 0; c < columns; c++)
            {
                _more.Add(Add(UiClasses.CrewMore));
            }
        }

        public void AddCrew(int crew)
        {
            _counts.Add(crew, Add(UiClasses.CrewCount));
        }

        public void ShowCount(int crew, string text)
        {
            _counts[crew].text = text;
        }

        public void PlaceCount(int crew, Vector3 world, float scale, bool shown)
        {
            Label count = _counts[crew];
            Vector2 point = WorldToPanel.Point(_camera, world, _panelRoot);
            count.style.left = point.x;
            count.style.top = point.y;
            count.style.scale = new Scale(new Vector3(scale, scale, 1f));
            count.style.opacity = shown ? 1f : 0f;
        }

        public void ShowMore(int column, int hidden, Vector3 world)
        {
            Label more = _more[column];
            more.text = hidden > 0 ? "+" + hidden : string.Empty;
            more.EnableInClassList(UiClasses.Hidden, hidden <= 0);
            Vector2 corner = WorldToPanel.Point(_camera, world, _panelRoot);
            more.style.left = corner.x;
            more.style.top = corner.y;
        }

        public void RemoveCrew(int crew)
        {
            _counts[crew].RemoveFromHierarchy();
            _counts.Remove(crew);
        }

        public void Clear()
        {
            foreach (Label count in _counts.Values)
            {
                count.RemoveFromHierarchy();
            }

            foreach (Label more in _more)
            {
                more.RemoveFromHierarchy();
            }

            _counts.Clear();
            _more.Clear();
        }

        private Label Add(string className)
        {
            var label = new Label();
            label.AddToClassList(className);
            label.pickingMode = PickingMode.Ignore;
            _layer.Add(label);
            return label;
        }

        private void AddToLayer(VisualElement label)
        {
            if (label.parent != _layer)
            {
                _layer.Add(label);
            }
        }
    }
}
