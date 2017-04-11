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
    public float speed { get; set; }

    public SpeedTag(float speed, int start, int end)
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

class PanTag : Tag
{
    public int start { get; set; }
    public int end { get { return start; } set { this.start = value; } }
    public string objName { get; private set; }
    public float delay { get; private set; }

    public PanTag(string objName, float delay, int start)
    {
        this.objName = objName;
        this.delay = delay;
        this.start = start;

    }
}

class VoiceTag : Tag
{
    public int start { get; set; }
    public int end { get; set; }
    public string voice { get; set; }

    public VoiceTag(string voice, int start, int end)
    {
        this.voice = voice;
        this.start = start;
        this.end = end;
    }

    public override string ToString()
    {
        return "VoiceTag(" + voice + ", " + start + ", " + end + ")";
    }
}

class MoveTag : Tag
{
    public int start { get; set; }
    public int end { get { return start; } set { this.start = value; } }
    public string dest { get; set; }
    public string mover { get; set; }

    public MoveTag(string dest, int start, string mover)
    {
        this.dest = dest;
        this.start = start;
        this.mover = mover;
        this.end = end;
    }

    public override string ToString()
    {
        return "MoveTag(" + dest + ", " + mover + ", " + start + ", " + end + ")";
    }
}

class SpeakerTag : Tag
{
    public int start { get; set; }
    public int end { get { return start; } set { this.start = value; } }
    public string name { get; set; }

    public SpeakerTag(string name)
    {
        this.name = name;
        start = 0;
    }

    public override string ToString()
    {
        return "SpeakerTag(" + name + ", " + start + ", " + end + ")";
    }
}

class AnimationTag : Tag
{
    public int start { get; set; }
    public int end { get { return start; } set { this.start = value; } }
    public string name { get; set; }
    public string animation { get; set; }

    public AnimationTag(string name, string animation, int start, int end)
    {
        this.name = name;
        this.animation = animation;
        this.start = start;
        this.end = end;
    }

    public override string ToString()
    {
        return "AnimationTag(" + name + ", " + animation + ", " + start + ", " + end + ")";
    }
}

class Dialog
{
    public string text { get; set; }
    public SpeakerTag speaker;
    public List<FormatTag> format { get; private set; }
    public List<SpeedTag> speeds { get; private set; }
    public List<VoiceTag> voices { get; private set; }
    public List<PanTag> pans { get; private set; }
    public List<MoveTag> moves { get; private set; }
    public List<AnimationTag> animations { get; private set; }

    public Dialog()
    {
        text = "";
        speaker = null;
        format = new List<FormatTag>();
        speeds = new List<SpeedTag>();
        voices = new List<VoiceTag>();
        pans = new List<PanTag>();
        moves = new List<MoveTag>();
        animations = new List<AnimationTag>();
    }

    public void addTag(FormatTag tag)
    {
        format.Add(tag);
    }

    public void addSpeed(SpeedTag tag)
    {
        speeds.Add(tag);
    }

    public void addVoice(VoiceTag tag)
    {
        voices.Add(tag);
    }

    public void addPan(PanTag tag)
    {
        pans.Add(tag);
    }

    public void addMove(MoveTag tag)
    {
        moves.Add(tag);
    }

    public void addAnimation(AnimationTag tag)
    {
        animations.Add(tag);
    }
}

public class TypewriterText : MonoBehaviour {
    private CutsceneManager cutsceneManager;
    public GameControls controls;

    private TextAsset dialogFile = null;
    List<Dialog> dialogs = new List<Dialog>();
    bool started = false;
    bool skipText = false;

    Voice voice;

    void Start()
    {
        cutsceneManager = GetComponent<CutsceneManager>();

        // Preload voice resources
        Resources.LoadAll("Voices");
    }

    void Update()
    {
        if (!started)
            skipText = false;
        else if (controls.SkipDialogue())
            skipText = true;
    }

    // Figure out how to parse a tag string and return an appropriate tag while adding it to the dialog
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
                
