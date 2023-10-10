// Author: Leonardo Tazzini (http://electro-logic.blogspot.it)

using CommunityToolkit.Mvvm.ComponentModel;
using System;
public partial class Register : ObservableObject
{
    [ObservableProperty]
    UInt16 _address;
    [ObservableProperty]
    string _value;
    [ObservableProperty]
    string _description;
    public Register(UInt16 address, UInt16 value)
    {
        _address = address;
        _value = value.ToString("X");
        _description = "";
    }
    public Register(UInt16 address, string value, string description="")
    {
        _address = address;
        _value = value;
        _description = description;
    }
    //public ushort Address
    //{
    //    get
    //    {
    //        return _address;
    //    }
    //    set
    //    {
    //        _address = value;
    //        OnPropertyChanged();
    //    }
    //}

    //public string Value
    //{
    //    get
    //    {
    //        return _value;
    //    }
    //    set
    //    {
    //        _value = value;
    //        OnPropertyChanged();
    //    }
    //}

    //public string Description
    //{
    //    get
    //    {
    //        return _description;
    //    }
    //    set
    //    {
    //        _description = value;
    //        OnPropertyChanged();
    //    }
    //}
}
