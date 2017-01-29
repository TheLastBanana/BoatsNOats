using System.Collections;
using System.Collections.Generic;
using System.Xml;
using UnityEngine;
using UnityEngine.UI;

public class TypewriterText : MonoBehaviour {
    Text text;
    public TextAsset dialog;

	// Use this for initialization
	void Start ()
    {
        // Get the text box for later use
        text = GetComponent<Text>();

        // skip BOM (magic incantation that avoids an error with unity and xml files?)
        //System.IO.StringReader stringReader = new System.IO.StringReader(dialog.text);
        //stringReader.Read(); // This reads the first character which is the BOM
        //XmlReader reader = XmlReader.Create(stringReader);
        XmlDocument doc = new XmlDocument();
        //doc.Load(reader);
        doc.LoadXml(dialog.text);
        XmlNodeList nodes = doc.SelectNodes("//dialog/text");
        foreach (XmlNode node in nodes)
        {
            Debug.Log(node.InnerXml);
        }
	}
	
	// Update is called once per frame
	void Update ()
    {
	}
}
