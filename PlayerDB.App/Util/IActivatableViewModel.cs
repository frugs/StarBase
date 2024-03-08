using System.ComponentModel;

namespace PlayerDB.App.Util;

public interface IActivatableViewModel : INotifyPropertyChanged
{
    bool IsActive { get; set; }
}