                float speed = float.Parse(speedSplit[1].Replace("\"", "")); // Values are quoted..
                SpeedTag st = new SpeedTag(speed, start, end);
                dia.addSpeed(st);
                t = st;
            }
            else if (split[1].StartsWith("pan"))
            {
                Debug.Assert(split.Length > 2, "Not enough parameters for pan");
                string[] objNameSplit = split[1].Split('=');
                string[] speedSplit = split[2].Split('=');
                Debug.Assert(objNameSplit.Length == 2, "objNameSplit didn't have key value");
                Debug.Assert(speedSplit.Length == 2, "pan speedSplit didn't have key value");

                string objName = objNameSplit[1].Replace("\"", ""); // Values are quoted..
                float speed = float.Parse(speedSplit[1].Replace("\"", "")); // Values are quoted..

                PanTag pt = new PanTag(objName, speed, start);
                dia.addPan(pt);

                t = pt;
            }
            else if (split[1].StartsWith("voice"))
            {
                string[] voiceSplit = tagString.Split('=');
                Debug.Assert(voiceSplit.Length == 2, "Voice split not 2");

                string voice = voiceSplit[1].Replace("\"", ""); // Values are quoted..
                voice = voice.Trim();
                VoiceTag vt = new VoiceTag(voice, start, end);
                dia.addVoice(vt);

                t = vt;
            }
            else if (split[1].StartsWith("move"))
            {
                Debug.Assert(split.Length > 2, "Not enough parameters for move");
                string[] destSplit = split[1].Split('=');
                string[] moverSplit = split[2].Split('=');
                Debug.Assert(destSplit.Length == 2, "Destination split not 2");
                Debug.Assert(moverSplit.Length == 2, "Mover split not 2");

                string dest = destSplit[1].Replace("\"", ""); // Values are quoted..
                string mover = moverSplit[1].Replace("\"", ""); // Values are quoted..
                MoveTag mt = new MoveTag(dest, start, mover);
                dia.addMove(mt);

                t = mt;
            }
            else if (split[1].StartsWith("speaker"))
            {
                string[] speakerSplit = split[1].Split('=');
                Debug.Assert(speakerSplit.Length == 2, "Move split not 2");
                string speaker = speakerSplit[1].Replace("\"", ""); // Values are quoted..
                SpeakerTag st = new SpeakerTag(speaker);
                dia.speaker = st;

                t = st;
            }
            else if (split[1].StartsWith("animation"))
            {
                Debug.Assert(split.Length > 2, "Not enough parameters for animation");
                string[] nameSplit = split[1].Split('=');
                string[] animationSplit = split[2].Split('=');
                Debug.Assert(nameSplit.Length == 2, "Name split not 2");
                Debug.Assert(animationSplit.Length == 2, "Animation split not 2");

                string name = nameSplit[1].Replace("\"", ""); // Values are quoted..
                string animation = animationSplit[1].Replace("\"", ""); // Values are quoted..
                AnimationTag at = new AnimationTag(name, animation, start, end);
                dia.addAnimation(at);

                t = at;
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
        // If we've already parsed a file one, then we need
        if (dialogs.Count > 0)
            dialogs.Clear();

        XmlDocument doc = new XmlDocument(); // Get the XML document
        doc.LoadXml(dialogFile.text);  // Load the text asset into the XML document

        // Select and iterate over the text nodes
        XmlNodeList nodes = doc.SelectNodes("//dialog/text");
        foreach (XmlNode node in nodes)
        {
            Dialog dia = new Dialog();
            Stack<Tag> currentTags = new Stack<Tag>();
            string line = node.InnerXml;
            string actualLine = "";
            for (int i = 0; i < line.Length; ++i)
            {
                // Tag starting
                if (line[i] == '<')
                {
                    string tag = ""; // This holds all text inside the tag including parameters
                    int start = actualLine.Length;
                    bool endTag = false; // Set if this tag starts with </
                    bool atomTag = false; // Set if this tag ends with />

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
                        // We founds the end of the tag and it's an atomic tag
                        if (line[i] == '/' && line[i+1] == '>')
                        {
                            // Debug.Log("Found atomic tag: " + tag);
                            atomTag = true;
                            ++i; // Skip the /, we don't want it in the tag string
                            continue; // just go back to checking the loop (should be > now)
                        }
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
                    else if (atomTag)
                    {
                        // We don't need to hold onto a reference to this
                        // because we don't need to pop it from a stack
                        addTag(ref dia, tag, start, start); // it only exists in this position
                    }
                    // Start tag, make a tagdef and push onto stack
                    else
                    {
                        Tag t = addTag(ref dia, tag, start, -1); // Default end to -1, we'll update when we find the end tag
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
            //Debug.Log("Actual text: " + actualLine);
            //foreach (FormatTag tag in dia.format)
            //    Debug.Log(tag);
            
            // Add to the list of parsed dialogs
            dialogs.Add(dia);
        }
    }
	
    IEnumerator AnimateText(int dialogNum)
    {
        Text text;
        string fullLine = dialogs[dialogNum].text; // The full text of this line
        SpeakerTag speaker = dialogs[dialogNum].speaker;
        List<FormatTag> fTags = dialogs[dialogNum].format; // The formatters for this line
        List<SpeedTag> sTags = dialogs[dialogNum].speeds;
        List<VoiceTag> vTags = dialogs[dialogNum].voices;
        List<PanTag> pTags = dialogs[dialogNum].pans;
        List<MoveTag> mTags = dialogs[dialogNum].moves;
        List<AnimationTag> aTags = dialogs[dialogNum].animations;

        // Keep track of what speed we're putting letters out at
        Stack<SpeedTag> activeSTags = new Stack<SpeedTag>();
        activeSTags.Push(new SpeedTag(.125f, 0, fullLine.Length)); // Default here

        // Push initial speaker
        if (speaker != null)
            text = setSpeaker(speaker);
        else
            text = setSpeaker(new SpeakerTag(""));

        // Iterate once for each character in the full string
        for (int i = 0; i < fullLine.Length; ++i)
        {
            // Pop speeds that end this character
            while (activeSTags.Peek().end == i)
                activeSTags.Pop();

            // Push speeds that begin this character
            foreach (SpeedTag tag in sTags)
                if (tag.start == i)
                    activeSTags.Push(tag);

            // Change voice
            foreach (VoiceTag tag in vTags)
                if (tag.start == i)
                    loadVoice(tag.voice);

            // Pan camera in cutscene
            foreach (PanTag tag in pTags)
                if (tag.start == i)
                    startPan(tag);

            // Move someone somewhere
            foreach (MoveTag tag in mTags)
                if (tag.start == i)
                    startMove(tag);

            // Start an animation on someone
            foreach (AnimationTag tag in aTags)
                if (tag.start == i)
                    startAnimation(tag);

            // Play voice and set its delay if there's a speaker
            if (voice != null && text != null)
            {
                if (!voice.isPlaying)
                    voice.Play();
            }

            string line = "";
            Stack<FormatTag> activeFTags = new Stack<FormatTag>();
            // Iterate once over every character up to where we are in the
            // string to build up the next updated string
            for (int j = 0; j <= i; ++j)
            {
                // Push new starting tags and append the ending tag to the text
                foreach (FormatTag tag in fTags)
                {
                    if (tag.start == j)
                    {
                        line += '<' + tag.startTag + '>';
                        activeFTags.Push(tag);
                    }
                }

                // Pop tags and append the ending tag to the text
                while (activeFTags.Count > 0 && activeFTags.Peek().end == j)
                {
                    FormatTag tag = activeFTags.Pop();
                    line += "</" + tag.endTag + '>';
                }

                line += fullLine[j];
                if (fullLine[j].Equals("?"))
                    Debug.Log(fullLine[j]);
            }

            // We haven't reached the point in the full string where these tags
            // terminate, but we still need to terminate them
            while (activeFTags.Count > 0)
            {
                FormatTag tag = activeFTags.Pop();
                line += "</" + tag.endTag + '>';
            }

            // Update the text if we have a speaker
            if (text != null)
                text.text = line;

            // Delay the correct amount before our next string, if the user hasn't skipped
            if (!skipText)
                yield return new WaitForSeconds(activeSTags.Peek().speed);
        }

        if (voice != null)
            voice.Stop();

        // We're done animating
        started = false;
    }

    // Change the speaking voice by loading sounds from a prefab
    void loadVoice(string voiceName)
    {
        Debug.Log("Changing to voice \"" + voiceName + "\"");

        // Remove the old voice
        if (voice != null)
            Destroy(voice.gameObject);

        // Instantiate the new one from the resources folder
        Object prefab = Resources.Load("Voices/" + voiceName);
        GameObject newObj = Instantiate(prefab, transform) as GameObject;

        voice = newObj.GetComponent<Voice>();
    }

    // -------- Here lies the external interface
    // Set to xml text file
    public void setTextFile(TextAsset asset)
    {
        dialogFile = asset;
        parseFile();
    }

    // Call to start animating the text
    public void startText(int dialogNum)
    {
        Debug.Log("Starting dialog number " + dialogNum);
        StartCoroutine(AnimateText(dialogNum));
        started = true;
    }

    // Call to CutsceneManager to do a pan
    private void startPan(PanTag tag)
    {
        cutsceneManager.QueuePan(tag.objName, tag.start, tag.delay);
    }

    // Call to CutsceneManager to move someone
    private void startMove(MoveTag tag)
    {
        cutsceneManager.QueueMove(tag.dest, tag.start, tag.mover);
    }

    // Call to CutsceneManager to start an animation on someone
    private void startAnimation(AnimationTag tag)
    {
        cutsceneManager.startAnimation(tag.name, tag.animation);
    }

    private Text setSpeaker(SpeakerTag tag)
    {
        return cutsceneManager.DecideSpeaker(tag.name);
    }

    public bool hasSpeaker(int dialogNum)
    {
        return (dialogs[dialogNum].speaker != null);
    }

    // Check if the text is done animating
    public bool isTextDone()
    {
        return started;
    }

    public int numDialogsLoaded()
    {
        return dialogs.Count;
    }
}
