using Godot;

namespace TCGHandLayoutPlugin.Scripts
{
    public partial class HoverLayout : Control
    {
        public void SetEnableHover(Layout layout, bool value){
           layout._enableHover = value;
           layout.HandLayout._needRecalculateCurve = true;
           layout.NotifyPropertyListChanged();
        }
        public void SetHoveredIndex(Layout layout, int value){
            layout._hoveredIndex = value;
            layout.HandLayout._needRecalculateCurve = true;
            if (layout._hoveredIndex >= layout.GetChildCount()){
                return;
            }
            layout.LayoutService.ResetPositionsIfInTree(layout);
            Control card = null;
            if (layout._hoveredIndex >= 0){
                layout.HoverSound?.Play();
                card = (Control) layout.GetChildren()[layout._hoveredIndex];
                layout.EmitSignal(Layout.SignalName.CardHovered, card, layout._hoveredIndex);
            }
            else{
                layout.EmitSignal(Layout.SignalName.CardsUnhovered);
            }
        }
        public void SetHoverPadding(Layout layout, float value){
            layout._hoverPadding = value;
            layout.HandLayout._needRecalculateCurve = true;
            layout.LayoutService.ResetPositionsIfInTree(layout);
        }
        public void SetHoverScale(Layout layout, Vector2 value){
            layout._hoverScale = value;
            layout.HandLayout._needRecalculateCurve = true;
            layout.LayoutService.ResetPositionsIfInTree(layout);
        }
        public void SetHoverRelativePosition(Layout layout, Vector2 value){
            layout._hoverRelativePosition = value;
            layout.HandLayout._needRecalculateCurve = true;
            layout.LayoutService.ResetPositionsIfInTree(layout);
        }
    }
}

