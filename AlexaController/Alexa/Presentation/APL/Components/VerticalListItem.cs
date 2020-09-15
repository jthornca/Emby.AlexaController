﻿namespace AlexaController.Alexa.Presentation.APL.Components
{
    public class VerticalListItem : Item
    {
        public object type => nameof(VerticalListItem);
        public string image         { get; set; }
        public string primaryText   { get; set; }
        public string secondaryText { get; set; }
        public string tertiaryText  { get; set; }
    }
}