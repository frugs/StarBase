namespace PlayerDB.Core.Settings;

public interface ISettingsService
{
    IObservable<(string SettingsKey, DataModel.Settings Change)> SettingsChangedObservable { get; }

    Task<DataModel.Settings> GetCurrentSettings();

    Task ApplySettingsChange(string settingsKey, DataModel.Settings change);

    Task ClearSettingsKey(string settingsKey);
}