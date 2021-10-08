using DaggerfallWorkshop.Game.UserInterface;
using DaggerfallWorkshop.Game.UserInterfaceWindows;
using DaggerfallWorkshop.Utility;

namespace Assets.MacadaynuMods.HiddenMapLocations
{
    public class HiddenMapLocationsWindow : DaggerfallTravelMapWindow
    {
        public HiddenMapLocationsWindow(IUserInterfaceManager uiManager)
            : base(uiManager)
        {
        }

        protected override bool checkLocationDiscovered(ContentReader.MapSummary summary)
        {
            return HiddenMapLocationsMod.discoveredMapSummaries.Contains(summary)
                || HiddenMapLocationsMod.revealedLocationTypes.Contains(summary.LocationType);
        }
    }
}