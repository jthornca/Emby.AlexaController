﻿namespace AlexaController.Alexa.RequestData.Model
{
    public class User
    {
        public string userId { get; set; }
        public string accessToken { get; set; }
        public Permissions permissions { get; set; }
    }
}
