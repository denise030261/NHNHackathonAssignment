using System;
using UnityEngine;

namespace NHNHackathon.Items
{
    [Serializable]
    public sealed class PaperPageDefinition
    {
        [SerializeField, TextArea(5, 14)] private string text;
        [SerializeField, Tooltip("Optional image displayed above the page text.")]
        private Sprite image;

        public string Text => text;
        public Sprite Image => image;
    }
}
