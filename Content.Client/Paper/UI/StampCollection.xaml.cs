using Robust.Client.AutoGenerated;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.Random;

namespace Content.Client.Paper.UI
{
    [GenerateTypedNameReferences]
    public sealed partial class StampCollection : Container
    {
        private List<StampWidget> _stamps = new();

        public int PlacementSeed;

        public StampCollection()
        {
            RobustXamlLoader.Load(this);
        }

        public void RemoveStamps()
        {
            _stamps.Clear();
        }

        public void AddStamp(StampWidget s)
        {
            _stamps.Add(s);
            AddChild(s);
        }

        protected override Vector2 ArrangeOverride(Vector2 finalSize)
        {
            var random = new Random(PlacementSeed);
            var r = (finalSize * 0.5f).Length;
            var dtheta = -MathHelper.DegreesToRadians(90);
            var theta0 = random.Next(0, 3) * dtheta;
            var thisCenter = PixelSizeBox.TopLeft + finalSize * UIScale * 0.5f;

            for (var i = 0; i < _stamps.Count; i++)
            {
                var stampOrientation = MathHelper.DegreesToRadians((random.NextFloat() - 0.5f) * 10.0f) ;
                _stamps[i].Orientation = stampOrientation;

                var theta = theta0 + dtheta * 0.5f + dtheta * i + (i > 4 ? MathF.Log(1 + i / 4) * dtheta : 0); // There is probably a better way to lay these out, to minimize overlaps
                var childCenterOnCircle = thisCenter;
                if (i > 0)
                {
                    // First stamp can go in the center. Subsequent stamps have to find space.
                    childCenterOnCircle += new Vector2(MathF.Cos(theta), MathF.Sin(theta)) * r * UIScale;
                }

                var childHeLocal = _stamps[i].DesiredPixelSize * 0.5f;
                var c = childHeLocal * MathF.Abs(MathF.Cos(stampOrientation));
                var s = childHeLocal * MathF.Abs(MathF.Sin(stampOrientation));
                var childHePage = new Vector2(c.X + s.Y, s.X + c.Y);
                var controlBox = new UIBox2(PixelSizeBox.TopLeft, PixelSizeBox.TopLeft + finalSize * UIScale);
                var clampedCenter = Clamp(Shrink(controlBox, childHePage), childCenterOnCircle);
                var realPosition = clampedCenter - childHePage;
                _stamps[i].ArrangePixel(new UIBox2i(ToI(realPosition), ToI(realPosition) + _stamps[i].DesiredPixelSize));
            }

            return finalSize;
        }

        private Vector2i ToI(Vector2 v)
        {
            return new Vector2i((int)v.X, (int)v.Y);
        }

        private UIBox2 Shrink(UIBox2 box, Vector2 shrinkHe)
        {
            return new UIBox2(box.TopLeft + shrinkHe, box.BottomRight - shrinkHe);
        }

        private Vector2 Clamp(UIBox2 box, Vector2 point)
        {
            return Vector2.ComponentMin(box.BottomRight, Vector2.ComponentMax(box.TopLeft, point));
        }
    }
}
