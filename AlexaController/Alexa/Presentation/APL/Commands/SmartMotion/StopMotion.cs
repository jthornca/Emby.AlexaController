﻿using System;
using System.Collections.Generic;
using System.Text;

namespace AlexaController.Alexa.Presentation.APL.Commands.SmartMotion
{
    public class StopMotion : ICommand
    {
        public object type => "SmartMotion:StopMotion";
    }
}