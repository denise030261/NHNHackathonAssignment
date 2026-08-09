using System;
using System.Text;
using UnityEngine;

namespace NHNHackathon.Items
{
    [Serializable]
    public sealed class PaperPageDefinition
    {
        [SerializeField, TextArea(5, 14), Tooltip("Wrap text with @ symbols to display it in bold. Example: @Important text@")]
        private string text;
        [SerializeField, Tooltip("Optional image displayed above the page text.")]
        private Sprite image;

        public string Text => text;
        public Sprite Image => image;
    }

    public static class PaperTextMarkup
    {
        public static string FormatForDisplay(string source)
        {
            if (string.IsNullOrEmpty(source) || source.IndexOf('@') < 0)
            {
                return source ?? string.Empty;
            }

            StringBuilder result = new(source.Length + 16);
            int currentIndex = 0;
            while (currentIndex < source.Length)
            {
                int openingIndex = source.IndexOf('@', currentIndex);
                if (openingIndex < 0)
                {
                    result.Append(source, currentIndex, source.Length - currentIndex);
                    break;
                }

                result.Append(source, currentIndex, openingIndex - currentIndex);
                int closingIndex = source.IndexOf('@', openingIndex + 1);
                if (closingIndex <= openingIndex + 1)
                {
                    result.Append('@');
                    currentIndex = openingIndex + 1;
                    continue;
                }

                result.Append("<b>");
                result.Append(source, openingIndex + 1, closingIndex - openingIndex - 1);
                result.Append("</b>");
                currentIndex = closingIndex + 1;
            }

            return result.ToString();
        }
    }
}
