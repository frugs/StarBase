using System.Threading.Tasks;

namespace PlayerDB.App.Settings;

public interface ISettingsPageViewModel
{
    Task ClearPlayersAndBuildOrders();

    Task ResetSettings();
}