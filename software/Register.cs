// Author: Leonardo Tazzini (http://electro-logic.blogspot.it)

using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Text.Json.Serialization;

namespace CameraVision;

public partial class Register : ObservableObject
{
    [property: JsonConverter(typeof(HexStringJsonConverter))]
    [ObservableProperty]
    UInt16 _address;

    [property: JsonConverter(typeof(HexStringJsonConverter))]
    [ObservableProperty]
    UInt16 _value;

    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [ObservableProperty]
    string _description;

    public Register() { }

    public Register(UInt16 address, UInt16 value)
    {
        Address = address;
        Value = value;
        Description = null;
    }
    public Register(UInt16 address, UInt16 value, string description = "")
    {
        Address = address;
        Value = value;
        Description = description;
    }
    public override string ToString() => $"0x{Address.ToString("X")}: 0x{Value.ToString("X")}";
}