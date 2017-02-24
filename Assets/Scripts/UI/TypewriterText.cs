using System.Collections;
using System.Collections.Generic;
using System.Xml;
using UnityEngine;
using UnityEngine.UI;

interface Tag
{
    int start { get; set; }
    int end { get; set; }
    string ToString();
}

class FormatTag : Tag
{
    public string name { get; set; }
    public int start { get; set; }
    public int end { get; set; }
    public Dictionary<string, string> tagParams {get; private set; }

    public FormatTag(string tagString, int start, int end)
    {
        // Get just the tag name and parse out the parameters
        tagParams = new Dictionary<string, string>();
        string[] tagValues = tagString.Split(' ');
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
        string ret = "FormatTag(" + name + ", [";
        foreach (KeyValuePair<string, string> pair in tagParams)
            ret += pair.Key + "=" + pair.Value + " ";
        ret += "], " + start + ", " + end + ")";
        return ret ;
    }

    public virtual string startTag
    {
        get
        {
            string[] joinedParams = new string[tagParams.Count];
            int i = 0;
            foreach (KeyValuePair<string, string> pair in tagParams)
            {
                joinedParams[i] = pair.Key + "=" + pair.Value;
                ++i;
            }
            string ret = name + string.Join(" ", joinedParams);
            Debug.Log("Format tag start" + ret);
            return name + string.Join(" ", joinedParams);
        }
    }

    public virtual string endTag
    {
        get { return name; }
    }
}

class SpeedTag : Tag
{
    public int start { get; set; }
    public int end { get; set; }
    public int speed { get; set; }

    public SpeedTag(int speed, int start, int end)
    {
        this.speed = speed;
        this.start = start;
        this.end = end;
    }

    public override string ToString()
    {
        return "SpeedTag(" + speed + ", " + start + ", " + end + ")";
    }
}

// The color tag ends with something just called color
class ColorTag : FormatTag
{
    // Init exactly the same
    public ColorTag(string tagString, int start, int end) : base(tagString, start, end) { }

    public override string endTag
    {
        get
        {
            return "color";
        }
    }
}

class Dialog
{
    public string text { get; set; }
    public List<FormatTag> format { get; private set; }

    public Dialog()
    {
        text = "";
        format = new List<FormatTag>();
    }

    public void addTag(FormatTag tag)
    {
        format.Add(tag);
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
        if (text != null)
            parseFile();
	}

    private Tag addTag(ref Dialog dia, string tagString, int start, int end)
    {
        Tag t;
        // Default tags can just be handled by a tag def
        if (!tagString.StartsWith("custom"))
        {
            // Create the format tag, 
            FormatTag ft = new FormatTag(tagString, start, end);
            dia.addTag(ft);
            t = ft;
        }
        else
        {
            string[] split = tagString.Split(' ');
            Debug.Assert(split.Length > 1, "Custom tag had no extra values: " + tagString);
            
            // It's a color tag
            if (split[1].StartsWith("color"))
            {
                ColorTag ft = new ColorTag(split[1], start, end);
                dia.addTag(ft);
                t = ft;
            }
            // It's a speed tag
            else if (split[1].StartsWith("speed"))
            {
                string[] speedSplit = split[1].Split('=');
                Debug.Assert(speedSplit.Length == 2, "Speed split not 2");

                Debug.Log("Bringing in int: " + speedSplit[1]);
                int speed = int.Parse(speedSplit[1].Replace("\"", "")); // Values are quoted..
                SpeedTag st = new SpeedTag(speed, start, end);
                t = st;
            }
            else
            {
                Debug.Assert(false, "Unknown custom tag");
                t = null;
            }
        }
        return t;
    }
     

    void parseFile()
    {
        XmlDocument doc = new XmlDocument(); // Get the XML document
        doc.LoadXml(dialogFile.text);  // Load the text asset into the XML document

        // Select and iterate over the text nodes
        XmlNodeList nodes = doc.SelectNodes("//dialog/text");
        foreach (XmlNode node in nodes)
        {
            Dialog dia = new Dialog();
            Stack<Tag> currentTags = new Stack<Tag>();
            string line = node.InnerXml;
            Debug.Log("line: " + line);
            string actualLine = "";
            for (int i = 0; i < line.Length; ++i)
            {
                //Debug.Log("here " + line[i]);
                // Tag starting
                if (line[i] == '<')
                {
                    string tag = ""; // This holds all text inside the tag including parameters
                    int start = actualLine.Length;
                    bool endTag = false; // Set if this tag starts with </

                    ++i; // Advance past <
                    
                    // Is this an end tag?
                    if (line[i] == '/')
                    {
                        endTag = true;
                        ++i; // Advance past /
                    }

                    // Parse until end of tag
                    while (line[i] != '>')
                    {
                        tag += line[i];
                        ++i; // Advance past this character
                    }

                    // This was an end tag, pop the most recent tag and set its end
                    // We can assume this is ending the most recent tag because
                    // if it wasn't the xml parser would've failed
                    if (endTag)
                    {
                        Tag lastTag = currentTags.Pop();
                        lastTag.end = actualLine.Length;
                    }
                    // Start tag, make a tagdef and push onto stack
                    else
                    {
                        Tag t = addTag(ref dia, tag, start, -1); // Default end to -1, we'll update when we find the end tag
                        Debug.Log("After adding '" + tag + "', " + dia.format.Count);
                        currentTags.Push(t);
                    }
                } // End tag parsing
                // Just text parsing
                else
                    actualLine += line[i];
            }

            // Finally, attach the dialogue text
            dia.text = actualLine;
            
            // TODO remove debugs
            Debug.Log("Actual text: " + actualLine);
            foreach (FormatTag tag in dia.format)
                Debug.Log(tag);
            
            // Add to the list of parsed dialogs
            dialogs.Add(dia);
        }
    }

    // Set to xml text file
    void setText(TextAsset asset)
    {
        dialogFile = asset;
        // Parse file
    }

    void startText()
    {
        return;
    }

    bool isTextDone()
    {
        return true;
    }
	
	// Update is called once per frame
	void Update ()
    {
        if (Input.GetKeyDown(KeyCode.P) && !runText)
        {
            Debug.Log("Starting text.");
            runText = true;
        }

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
        List<FormatTag> tags = dialogs[0].format;
        Debug.Log("Got " + tags.Count + " tags");
        float delay = .125f;

        for (int i = 0; i < fullLine.Length; ++i)
        {
            string line = "";
            Stack<FormatTag> activeTags = new Stack<FormatTag>();
            for (int j = 0; j < i; ++j)
            {
                // Push new starting tags
                for (int tagNum = 0; tagNum < tags.Count; ++tagNum )
                {
                    FormatTag tag = tags[tagNum];
                    if (tag.start == j)
                    {
                        line += '<' + tag.startTag + '>';
                        activeTags.Push(tag);
                    }
                }

                while (activeTags.Count > 0 && activeTags.Peek().end == j)
                {
                    FormatTag tag = activeTags.Pop();
                    line += "</" + tag.endTag + '>';
                }

                line += fullLine[j];
            }
            
            while (activeTags.Count > 0)
            {
                FormatTag tag = activeTags.Pop();
                line += "</" + tag.endTag + '>';
            }

            text.text = line;
            yield return new WaitForSeconds(delay);
        }
    }
}
