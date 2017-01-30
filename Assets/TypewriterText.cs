using System.Collections;
using System.Collections.Generic;
using System.Xml;
using UnityEngine;
using UnityEngine.UI;

class TagDef
{
    public string name { get; set; }
    public int start { get; set; }
    public int end { get; set; }
    public Dictionary<string, string> tagParams {get; private set; }

    public TagDef(string text, int start, int end)
    {
        // Get just the tag name and parse out the parameters
        tagParams = new Dictionary<string, string>();
        string[] tagValues = text.Split(' ');
        Debug.Assert(tagValues.Length >= 1, "Tag string doesn't have start?");
        this.name = tagValues[0];

        for (int i = 1; i < tagValues.Length; ++i)
        {
            string[] param = tagValues[i].Split('=');
            Debug.Assert(param.Length == 2, "xml tag param length not 2");
            tagParams[param[0]] = param[1];
        }


        this.start = start;
        this.end = end;
    }

    public override string ToString()
    {
        string ret = "TagDef(" + name + ", [";
        foreach (KeyValuePair<string, string> pair in tagParams)
            ret += pair.Key + "=" + pair.Value + " ";
        ret += "], " + start + ", " + end + ")";
        return ret ;
    }
}

class Dialog
{
    public string text { get; private set; }
    public List<TagDef> format { get; private set; }

    public Dialog(string text, List<TagDef> format)
    {
        this.text = text;
        this.format = format;
    }
}

public class TypewriterText : MonoBehaviour {
    Text text;
    public TextAsset dialogFile;
    List<Dialog> dialogs = new List<Dialog>();
    bool runText = false;
    bool started = false;

	// Use this for initialization
	void Start ()
    {
        // Get the text box for later use
        text = GetComponent<Text>();

        // Get the XML document
        XmlDocument doc = new XmlDocument();
        doc.LoadXml(dialogFile.text);
        XmlNodeList nodes = doc.SelectNodes("//dialog/text");
        foreach (XmlNode node in nodes)
        {
            List<TagDef> tags = new List<TagDef>();
            Stack<string> currentTags = new Stack<string>();
            string line = node.InnerXml;
            string actualLine = "";
            for (int i = 0; i < line.Length; ++i)
            {
                // Tag starting
                if (line[i] == '<')
                {
                    int start = actualLine.Length;
                    ++i; // Advance past <
                    string tag = ""; // This holds all text inside the tag including parameters
                    bool endTag = false; // Set if this tag starts with </

                    // Parse until end of tag
                    while (line[i] != '>')
                    {
                        if (line[i] == '/')
                            endTag = true;
                        else
                            tag += line[i];
                        ++i;
                    }

                    // This was an end tag, gotta make sure it was the last one on the stack
                    if (endTag)
                    {
                        string lastTag = currentTags.Pop();
                        if (lastTag.Equals(tag))
                        {
                            foreach (TagDef def in tags)
                            {
                                if (def.name.Equals(tag))
                                {
                                    def.end = actualLine.Length;
                                    break;
                                }
                            }
                        }
                    }
                    // Start tag, make a tagdef and push onto stack
                    else
                    {
                        TagDef def = new TagDef(tag, start, -1);
                        currentTags.Push(def.name);
                        tags.Add(def); // Default end to -1, we'll update when we find the end tag
                    }
                } // End tag parsing
                // Just text parsing
                else
                    actualLine += line[i];
            }

            Debug.Log("Actual text: " + actualLine);
            foreach (TagDef tag in tags)
                Debug.Log(tag);

            dialogs.Add(new Dialog(actualLine, tags));
        }
	}
	
	// Update is called once per frame
	void Update ()
    {
        if (Input.GetKeyDown(KeyCode.KeypadEnter) && !runText)
            runText = true;

        if (runText && !started)
        {
            Debug.Log("Starting text.");
            StartCoroutine(AnimateText());
            started = true;
        }
	}

    IEnumerator AnimateText()
    {
        string fullLine = dialogs[0].text;
        List<TagDef> tags = dialogs[0].format;
        float delay = .125f;

        for (int i = 0; i < fullLine.Length; ++i)
        {
            string line = "";
            Stack<TagDef> activeTags = new Stack<TagDef>();
            for (int j = 0; j < i; ++j)
            {
                // Push new starting tags
                for (int tagNum = 0; tagNum < tags.Count; ++tagNum )
                {
                    TagDef tag = tags[tagNum];
                    if (tag.start == j)
                    {
                        line += '<' + tag.name + '>';
                        activeTags.Push(tag);
                        Debug.Log("Pushing tag: " + tag.name);
                    }
                }

                while (activeTags .Count > 0 && activeTags.Peek().end == j)
                {
                    TagDef tag = activeTags.Pop();
                    line += "</" + tag.name + '>';
                }

                line += fullLine[j];
            }
            
            while (activeTags.Count > 0)
            {
                TagDef tag = activeTags.Pop();
                line += "</" + tag.name + '>';
            }

            text.text = line;
            yield return new WaitForSeconds(delay);
        }
    }
}
