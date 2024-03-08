namespace PlayerDB.SettingsStorage;

public interface ISettingsStorage
{
    Task<DataModel.Settings> LoadSettings();

    Task StoreSettings(DataModel.Settings settings);
}