﻿

// ReSharper disable ComplexConditionExpression
// ReSharper disable InconsistentNaming

namespace AlexaController.Alexa.Speech
{
    public enum Domain
    {
        conversational,
        music,
        news
    }

    public enum StrengthBreak
    {
        weak,
        medium,
        strong
    }

    public enum Emphasis
    {
        /// <summary>
        /// Decrease the volume and speed up the speaking rate. The speech is softer and faster.
        /// </summary>
        reduced,

        /// <summary>
        ///  Increase the volume and slow down the speaking rate so the speech is louder and slower.
        /// </summary>
        strong,

        /// <summary>
        /// Increase the volume and slow down the speaking rate, but not as much as when set to strong. This is used as a default if level is not provided.
        /// </summary>
        moderate
    }

    public enum Rate
    {
        slow,
        medium,
        fast
    }

    public enum Effect
    {
        whispered
    }

    public enum Emotion
    {
        excited,
        disappointed,
        normal
    }

    public enum Intensity
    {
        low,
        medium,
        high
    }

    public class SpeechStyle
    {
        public static string SayInDomain(Domain domain, string text)                           => $"<amazon:domain name='{domain}'>{text}</amazon:domain>";

        public static string SayWithEffect(Effect effect, string text)                         => $"<amazon:effect name='{effect}'>{text}</amazon:effect>";

        public static string SayAsCardinal(string text)                                        => $"<say-as interpret-as='cardinal'>{text}</say-as>";

        public static string SpellOut(string text)                                             => $"<say-as interpret-as='spell-out'>{text}</say-as>.";

        public static string InsertTimedBreak(string intDurationSeconds)                       => $"<break time='{intDurationSeconds}s'/>";

        public static string InsertStrengthBreak(StrengthBreak strength)                       => $"<break strength='{strength}'/>";

        public static string SayWithEmphasis(string text, Emphasis emphasis)                   => $"<emphasis level='{emphasis}'>{text}</emphasis>";

        public static string SayWithEmotion(string text, Emotion emotion, Intensity intensity) => $"<amazon:emotion name='{emotion}' intensity='{intensity}'>{text}</amazon:emotion>";

        public static string SpeechRate(Rate rate, string text)                                => $"<prosody rate='{rate}'>{text}</prosody>";

        public static string ExpressiveInterjection(string text)                               => $"<say-as interpret-as='interjection'>{text}</say-as>";
    }
}