﻿namespace AlexaController.Alexa.Model.RequestData
{
    public class Header
    {
        public string @namespace { get; set; }
        public string name { get; set; }
        public string messageId { get; set; }
        public string dialogRequestId { get; set; }
    }
}