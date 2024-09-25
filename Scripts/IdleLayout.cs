using Godot;

namespace TCGHandLayoutPlugin.Scripts
{
    public partial class IdleLayout : Control
    {
        public void SetDynamicRadius(Layout layout, bool dynamicRadius, bool value){
            if (dynamicRadius == value){
                return;
            }
            layout._dynamicRadius = value;
            layout.HandLayout._needRecalculateCurve = true;
            layout.NotifyPropertyListChanged();
        }
        public void SetDynamicRadiusFactor(Layout layout, float value){
            layout._dynamicRadiusFactor = value;
            layout.HandLayout._needRecalculateCurve = true;
            layout.LayoutService.ResetPositionsIfInTree(layout);
        }
        public void SetRadius(Layout layout, float value){
            layout._cardRadius = value;
            layout.LayoutService.ResetPositionsIfInTree(layout);
        }
        public void SetCirclePercentage(Layout layout, float value){
            layout._circlePercentage = value;
            layout.LayoutService.ResetPositionsIfInTree(layout);
        }
    }    
}

