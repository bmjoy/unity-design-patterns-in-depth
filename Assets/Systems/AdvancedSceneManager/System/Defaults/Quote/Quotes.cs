using System;
using System.Collections.Generic;
using UnityEngine;

public class Quotes : MonoBehaviour
{
    public List<Quote> quoteList;

    [Serializable]
    public class Quote
    {
        public string name;
        public string quote;
    }
}