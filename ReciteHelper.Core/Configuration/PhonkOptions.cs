using System;
using System.Collections.Generic;
using System.Text;

namespace ReciteHelper.Core.Configuration;

public class PhonkOptions
{
    public bool EnablePhonk { get; set; } = false;
    public int WrongCount { get; set; } = 3;
}
